using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed class CallSheetGenerationService(
    IAssignmentQueryService templateQuery,
    IShiftDefinitionRepository shiftDefRepo,
    IShiftInstanceRepository shiftInstanceRepo,
    IWorkInstanceRepository workInstanceRepo,
    IDepartmentRepository departmentRepo)
{
    public async Task<ShiftInstance> GenerateForShiftAsync(
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber shiftDefinitionCtrlNbr,
        DateOnly targetDate,
        ControlNumber? departmentCtrlNbr = null,
        CancellationToken ct = default)
    {
        var shiftDef = await shiftDefRepo.GetByCtrlNbrAsync(shiftDefinitionCtrlNbr, ct)
            ?? throw new InvalidOperationException($"Shift definition {shiftDefinitionCtrlNbr} not found.");

        if (!shiftDef.IsActive)
            throw new InvalidOperationException($"Shift definition '{shiftDef.ShiftCode}' is not active.");

        if (shiftDef.WorkAreaGroupCtrlNbr != workAreaGroupCtrlNbr)
            throw new InvalidOperationException($"Shift definition '{shiftDef.ShiftCode}' does not belong to the specified work area.");

        // Find or create the WorkInstance for this work area + date
        var workInstance = await FindOrCreateWorkInstanceAsync(workAreaGroupCtrlNbr, targetDate, ct);

        // Duplicate check: shift already generated for this work instance + department?
        var alreadyExists = await shiftInstanceRepo.ExistsByWorkInstanceAndShiftCodeAsync(
            workInstance.CtrlNbr, shiftDef.ShiftCode, departmentCtrlNbr, ct);

        if (alreadyExists)
            throw new InvalidOperationException(
                $"A call sheet for shift '{shiftDef.ShiftCode}' on {targetDate:yyyy-MM-dd} already exists.");

        // Resolve department name
        string? departmentName = null;
        if (departmentCtrlNbr is not null)
        {
            var dept = await departmentRepo.GetByCtrlNbrAsync(departmentCtrlNbr, ct);
            departmentName = dept?.Name;
        }

        // Query assignment templates for this shift + date
        var templates = await templateQuery.GetTemplatesForDateAsync(
            workAreaGroupCtrlNbr, shiftDefinitionCtrlNbr, targetDate, departmentCtrlNbr, ct);
        // templates may be empty (e.g. no assignments scheduled on a weekend) —
        // that is fine; the shift instance is created with zero position slots.


        // Create the shift instance with snapshot data
        var shiftInstance = ShiftInstance.Create(
            workInstance.CtrlNbr,
            shiftDef.ShiftCode,
            shiftDef.DisplayName,
            departmentCtrlNbr,
            departmentName);

        // Add position slots with denormalized assignment/craft role data
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
                    template.OffDutyTime);
            }
        }

        await shiftInstanceRepo.AddAsync(shiftInstance, ct);
        return shiftInstance;
    }

    public async Task<ShiftInstance> RegenerateShiftAsync(
        ControlNumber shiftInstanceCtrlNbr,
        CancellationToken ct = default)
    {
        var existingShift = await shiftInstanceRepo.GetByCtrlNbrAsync(shiftInstanceCtrlNbr, ct)
            ?? throw new InvalidOperationException($"Shift instance {shiftInstanceCtrlNbr} not found.");

        var workInstance = await workInstanceRepo.GetByCtrlNbrAsync(existingShift.WorkInstanceCtrlNbr, ct)
            ?? throw new InvalidOperationException($"Work instance {existingShift.WorkInstanceCtrlNbr} not found.");

        var shiftDefs = await shiftDefRepo.GetByWorkAreaAsync(workInstance.WorkAreaGroupCtrlNbr);
        var shiftDef = shiftDefs.FirstOrDefault(sd => sd.ShiftCode == existingShift.ShiftCode && sd.IsActive)
            ?? throw new InvalidOperationException($"Active shift definition for code '{existingShift.ShiftCode}' not found.");

        var targetDate = DateOnly.FromDateTime(workInstance.StartUtc);

        // Resolve department name (refresh from current data)
        string? departmentName = existingShift.DepartmentName;
        if (existingShift.DepartmentCtrlNbr is not null)
        {
            var dept = await departmentRepo.GetByCtrlNbrAsync(existingShift.DepartmentCtrlNbr, ct);
            departmentName = dept?.Name ?? existingShift.DepartmentName;
        }

        // Delete the old shift instance
        await shiftInstanceRepo.DeleteAsync(shiftInstanceCtrlNbr, ct);

        // Query fresh templates and rebuild
        var templates = await templateQuery.GetTemplatesForDateAsync(
            workInstance.WorkAreaGroupCtrlNbr, shiftDef.CtrlNbr, targetDate, existingShift.DepartmentCtrlNbr, ct);
        var newShift = ShiftInstance.Create(
            workInstance.CtrlNbr,
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
                    template.OffDutyTime);
            }
        }

        await shiftInstanceRepo.AddAsync(newShift, ct);
        return newShift;
    }

    private async Task<WorkInstance> FindOrCreateWorkInstanceAsync(
        ControlNumber workAreaGroupCtrlNbr,
        DateOnly targetDate,
        CancellationToken ct)
    {
        var dayStartUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEndUtc = targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var existing = await workInstanceRepo.GetByWorkAreaAndDateRangeAsync(
            workAreaGroupCtrlNbr, dayStartUtc, dayEndUtc);

        if (existing.Count > 0)
            return existing[0];

        var workInstance = WorkInstance.Create(
            assignmentGroupCtrlNbr: null,
            workAreaGroupCtrlNbr,
            startUtc: dayStartUtc,
            endUtc: dayEndUtc,
            callTimeUtc: null);

        await workInstanceRepo.AddAsync(workInstance, ct);
        return workInstance;
    }
}
