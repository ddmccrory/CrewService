using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed class CallSheetGenerationService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    IAssignmentQueryService assignmentQuery)
{
    public async Task<ShiftInstance> GenerateForShiftAsync(
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber shiftDefinitionCtrlNbr,
        DateOnly targetDate,
        ControlNumber? departmentCtrlNbr = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var shiftDef = await uow.ShiftDefinitions.GetByCtrlNbrAsync(shiftDefinitionCtrlNbr, ct)
            ?? throw new InvalidOperationException($"Shift definition {shiftDefinitionCtrlNbr} not found.");

        if (!shiftDef.IsActive)
            throw new InvalidOperationException($"Shift definition '{shiftDef.ShiftCode}' is not active.");

        if (shiftDef.WorkAreaGroupCtrlNbr != workAreaGroupCtrlNbr)
            throw new InvalidOperationException($"Shift definition '{shiftDef.ShiftCode}' does not belong to the specified work area.");

        var workInstance = await FindOrCreateWorkInstanceAsync(uow, workAreaGroupCtrlNbr, targetDate, ct);

        var alreadyExists = await uow.ShiftInstances.ExistsByWorkInstanceAndShiftCodeAsync(
            workInstance.CtrlNbr, shiftDef.ShiftCode, departmentCtrlNbr, ct);

        if (alreadyExists)
            throw new InvalidOperationException(
                $"A call sheet for shift '{shiftDef.ShiftCode}' on {targetDate:yyyy-MM-dd} already exists.");

        string? departmentName = null;
        if (departmentCtrlNbr is not null)
        {
            var dept = await uow.Departments.GetByCtrlNbrAsync(departmentCtrlNbr, ct);
            departmentName = dept?.Name;
        }

        var templates = await assignmentQuery.GetTemplatesForDateAsync(
            workAreaGroupCtrlNbr, shiftDefinitionCtrlNbr, targetDate, departmentCtrlNbr, ct);

        var shiftInstance = ShiftInstance.Create(
            workInstance.CtrlNbr,
            shiftDefinitionCtrlNbr,
            shiftDef.ShiftCode,
            shiftDef.DisplayName,
            departmentCtrlNbr,
            departmentName);

        foreach (var template in templates)
        {
            foreach (var position in template.Positions)
            {
                shiftInstance.AddPositionSlot(
                    position.PositionCtrlNbr,
                    position.IncumbentEmployeeCtrlNbr,
                    position.DisplayOrder,
                    template.AssignmentCtrlNbr,
                    template.AssignmentCode,
                    template.AssignmentName,
                    position.CraftRoleName,
                    template.GroupName,
                    template.GroupCode,
                    template.OnDutyTime,
                    template.OffDutyTime,
                    position.CrewName,
                    position.CrewType);
            }
        }

        await uow.ShiftInstances.AddAsync(shiftInstance, ct);
        await uow.CommitAsync(ct);
        return shiftInstance;
    }

    public async Task<ShiftInstance> RegenerateShiftAsync(
        ControlNumber shiftInstanceCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var existingShift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftInstanceCtrlNbr, ct)
            ?? throw new InvalidOperationException($"Shift instance {shiftInstanceCtrlNbr} not found.");

        var workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(existingShift.WorkInstanceCtrlNbr, ct)
            ?? throw new InvalidOperationException($"Work instance {existingShift.WorkInstanceCtrlNbr} not found.");

        var shiftDefs = await uow.ShiftDefinitions.GetByWorkAreaAsync(workInstance.WorkAreaGroupCtrlNbr);
        var shiftDef = shiftDefs.FirstOrDefault(sd => sd.ShiftCode == existingShift.ShiftCode && sd.IsActive)
            ?? throw new InvalidOperationException($"Active shift definition for code '{existingShift.ShiftCode}' not found.");

        var targetDate = DateOnly.FromDateTime(workInstance.StartUtc);

        string? departmentName = existingShift.DepartmentName;
        if (existingShift.DepartmentCtrlNbr is not null)
        {
            var dept = await uow.Departments.GetByCtrlNbrAsync(existingShift.DepartmentCtrlNbr, ct);
            departmentName = dept?.Name ?? existingShift.DepartmentName;
        }

        await uow.ShiftInstances.DeleteAsync(shiftInstanceCtrlNbr, ct);

        var templates = await assignmentQuery.GetTemplatesForDateAsync(
            workInstance.WorkAreaGroupCtrlNbr, shiftDef.CtrlNbr, targetDate, existingShift.DepartmentCtrlNbr, ct);

        var newShift = ShiftInstance.Create(
            workInstance.CtrlNbr,
            shiftDef.CtrlNbr,
            shiftDef.ShiftCode,
            shiftDef.DisplayName,
            existingShift.DepartmentCtrlNbr,
            departmentName);

        foreach (var template in templates)
        {
            foreach (var position in template.Positions)
            {
                newShift.AddPositionSlot(
                    position.PositionCtrlNbr,
                    position.IncumbentEmployeeCtrlNbr,
                    position.DisplayOrder,
                    template.AssignmentCtrlNbr,
                    template.AssignmentCode,
                    template.AssignmentName,
                    position.CraftRoleName,
                    template.GroupName,
                    template.GroupCode,
                    template.OnDutyTime,
                    template.OffDutyTime,
                    position.CrewName,
                    position.CrewType);
            }
        }

        await uow.ShiftInstances.AddAsync(newShift, ct);
        await uow.CommitAsync(ct);
        return newShift;
    }

    private static async Task<WorkInstance> FindOrCreateWorkInstanceAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber workAreaGroupCtrlNbr,
        DateOnly targetDate,
        CancellationToken ct)
    {
        var dayStartUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEndUtc = targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var existing = await uow.WorkInstances.GetByWorkAreaAndDateRangeAsync(
            workAreaGroupCtrlNbr, dayStartUtc, dayEndUtc);

        if (existing.Count > 0)
            return existing[0];

        var workInstance = WorkInstance.Create(
            assignmentGroupCtrlNbr: null,
            workAreaGroupCtrlNbr,
            startUtc: dayStartUtc,
            endUtc: dayEndUtc,
            callTimeUtc: null);

        await uow.WorkInstances.AddAsync(workInstance, ct);
        return workInstance;
    }
}

