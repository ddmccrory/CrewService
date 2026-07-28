using CrewService.Application.Time;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed class CallSheetGenerationService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    IAssignmentQueryService assignmentQuery,
    IWorkAreaClock clock,
    CallSheetSlotVacancyEvaluationService vacancyEvaluationService)
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

        await CreateScheduledOnDutyRecordsAsync(uow, shiftInstance, workAreaGroupCtrlNbr, targetDate, ct);
        _ = await vacancyEvaluationService.ApplyEvaluatedStateAsync(
            uow,
            shiftInstance,
            workAreaGroupCtrlNbr,
            targetDate,
            ct);

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

        // On-duty records are a separate aggregate and do not cascade-delete with the shift, so
        // explicitly soft-delete the prior records for the slots being replaced to avoid orphans.
        var existingSlotCtrlNbrs = existingShift.PositionSlots.Select(s => s.CtrlNbr).ToList();
        var staleOnDutyRecords = await uow.OnDutyRecords.GetByPositionSlotsAsync(existingSlotCtrlNbrs, ct);
        foreach (var stale in staleOnDutyRecords)
            uow.OnDutyRecords.Remove(stale);

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

        await CreateScheduledOnDutyRecordsAsync(uow, newShift, workInstance.WorkAreaGroupCtrlNbr, targetDate, ct);
        _ = await vacancyEvaluationService.ApplyEvaluatedStateAsync(
            uow,
            newShift,
            workInstance.WorkAreaGroupCtrlNbr,
            targetDate,
            ct);

        await uow.CommitAsync(ct);
        return newShift;
    }

    private async Task CreateScheduledOnDutyRecordsAsync(
        IOrchestrationUnitOfWork uow,
        ShiftInstance shiftInstance,
        ControlNumber workAreaGroupCtrlNbr,
        DateOnly targetDate,
        CancellationToken ct)
    {
        var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(workAreaGroupCtrlNbr, ct);
        var tz = clock.ResolveTimeZone(workArea?.TimeZoneId);

        foreach (var slot in shiftInstance.PositionSlots)
        {
            if (slot.IncumbentEmployeeCtrlNbr is not { } employeeCtrlNbr)
                continue;

            var onDutyUtc = clock.CombineLocalToUtc(targetDate, slot.OnDutyTime, tz).UtcDateTime;

            var lastOffDuty = await uow.OffDutyRecords.GetLastForEmployeeAsync(employeeCtrlNbr, ct);
            var previousRestHours = OnDutyHistoryCalculator.CalculatePreviousRestHours(lastOffDuty, onDutyUtc);

            var recentOnDuty = await uow.OnDutyRecords.GetRecentForEmployeeAsync(employeeCtrlNbr, 7, ct);
            var consecutiveDays = OnDutyHistoryCalculator.CalculateConsecutiveDays(recentOnDuty, onDutyUtc);

            var isAssigned = await IsWorkingOwnAssignedPositionAsync(uow, slot, employeeCtrlNbr, ct);

            var record = OnDutyRecord.CreateScheduled(
                slot.CtrlNbr,
                employeeCtrlNbr,
                onDutyUtc,
                previousRestHours,
                consecutiveDays,
                isAssigned);

            await uow.OnDutyRecords.AddAsync(record, ct);
        }
    }

    /// <summary>
    /// Mirrors the legacy StrategicApplications <c>AssignedEmployee</c> rule: the incumbent is
    /// "assigned" when the position they are working is their own assigned position. The slot's
    /// backing <see cref="StaffablePosition"/> is resolved through its <c>CrewPosition</c> and
    /// compared against the employee's current <see cref="PositionAssignment"/> rows.
    /// </summary>
    private static async Task<bool> IsWorkingOwnAssignedPositionAsync(
        IOrchestrationUnitOfWork uow,
        PositionSlotInstance slot,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct)
    {
        if (slot.CrewPositionCtrlNbr is not { } crewPositionCtrlNbr)
            return false;

        var crewPosition = await uow.CrewPositions.GetByCtrlNbrAsync(crewPositionCtrlNbr, ct);
        if (crewPosition is null)
            return false;

        var assignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        return assignments.Any(a => a.StaffablePositionCtrlNbr == crewPosition.StaffablePositionCtrlNbr);
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

