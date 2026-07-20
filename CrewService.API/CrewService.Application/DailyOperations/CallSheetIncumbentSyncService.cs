using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed class CallSheetIncumbentSyncService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task SyncStaffablePositionIncumbentAsync(
        ControlNumber staffablePositionCtrlNbr,
        ControlNumber? incumbentEmployeeCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        await SyncStaffablePositionIncumbentAsync(uow, staffablePositionCtrlNbr, incumbentEmployeeCtrlNbr, ct);
        await uow.CommitAsync(ct);
    }

    public static async Task SyncStaffablePositionIncumbentAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber staffablePositionCtrlNbr,
        ControlNumber? incumbentEmployeeCtrlNbr,
        CancellationToken ct = default)
    {
        var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(staffablePositionCtrlNbr);
        if (crewPosition is null)
            return;

        var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPosition.CraftRoleCtrlNbr, ct);
        var craftPolicy = craftRole is null
            ? null
            : await uow.CraftCallSheetRules.GetByCraftAsync(craftRole.CraftCtrlNbr);

        var shifts = await uow.ShiftInstances.GetIncompleteByCrewPositionAsync(crewPosition.CtrlNbr, ct);
        if (shifts.Count == 0)
            return;

        var slotCtrlNbrs = shifts
            .SelectMany(s => s.PositionSlots)
            .Where(s => s.CrewPositionCtrlNbr == crewPosition.CtrlNbr)
            .Select(s => s.CtrlNbr)
            .Distinct()
            .ToList();

        var existingOnDutyRecords = await uow.OnDutyRecords.GetByPositionSlotsAsync(slotCtrlNbrs, ct);
        var recordsBySlot = existingOnDutyRecords
            .GroupBy(r => r.PositionSlotCtrlNbr)
            .ToDictionary(g => g.Key, g => g.ToList());

        var workInstanceByCtrlNbr = new Dictionary<ControlNumber, WorkInstance?>();

        foreach (var shift in shifts)
        {
            var changed = false;
            if (!workInstanceByCtrlNbr.TryGetValue(shift.WorkInstanceCtrlNbr, out var workInstance))
            {
                workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr, ct);
                workInstanceByCtrlNbr[shift.WorkInstanceCtrlNbr] = workInstance;
            }

            if (workInstance is null)
                throw new InvalidOperationException($"Work instance {shift.WorkInstanceCtrlNbr.Value} not found for shift {shift.CtrlNbr.Value}.");

            var slots = shift.PositionSlots
                .Where(s => s.CrewPositionCtrlNbr == crewPosition.CtrlNbr)
                .ToList();

            foreach (var slot in slots)
            {
                var slotOnDutyTimeUtc = await ResolveSlotOnDutyTimeUtcAsync(uow, slot, workInstance, ct);

                if (craftPolicy is { IsEnabled: true }
                    && !CanChangeSlotIncumbent(slotOnDutyTimeUtc, craftPolicy.PreOnDutyChangeCutoffMinutes))
                {
                    continue;
                }

                if (slot.Status == PositionSlotStatus.OnDuty
                    || slot.Status == PositionSlotStatus.OnDutyOvertime
                    || slot.Status == PositionSlotStatus.TiedUp
                    || slot.IsAnnulled)
                {
                    continue;
                }

                if (slot.IncumbentEmployeeCtrlNbr == incumbentEmployeeCtrlNbr
                    && slot.IsIncumbent == (incumbentEmployeeCtrlNbr is not null))
                {
                    continue;
                }

                var previousIncumbentEmployeeCtrlNbr = slot.IncumbentEmployeeCtrlNbr;

                if (previousIncumbentEmployeeCtrlNbr is not null)
                {
                    RemoveOnDutyRecordsForEmployee(
                        uow,
                        recordsBySlot,
                        slot.CtrlNbr,
                        previousIncumbentEmployeeCtrlNbr);
                }

                slot.SetIncumbent(incumbentEmployeeCtrlNbr, isIncumbent: incumbentEmployeeCtrlNbr is not null);

                if (incumbentEmployeeCtrlNbr is not null)
                {
                    await EnsureOnDutyRecordForAwardedEmployeeAsync(
                        uow,
                        recordsBySlot,
                        slot,
                        slotOnDutyTimeUtc,
                        incumbentEmployeeCtrlNbr,
                        ct);
                }

                changed = true;
            }

            if (changed)
                await uow.ShiftInstances.UpdateAsync(shift, ct);
        }
    }

    private static bool CanChangeSlotIncumbent(
        DateTime slotOnDutyTimeUtc,
        int cutoffMinutes)
    {
        if (cutoffMinutes <= 0)
            return true;

        return DateTime.UtcNow <= slotOnDutyTimeUtc.AddMinutes(-cutoffMinutes);
    }

    private static async Task<DateTime> ResolveSlotOnDutyTimeUtcAsync(
        IOrchestrationUnitOfWork uow,
        PositionSlotInstance slot,
        WorkInstance workInstance,
        CancellationToken ct)
    {
        var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(workInstance.WorkAreaGroupCtrlNbr, ct);
        var tz = ResolveTimeZone(workArea?.TimeZoneId);

        var localDate = DateOnly.FromDateTime(workInstance.StartUtc);
        var localOnDuty = localDate.ToDateTime(slot.OnDutyTime, DateTimeKind.Unspecified);

        if (tz is null)
            return DateTime.SpecifyKind(localOnDuty, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeToUtc(localOnDuty, tz);
    }

    private static TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return null;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    private static void RemoveOnDutyRecordsForEmployee(
        IOrchestrationUnitOfWork uow,
        IDictionary<ControlNumber, List<OnDutyRecord>> recordsBySlot,
        ControlNumber slotCtrlNbr,
        ControlNumber employeeCtrlNbr)
    {
        if (!recordsBySlot.TryGetValue(slotCtrlNbr, out var slotRecords)
            || slotRecords.Count == 0)
        {
            return;
        }

        var toRemove = slotRecords
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr)
            .ToList();

        foreach (var record in toRemove)
        {
            uow.OnDutyRecords.Remove(record);
            slotRecords.Remove(record);
        }
    }

    private static async Task EnsureOnDutyRecordForAwardedEmployeeAsync(
        IOrchestrationUnitOfWork uow,
        IDictionary<ControlNumber, List<OnDutyRecord>> recordsBySlot,
        PositionSlotInstance slot,
        DateTime slotOnDutyUtc,
        ControlNumber awardedEmployeeCtrlNbr,
        CancellationToken ct)
    {
        if (recordsBySlot.TryGetValue(slot.CtrlNbr, out var slotRecords)
            && slotRecords.Any(r => r.EmployeeCtrlNbr == awardedEmployeeCtrlNbr))
        {
            return;
        }

        var lastOffDuty = await uow.OffDutyRecords.GetLastForEmployeeAsync(awardedEmployeeCtrlNbr, ct);
        var previousRestHours = OnDutyHistoryCalculator.CalculatePreviousRestHours(lastOffDuty, slotOnDutyUtc);

        var recentOnDuty = await uow.OnDutyRecords.GetRecentForEmployeeAsync(awardedEmployeeCtrlNbr, 7, ct);
        var consecutiveDays = OnDutyHistoryCalculator.CalculateConsecutiveDays(recentOnDuty, slotOnDutyUtc);

        var record = OnDutyRecord.CreateScheduled(
            slot.CtrlNbr,
            awardedEmployeeCtrlNbr,
            slotOnDutyUtc,
            previousRestHours,
            consecutiveDays,
            isAssigned: true);

        await uow.OnDutyRecords.AddAsync(record, ct);

        if (!recordsBySlot.TryGetValue(slot.CtrlNbr, out slotRecords))
        {
            slotRecords = [];
            recordsBySlot[slot.CtrlNbr] = slotRecords;
        }

        slotRecords.Add(record);
    }
}
