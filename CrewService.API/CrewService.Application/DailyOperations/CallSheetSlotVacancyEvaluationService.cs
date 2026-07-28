using CrewService.Application.Absence;
using CrewService.Application.TenantConfig;
using CrewService.Application.Time;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed record SlotVacancyEvaluation(
    PositionSlotStatus EffectiveStatus,
    SlotVacancyDisplayContract Display,
    ControlNumber? AbsenceRequestCtrlNbr);

public sealed class CallSheetSlotVacancyEvaluationService(
    IWorkAreaClock clock,
    IRailroadResolver railroadResolver,
    IAbsenceCodeRepository absenceCodeRepository)
{
    public async Task<bool> ApplyEvaluatedStateAsync(
        IOrchestrationUnitOfWork uow,
        ShiftInstance shift,
        ControlNumber workAreaGroupCtrlNbr,
        DateOnly targetDate,
        CancellationToken ct = default)
    {
        var evaluations = await EvaluateShiftAsync(uow, shift, workAreaGroupCtrlNbr, targetDate, ct);
        var changed = false;

        foreach (var slot in shift.PositionSlots)
        {
            if (!evaluations.TryGetValue(slot.CtrlNbr, out var evaluation))
                continue;

            if (await ReconcileVacancyImpactAsync(uow, slot.CtrlNbr, evaluation))
                changed = true;

            if (evaluation.EffectiveStatus == PositionSlotStatus.MarkedOff)
            {
                if (slot.Status != PositionSlotStatus.MarkedOff)
                {
                    slot.MarkMarkedOff();
                    changed = true;
                }

                continue;
            }

            if (slot.Status == PositionSlotStatus.MarkedOff)
            {
                slot.ClearMarkedOff();
                changed = true;
            }
        }

        return changed;
    }

    private static async Task<bool> ReconcileVacancyImpactAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber slotCtrlNbr,
        SlotVacancyEvaluation evaluation)
    {
        var vacancyImpacts = uow.VacancyImpacts;
        if (vacancyImpacts is null)
            return false;

        var impacts = await vacancyImpacts.GetByPositionSlotAsync(slotCtrlNbr) ?? new List<VacancyImpact>();
        var openImpact = impacts
            .OrderByDescending(i => i.CtrlNbr)
            .FirstOrDefault(i => i.ImpactEndUtc is null);
        var nowUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        if (evaluation.EffectiveStatus == PositionSlotStatus.MarkedOff
            && evaluation.AbsenceRequestCtrlNbr is { } absenceRequestCtrlNbr)
        {
            if (openImpact is not null && openImpact.AbsenceRequestCtrlNbr == absenceRequestCtrlNbr)
                return false;

            if (openImpact is not null)
            {
                openImpact.ClearByMarkUp(nowUtc);
                vacancyImpacts.Update(openImpact);
            }

            var newImpact = VacancyImpact.Create(absenceRequestCtrlNbr, slotCtrlNbr, nowUtc);
            vacancyImpacts.Add(newImpact);
            return true;
        }

        if (openImpact is null)
            return false;

        openImpact.ClearByMarkUp(nowUtc);
        vacancyImpacts.Update(openImpact);
        return true;
    }

    public async Task<int> SyncImpactedShiftsForEmployeeAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct = default)
    {
        var assignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        if (assignments.Count == 0)
            return 0;

        var crewPositionIds = new HashSet<ControlNumber>();
        foreach (var assignment in assignments)
        {
            var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(assignment.StaffablePositionCtrlNbr);
            if (crewPosition is not null)
                crewPositionIds.Add(crewPosition.CtrlNbr);
        }

        if (crewPositionIds.Count == 0)
            return 0;

        var shiftIds = new HashSet<ControlNumber>();
        var shifts = new List<ShiftInstance>();
        foreach (var crewPositionId in crewPositionIds)
        {
            var impacted = await uow.ShiftInstances.GetIncompleteByCrewPositionAsync(crewPositionId, ct);
            foreach (var shift in impacted)
            {
                if (shiftIds.Add(shift.CtrlNbr))
                    shifts.Add(shift);
            }
        }

        var changedCount = 0;
        foreach (var shift in shifts)
        {
            var workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr, ct);
            if (workInstance is null)
                continue;

            var targetDate = await ResolveTargetDateAsync(workInstance, ct);
            if (await ApplyEvaluatedStateAsync(uow, shift, workInstance.WorkAreaGroupCtrlNbr, targetDate, ct))
            {
                await uow.ShiftInstances.UpdateAsync(shift, ct);
                changedCount++;
            }
        }

        return changedCount;
    }

    private async Task<DateOnly> ResolveTargetDateAsync(WorkInstance workInstance, CancellationToken ct)
    {
        var tz = await clock.GetWorkAreaTimeZoneAsync(workInstance.WorkAreaGroupCtrlNbr, ct);
        if (tz is null)
            return DateOnly.FromDateTime(workInstance.StartUtc);

        var startUtc = DateTime.SpecifyKind(workInstance.StartUtc, DateTimeKind.Utc);
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(startUtc, tz);
        return DateOnly.FromDateTime(localStart);
    }

    public async Task<IReadOnlyDictionary<ControlNumber, SlotVacancyEvaluation>> EvaluateShiftAsync(
        IOrchestrationUnitOfWork uow,
        ShiftInstance shift,
        ControlNumber workAreaGroupCtrlNbr,
        DateOnly targetDate,
        CancellationToken ct = default)
    {
        var evaluations = new Dictionary<ControlNumber, SlotVacancyEvaluation>();
        if (shift.PositionSlots.Count == 0)
            return evaluations;

        var railroadCtrlNbr = await railroadResolver.ResolveFromWorkAreaAsync(uow, workAreaGroupCtrlNbr, ct);
        if (railroadCtrlNbr is null)
        {
            foreach (var slot in shift.PositionSlots)
            {
                evaluations[slot.CtrlNbr] = new SlotVacancyEvaluation(
                    slot.Status,
                    SlotVacancyDisplayContractResolver.Resolve(slot.Status, displayCode: null),
                    AbsenceRequestCtrlNbr: null);
            }

            return evaluations;
        }

        var tz = await clock.GetWorkAreaTimeZoneAsync(workAreaGroupCtrlNbr, ct);
        var slotIds = shift.PositionSlots.Select(s => s.CtrlNbr).ToList();
        var onDutyBySlot = (await uow.OnDutyRecords.GetByPositionSlotsAsync(slotIds, ct))
            .GroupBy(r => r.PositionSlotCtrlNbr)
            .ToDictionary(g => g.Key, g => g.ToList());

        var employeeIds = shift.PositionSlots
            .Where(s => s.IncumbentEmployeeCtrlNbr is not null)
            .Select(s => s.IncumbentEmployeeCtrlNbr!)
            .Distinct()
            .ToList();

        var absencesByEmployee = new Dictionary<ControlNumber, List<AbsenceRequest>>();
        foreach (var employeeCtrlNbr in employeeIds)
        {
            var employeeAbsences = await uow.AbsenceRequests.GetByEmployeeAsync(employeeCtrlNbr);
            absencesByEmployee[employeeCtrlNbr] = employeeAbsences;
        }

        var absenceCodeCache = new Dictionary<ControlNumber, string>();
        var cutoffMinutesBySlot = new Dictionary<ControlNumber, int>();

        foreach (var slot in shift.PositionSlots)
        {
            if (slot.Status is PositionSlotStatus.Annulled or PositionSlotStatus.Open)
            {
                evaluations[slot.CtrlNbr] = new SlotVacancyEvaluation(
                    slot.Status,
                    SlotVacancyDisplayContractResolver.Resolve(slot.Status, displayCode: null),
                    AbsenceRequestCtrlNbr: null);
                continue;
            }

            if (slot.IncumbentEmployeeCtrlNbr is not { } employeeCtrlNbr)
            {
                evaluations[slot.CtrlNbr] = new SlotVacancyEvaluation(
                    slot.Status,
                    SlotVacancyDisplayContractResolver.Resolve(slot.Status, displayCode: null),
                    AbsenceRequestCtrlNbr: null);
                continue;
            }

            if (HasInactiveOnDutyRecord(onDutyBySlot, slot.CtrlNbr))
            {
                evaluations[slot.CtrlNbr] = new SlotVacancyEvaluation(
                    slot.Status,
                    SlotVacancyDisplayContractResolver.Resolve(slot.Status, displayCode: null),
                    AbsenceRequestCtrlNbr: null);
                continue;
            }

            var (slotOnDutyUtc, slotOffDutyUtc) = ResolveSlotDutyWindowUtc(targetDate, slot, tz);
            var cutoffMinutes = await ResolveCutoffMinutesForSlotAsync(uow, slot, cutoffMinutesBySlot, ct);
            var activeAbsence = FindActiveAbsence(
                absencesByEmployee.GetValueOrDefault(employeeCtrlNbr),
                slotOnDutyUtc,
                slotOffDutyUtc,
                cutoffMinutes);

            if (slot.Status == PositionSlotStatus.DoNotFill)
            {
                var doNotFillDisplayCode = activeAbsence is null
                    ? null
                    : await ResolveDisplayCodeAsync(activeAbsence, absenceCodeCache);

                evaluations[slot.CtrlNbr] = new SlotVacancyEvaluation(
                    PositionSlotStatus.DoNotFill,
                    SlotVacancyDisplayContractResolver.Resolve(PositionSlotStatus.DoNotFill, doNotFillDisplayCode),
                    activeAbsence?.CtrlNbr);
                continue;
            }

            if (activeAbsence is null)
            {
                evaluations[slot.CtrlNbr] = new SlotVacancyEvaluation(
                    slot.Status,
                    SlotVacancyDisplayContractResolver.Resolve(slot.Status, displayCode: null),
                    AbsenceRequestCtrlNbr: null);
                continue;
            }

            var displayCode = await ResolveDisplayCodeAsync(activeAbsence, absenceCodeCache);

            evaluations[slot.CtrlNbr] = new SlotVacancyEvaluation(
                PositionSlotStatus.MarkedOff,
                SlotVacancyDisplayContractResolver.Resolve(PositionSlotStatus.MarkedOff, displayCode),
                activeAbsence.CtrlNbr);
        }

        return evaluations;
    }

    private static bool HasInactiveOnDutyRecord(
        IReadOnlyDictionary<ControlNumber, List<OnDutyRecord>> onDutyBySlot,
        ControlNumber slotCtrlNbr)
    {
        return onDutyBySlot.TryGetValue(slotCtrlNbr, out var records)
               && records.Any(r => r.Status == OnDutyStatus.TiedUp
                                   || r.CompletionStatus == OnDutyCompletionStatus.Completed);
    }

    private static AbsenceRequest? FindActiveAbsence(
        List<AbsenceRequest>? absences,
        DateTime slotOnDutyUtc,
        DateTime slotOffDutyUtc,
        int cutoffMinutes)
    {
        if (absences is null || absences.Count == 0)
            return null;

        var cutoffUtc = slotOnDutyUtc.AddMinutes(-Math.Max(0, cutoffMinutes));

        return absences
            .Where(r => r.ApprovedAtUtc is not null
                        && r.DeniedAtUtc is null
                        && r.CancelledAtUtc is null
                        && r.StartRecords.Count > 0)
            .Where(r => r.StartRecords[0].ActualStartUtc <= slotOffDutyUtc)
            .Where(r => r.EndRecords.Count == 0 || r.EndRecords[0].ActualEndUtc > cutoffUtc)
            .OrderByDescending(r => r.StartRecords[0].ActualStartUtc)
            .FirstOrDefault();
    }

    private static (DateTime OnDutyUtc, DateTime OffDutyUtc) ResolveSlotDutyWindowUtc(
        DateOnly targetDate,
        PositionSlotInstance slot,
        TimeZoneInfo? workAreaTimeZone)
    {
        var onDutyUtc = workAreaTimeZone is null
            ? DateTime.SpecifyKind(targetDate.ToDateTime(slot.OnDutyTime), DateTimeKind.Utc)
            : TimeZoneInfo.ConvertTimeToUtc(targetDate.ToDateTime(slot.OnDutyTime), workAreaTimeZone);

        var offDutyDate = slot.OffDutyTime <= slot.OnDutyTime
            ? targetDate.AddDays(1)
            : targetDate;

        var offDutyUtc = workAreaTimeZone is null
            ? DateTime.SpecifyKind(offDutyDate.ToDateTime(slot.OffDutyTime), DateTimeKind.Utc)
            : TimeZoneInfo.ConvertTimeToUtc(offDutyDate.ToDateTime(slot.OffDutyTime), workAreaTimeZone);

        return (onDutyUtc, offDutyUtc);
    }

    private static async Task<int> ResolveCutoffMinutesForSlotAsync(
        IOrchestrationUnitOfWork uow,
        PositionSlotInstance slot,
        IDictionary<ControlNumber, int> cutoffMinutesBySlot,
        CancellationToken ct)
    {
        if (cutoffMinutesBySlot.TryGetValue(slot.CtrlNbr, out var cachedCutoff))
            return cachedCutoff;

        if (slot.CrewPositionCtrlNbr is not { } crewPositionCtrlNbr)
        {
            cutoffMinutesBySlot[slot.CtrlNbr] = 0;
            return 0;
        }

        var crewPosition = await uow.CrewPositions.GetByCtrlNbrAsync(crewPositionCtrlNbr, ct);
        if (crewPosition is null)
        {
            cutoffMinutesBySlot[slot.CtrlNbr] = 0;
            return 0;
        }

        var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPosition.CraftRoleCtrlNbr, ct);
        if (craftRole is null)
        {
            cutoffMinutesBySlot[slot.CtrlNbr] = 0;
            return 0;
        }

        var craftRule = await uow.CraftCallSheetRules.GetByCraftAsync(craftRole.CraftCtrlNbr);
        var cutoff = craftRule is { IsEnabled: true }
            ? Math.Max(0, craftRule.PreOnDutyChangeCutoffMinutes)
            : 0;

        cutoffMinutesBySlot[slot.CtrlNbr] = cutoff;
        return cutoff;
    }

    private async Task<string?> ResolveDisplayCodeAsync(
        AbsenceRequest absence,
        IDictionary<ControlNumber, string> cache)
    {
        if (absence.AbsenceCodeCtrlNbr is { } codeCtrlNbr)
        {
            if (cache.TryGetValue(codeCtrlNbr, out var cachedCode))
                return cachedCode;

            var absenceCode = await absenceCodeRepository.GetByCtrlNbrAsync(codeCtrlNbr);
            var resolvedCode = string.IsNullOrWhiteSpace(absenceCode?.Code)
                ? (string.IsNullOrWhiteSpace(absence.ReasonCode) ? null : absence.ReasonCode)
                : absenceCode.Code;

            if (!string.IsNullOrWhiteSpace(resolvedCode))
                cache[codeCtrlNbr] = resolvedCode.Trim().ToUpperInvariant();

            return resolvedCode?.Trim().ToUpperInvariant();
        }

        return string.IsNullOrWhiteSpace(absence.ReasonCode)
            ? null
            : absence.ReasonCode.Trim().ToUpperInvariant();
    }
}