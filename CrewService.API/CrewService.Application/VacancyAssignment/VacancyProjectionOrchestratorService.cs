using System.Text.Json;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.VacancyAssignment;

public sealed class VacancyProjectionOrchestratorService(
    IBoardCandidateProvider boardCandidateProvider,
    ISkipContextProvider skipContextProvider)
{
    private const string VacancyReasonCode = "ABSENCE_MARK_OFF";
    private const string IncumbentRemovedVacancyReasonCode = "INCUMBENT_REMOVED";

    public async Task ReconcileForEmployeeAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct = default)
    {
        await ReconcileForEmployeeAsync(uow, employeeCtrlNbr, effectiveFromUtc: null, ct);
    }

    public async Task ReconcileForEmployeeAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        DateTime? effectiveFromUtc,
        CancellationToken ct = default)
    {
        var normalizedFromUtc = effectiveFromUtc.HasValue
            ? DateTime.SpecifyKind(effectiveFromUtc.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var impactedShifts = await GetImpactedIncompleteShiftsAsync(uow, employeeCtrlNbr, normalizedFromUtc, ct);
        if (impactedShifts.Count == 0)
            return;

        var impactedWorkAreas = impactedShifts
            .Select(s => s.WorkAreaGroupCtrlNbr)
            .Distinct()
            .ToList();

        foreach (var workAreaGroupCtrlNbr in impactedWorkAreas)
        {
            await ReconcileForWorkAreaAsync(
                uow,
                workAreaGroupCtrlNbr,
                normalizedFromUtc,
                ct);
        }
    }

    public Task ReconcileForWorkAreaAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber workAreaGroupCtrlNbr,
        CancellationToken ct = default)
    {
        return ReconcileForWorkAreaAsync(uow, workAreaGroupCtrlNbr, effectiveFromUtc: null, ct);
    }

    public async Task ReconcileForWorkAreaAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber workAreaGroupCtrlNbr,
        DateTime? effectiveFromUtc,
        CancellationToken ct = default)
    {
        var normalizedFromUtc = effectiveFromUtc.HasValue
            ? DateTime.SpecifyKind(effectiveFromUtc.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var shifts = await uow.ShiftInstances.GetIncompleteByWorkAreaAsync(workAreaGroupCtrlNbr, ct);
        if (shifts.Count == 0)
            return;

        var orderedContexts = new List<ImpactedShiftContext>();
        foreach (var shift in shifts)
        {
            var workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr, ct);
            if (workInstance is null)
                continue;

            var workStartUtc = DateTime.SpecifyKind(workInstance.StartUtc, DateTimeKind.Utc);
            var workEndUtc = DateTime.SpecifyKind(workInstance.EndUtc, DateTimeKind.Utc);

            if (normalizedFromUtc.HasValue
                && !await ShiftMayContainVacancyAtOrAfterAsync(uow, shift, workEndUtc, normalizedFromUtc.Value, ct))
            {
                continue;
            }

            orderedContexts.Add(new ImpactedShiftContext(shift, workAreaGroupCtrlNbr, workStartUtc));
        }

        if (orderedContexts.Count == 0)
            return;

        await ReconcileForShiftsAsync(
            uow,
            workAreaGroupCtrlNbr,
            orderedContexts
                .OrderBy(c => c.WorkStartUtc)
                .ThenBy(c => c.Shift.ShiftCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Shift.CtrlNbr.Value)
                .Select(c => c.Shift)
                .ToList(),
            ct);
    }

    public Task ReconcileForShiftsAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber workAreaGroupCtrlNbr,
        IReadOnlyList<ShiftInstance> orderedShifts,
        CancellationToken ct = default)
    {
        return ReconcileForShiftsAsync(
            uow,
            workAreaGroupCtrlNbr,
            orderedShifts,
            anchorPositionSlotCtrlNbr: null,
            ct);
    }

    public async Task ReconcileForShiftsAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber workAreaGroupCtrlNbr,
        IReadOnlyList<ShiftInstance> orderedShifts,
        ControlNumber? anchorPositionSlotCtrlNbr,
        CancellationToken ct = default)
    {
        if (orderedShifts.Count == 0)
            return;

        var vacancyContexts = new List<OrderedSlotVacancyContext>();
        var onDutyUtcBySlot = new Dictionary<ControlNumber, DateTime>();

        for (var shiftOrder = 0; shiftOrder < orderedShifts.Count; shiftOrder++)
        {
            var shift = orderedShifts[shiftOrder];
            var workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr, ct);
            if (workInstance is null)
                continue;

            var shiftStartDateUtc = DateTime.SpecifyKind(workInstance.StartUtc, DateTimeKind.Utc).Date;
            var scopedSlots = shiftOrder == 0 && anchorPositionSlotCtrlNbr is not null
                ? GetProjectionScopedSlotsForAnchor(shift.PositionSlots, anchorPositionSlotCtrlNbr)
                : shift.PositionSlots;
            var contexts = await BuildSlotContextsAsync(uow, scopedSlots, ct);

            foreach (var context in contexts)
            {
                if (ShouldProjectVacancy(context.Slot))
                {
                    _ = await EnsureOpenVacancyAsync(uow, workAreaGroupCtrlNbr, context, ct);
                    vacancyContexts.Add(new OrderedSlotVacancyContext(shiftOrder, context));
                    onDutyUtcBySlot[context.Slot.CtrlNbr] = DateTime.SpecifyKind(
                        shiftStartDateUtc + context.Slot.OnDutyTime.ToTimeSpan(),
                        DateTimeKind.Utc);
                    continue;
                }

                await CloseOpenVacancyAsync(uow, context, ct);
                await ClearProjectionsAsync(uow, context.Slot.CtrlNbr);
            }
        }

        if (vacancyContexts.Count == 0)
            return;

        var nowUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        var reservedEmployeeCtrlNbrs = new HashSet<long>();

        var orderedVacancies = vacancyContexts
            .OrderBy(v => v.ShiftOrder)
            .ThenBy(v => GetAssignmentOrderGroup(v.Context.Slot.AssignmentCode))
            .ThenBy(v => GetNumericAssignmentOrder(v.Context.Slot.AssignmentCode))
            .ThenBy(v => v.Context.Slot.AssignmentCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(v => v.Context.Slot.DisplayOrder)
            .ThenBy(v => v.Context.Slot.CtrlNbr.Value)
            .ToList();

        foreach (var orderedVacancy in orderedVacancies)
        {
            var vacancy = orderedVacancy.Context;
            var skipSlot = new SkipRuleSlot(vacancy.Slot.CtrlNbr, vacancy.Slot.CrewPositionCtrlNbr);
            var candidates = await boardCandidateProvider.GetCandidatesAsync(
                workAreaGroupCtrlNbr,
                vacancy.CraftCtrlNbr,
                skipSlot,
                ct);

            var vacancyOnDutyUtc = onDutyUtcBySlot.TryGetValue(vacancy.Slot.CtrlNbr, out var slotOnDutyUtc)
                ? slotOnDutyUtc
                : nowUtc;

            var projectedEmployeeCtrlNbr = await SelectProjectedEmployeeAsync(
                uow,
                candidates,
                skipSlot,
                vacancyOnDutyUtc,
                reservedEmployeeCtrlNbrs,
                ct);

            if (projectedEmployeeCtrlNbr is not null)
                reservedEmployeeCtrlNbrs.Add(projectedEmployeeCtrlNbr.Value);

            await ReplaceProjectionAsync(
                uow,
                vacancy.Slot.CtrlNbr,
                nowUtc,
                projectedEmployeeCtrlNbr,
                JsonSerializer.Serialize(new
                {
                    Source = nameof(VacancyProjectionOrchestratorService),
                    ShiftOrder = orderedVacancy.ShiftOrder,
                    VacancyOrder = new { vacancy.Slot.AssignmentCode, vacancy.Slot.DisplayOrder },
                    CandidateCount = candidates.Count,
                    SelectedEmployeeCtrlNbr = projectedEmployeeCtrlNbr?.Value
                }));
        }
    }

    [Obsolete("Use ReconcileForShiftsAsync with an anchored forward shift list.")]
    public Task ReconcileForShiftAsync(
        IOrchestrationUnitOfWork uow,
        ShiftInstance shift,
        ControlNumber workAreaGroupCtrlNbr,
        CancellationToken ct = default)
    {
        return ReconcileForShiftsAsync(
            uow,
            workAreaGroupCtrlNbr,
            [shift],
            anchorPositionSlotCtrlNbr: null,
            ct);
    }

    [Obsolete("Use ReconcileForShiftsAsync with an anchored forward shift list.")]
    public Task ReconcileForShiftAsync(
        IOrchestrationUnitOfWork uow,
        ShiftInstance shift,
        ControlNumber workAreaGroupCtrlNbr,
        IDictionary<ControlNumber, int>? projectionSequenceByCraft,
        CancellationToken ct = default)
    {
        _ = projectionSequenceByCraft;

        return ReconcileForShiftsAsync(
            uow,
            workAreaGroupCtrlNbr,
            [shift],
            anchorPositionSlotCtrlNbr: null,
            ct);
    }

    private async Task ReconcileForShiftSlotsAsync(
        IOrchestrationUnitOfWork uow,
        ShiftInstance shift,
        ControlNumber workAreaGroupCtrlNbr,
        IReadOnlyList<PositionSlotInstance> slots,
        IDictionary<ControlNumber, int>? projectionSequenceByCraft,
        CancellationToken ct)
    {
        if (slots.Count == 0)
            return;

        var workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr, ct);
        if (workInstance is null)
            return;

        var shiftStartDateUtc = DateTime.SpecifyKind(workInstance.StartUtc, DateTimeKind.Utc).Date;

        var contexts = await BuildSlotContextsAsync(uow, slots, ct);
        var vacancyContexts = new List<SlotVacancyContext>();

        foreach (var context in contexts)
        {
            if (ShouldProjectVacancy(context.Slot))
            {
                _ = await EnsureOpenVacancyAsync(uow, workAreaGroupCtrlNbr, context, ct);
                vacancyContexts.Add(context);
                continue;
            }

            await CloseOpenVacancyAsync(uow, context, ct);
            await ClearProjectionsAsync(uow, context.Slot.CtrlNbr);
        }

        if (vacancyContexts.Count == 0)
            return;

        var orderedVacancies = vacancyContexts
            .OrderBy(v => GetAssignmentOrderGroup(v.Slot.AssignmentCode))
            .ThenBy(v => GetNumericAssignmentOrder(v.Slot.AssignmentCode))
            .ThenBy(v => v.Slot.AssignmentCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(v => v.Slot.DisplayOrder)
            .ThenBy(v => v.Slot.CtrlNbr.Value)
            .ToList();

        _ = projectionSequenceByCraft;
        var nowUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        var reservedEmployeeCtrlNbrs = new HashSet<long>();

        foreach (var vacancy in orderedVacancies)
        {
            var skipSlot = new SkipRuleSlot(vacancy.Slot.CtrlNbr, vacancy.Slot.CrewPositionCtrlNbr);
            var candidates = await boardCandidateProvider.GetCandidatesAsync(
                workAreaGroupCtrlNbr,
                vacancy.CraftCtrlNbr,
                skipSlot,
                ct);

            var vacancyOnDutyUtc = DateTime.SpecifyKind(
                shiftStartDateUtc + vacancy.Slot.OnDutyTime.ToTimeSpan(),
                DateTimeKind.Utc);

            var projectedEmployeeCtrlNbr = await SelectProjectedEmployeeAsync(
                uow,
                candidates,
                skipSlot,
                vacancyOnDutyUtc,
                reservedEmployeeCtrlNbrs,
                ct);

            if (projectedEmployeeCtrlNbr is not null)
                reservedEmployeeCtrlNbrs.Add(projectedEmployeeCtrlNbr.Value);

            await ReplaceProjectionAsync(
                uow,
                vacancy.Slot.CtrlNbr,
                nowUtc,
                projectedEmployeeCtrlNbr,
                JsonSerializer.Serialize(new
                {
                    Source = nameof(VacancyProjectionOrchestratorService),
                    VacancyOrder = new { vacancy.Slot.AssignmentCode, vacancy.Slot.DisplayOrder },
                    CandidateCount = candidates.Count,
                    SelectedEmployeeCtrlNbr = projectedEmployeeCtrlNbr?.Value
                }));
        }
    }

    private async Task<ControlNumber?> SelectProjectedEmployeeAsync(
        IOrchestrationUnitOfWork uow,
        IReadOnlyList<SkipRuleCandidate> candidates,
        SkipRuleSlot skipSlot,
        DateTime vacancyOnDutyUtc,
        ISet<long> reservedEmployeeCtrlNbrs,
        CancellationToken ct)
    {
        foreach (var candidate in candidates)
        {
            if (reservedEmployeeCtrlNbrs.Contains(candidate.EmployeeCtrlNbr.Value))
                continue;

            var context = await skipContextProvider.BuildAsync(uow, candidate, skipSlot, ct);
            if (!IsEligibleForVacancy(context, vacancyOnDutyUtc))
                continue;

            return candidate.EmployeeCtrlNbr;
        }

        return null;
    }

    private static async Task<IReadOnlyList<ImpactedShiftContext>> GetImpactedIncompleteShiftsAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        DateTime? effectiveFromUtc,
        CancellationToken ct)
    {
        var shifts = new List<ShiftInstance>();
        var seen = new HashSet<ControlNumber>();

        var incumbentImpactedShifts = await uow.ShiftInstances.GetIncompleteByIncumbentEmployeeAsync(employeeCtrlNbr, ct);
        foreach (var shift in incumbentImpactedShifts)
        {
            if (seen.Add(shift.CtrlNbr))
                shifts.Add(shift);
        }

        // Include incomplete shifts tied to this employee through existing on-duty records, even when
        // the employee is no longer the current incumbent (e.g., marked-off/coverage transitions).
        var onDutyRecords = await uow.OnDutyRecords.GetIncompleteForEmployeeAsync(employeeCtrlNbr, ct);
        foreach (var onDutyRecord in onDutyRecords.OrderByDescending(r => r.OnDutyTimeUtc))
        {
            var tieUpContext = await uow.OnDutyRecords.GetTieUpContextAsync(onDutyRecord.CtrlNbr, ct);
            if (tieUpContext is null)
                continue;

            var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(tieUpContext.ShiftInstanceCtrlNbr, ct);
            if (shift is null || shift.IsComplete)
                continue;

            if (seen.Add(shift.CtrlNbr))
                shifts.Add(shift);
        }

        var assignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        var crewPositionCtrlNbrs = new HashSet<ControlNumber>();
        foreach (var assignment in assignments)
        {
            var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(assignment.StaffablePositionCtrlNbr);
            if (crewPosition is not null)
                crewPositionCtrlNbrs.Add(crewPosition.CtrlNbr);
        }

        foreach (var crewPositionCtrlNbr in crewPositionCtrlNbrs)
        {
            var impacted = await uow.ShiftInstances.GetIncompleteByCrewPositionAsync(crewPositionCtrlNbr, ct);
            foreach (var shift in impacted)
            {
                if (seen.Add(shift.CtrlNbr))
                    shifts.Add(shift);
            }
        }

        if (shifts.Count == 0)
            return [];

        var contexts = new List<ImpactedShiftContext>();
        foreach (var shift in shifts)
        {
            var workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr, ct);
            if (workInstance is null)
                continue;

            var workStartUtc = DateTime.SpecifyKind(workInstance.StartUtc, DateTimeKind.Utc);
            var workEndUtc = DateTime.SpecifyKind(workInstance.EndUtc, DateTimeKind.Utc);

            if (effectiveFromUtc.HasValue
                && !await ShiftMayContainVacancyAtOrAfterAsync(uow, shift, workEndUtc, effectiveFromUtc.Value, ct))
            {
                continue;
            }

            contexts.Add(new ImpactedShiftContext(shift, workInstance.WorkAreaGroupCtrlNbr, workStartUtc));
        }

        return contexts
            .OrderBy(c => c.WorkStartUtc)
            .ThenBy(c => c.Shift.ShiftCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Shift.CtrlNbr.Value)
            .ToList();
    }

    private static async Task<bool> ShiftMayContainVacancyAtOrAfterAsync(
        IOrchestrationUnitOfWork uow,
        ShiftInstance shift,
        DateTime workEndUtc,
        DateTime effectiveFromUtc,
        CancellationToken ct)
    {
        var normalizedEffectiveFromUtc = DateTime.SpecifyKind(effectiveFromUtc, DateTimeKind.Utc);

        var slotCtrlNbrs = shift.PositionSlots.Select(s => s.CtrlNbr).ToList();
        if (slotCtrlNbrs.Count == 0)
            return workEndUtc > normalizedEffectiveFromUtc;

        var onDutyRecords = await uow.OnDutyRecords.GetByPositionSlotsAsync(slotCtrlNbrs, ct);
        if (onDutyRecords.Any(r => DateTime.SpecifyKind(r.OnDutyTimeUtc, DateTimeKind.Utc) >= normalizedEffectiveFromUtc))
            return true;

        return workEndUtc > normalizedEffectiveFromUtc;
    }

    private static async Task<IReadOnlyList<SlotVacancyContext>> BuildSlotContextsAsync(
        IOrchestrationUnitOfWork uow,
        IReadOnlyList<PositionSlotInstance> slots,
        CancellationToken ct)
    {
        var contexts = new List<SlotVacancyContext>(slots.Count);

        foreach (var slot in slots)
        {
            if (slot.CrewPositionCtrlNbr is not { } crewPositionCtrlNbr)
                continue;

            var crewPosition = await uow.CrewPositions.GetByCtrlNbrAsync(crewPositionCtrlNbr, ct);
            if (crewPosition is null)
                continue;

            var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPosition.CraftRoleCtrlNbr, ct);
            if (craftRole is null)
                continue;

            contexts.Add(new SlotVacancyContext(slot, craftRole.CraftCtrlNbr, crewPosition.StaffablePositionCtrlNbr));
        }

        return contexts;
    }

    private static async Task<PositionVacancy> EnsureOpenVacancyAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber workAreaGroupCtrlNbr,
        SlotVacancyContext context,
        CancellationToken ct)
    {
        var vacancyReasonCode = ResolveVacancyReasonCode(context.Slot);
        var existing = await uow.PositionVacancies.GetByTargetAsync(StaffablePositionType.Crew, context.TargetCtrlNbr);
        var open = existing.FirstOrDefault(v => IsOpenVacancy(v) && v.VacancyReasonCode == vacancyReasonCode);
        if (open is not null)
            return open;

        var vacancy = PositionVacancy.Create(
            workAreaGroupCtrlNbr,
            StaffablePositionType.Crew,
            context.TargetCtrlNbr,
            context.CraftCtrlNbr,
            vacancyReasonCode,
            previousIncumbentCtrlNbr: context.Slot.IncumbentEmployeeCtrlNbr,
            targetName: BuildTargetName(context.Slot));
        uow.PositionVacancies.Add(vacancy);
        return vacancy;
    }

    private static async Task CloseOpenVacancyAsync(
        IOrchestrationUnitOfWork uow,
        SlotVacancyContext context,
        CancellationToken ct)
    {
        var activeReasonCode = ResolveVacancyReasonCode(context.Slot);
        var existing = await uow.PositionVacancies.GetByTargetAsync(StaffablePositionType.Crew, context.TargetCtrlNbr);
        foreach (var vacancy in existing.Where(v => IsOpenVacancy(v) && v.VacancyReasonCode != activeReasonCode))
        {
            vacancy.Abolish();
            uow.PositionVacancies.Update(vacancy);
        }

        if (ShouldProjectVacancy(context.Slot))
            return;

        foreach (var vacancy in existing.Where(v => IsOpenVacancy(v)))
        {
            vacancy.Abolish();
            uow.PositionVacancies.Update(vacancy);
        }
    }

    private static bool ShouldProjectVacancy(PositionSlotInstance slot)
    {
        if (slot.Status is PositionSlotStatus.Annulled or PositionSlotStatus.DoNotFill)
            return false;

        if (slot.Status == PositionSlotStatus.MarkedOff)
            return true;

        return slot.IncumbentEmployeeCtrlNbr is null;
    }

    private static string ResolveVacancyReasonCode(PositionSlotInstance slot)
        => slot.Status == PositionSlotStatus.MarkedOff
            ? VacancyReasonCode
            : IncumbentRemovedVacancyReasonCode;

    private static bool IsOpenVacancy(PositionVacancy vacancy) =>
        vacancy.Status == "Open" || vacancy.Status == "Bulletined";

    private static int GetAssignmentOrderGroup(string? assignmentCode)
        => long.TryParse(assignmentCode, out _) ? 0 : 1;

    private static long GetNumericAssignmentOrder(string? assignmentCode)
        => long.TryParse(assignmentCode, out var numeric) ? numeric : long.MaxValue;

    private static string BuildTargetName(PositionSlotInstance slot)
    {
        if (string.IsNullOrWhiteSpace(slot.AssignmentName))
            return $"{slot.AssignmentCode} Position {slot.DisplayOrder}";

        return $"{slot.AssignmentName} — Position {slot.DisplayOrder}";
    }

    private static IReadOnlyList<PositionSlotInstance> GetProjectionScopedSlotsForAnchor(
        IReadOnlyList<PositionSlotInstance> slots,
        ControlNumber anchorPositionSlotCtrlNbr)
    {
        var ordered = slots
            .OrderBy(s => GetAssignmentOrderGroup(s.AssignmentCode))
            .ThenBy(s => GetNumericAssignmentOrder(s.AssignmentCode))
            .ThenBy(s => s.AssignmentCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.DisplayOrder)
            .ThenBy(s => s.CtrlNbr.Value)
            .ToList();

        var anchorIndex = ordered.FindIndex(s => s.CtrlNbr == anchorPositionSlotCtrlNbr);
        if (anchorIndex < 0)
            return ordered;

        return ordered.Skip(anchorIndex).ToList();
    }

    private static async Task ClearProjectionsAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber positionSlotCtrlNbr)
    {
        var existing = await uow.DispatchProjections.GetByPositionSlotAsync(positionSlotCtrlNbr);
        foreach (var projection in existing)
            uow.DispatchProjections.Remove(projection);
    }

    private static async Task ReplaceProjectionAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber positionSlotCtrlNbr,
        DateTime asOfUtc,
        ControlNumber? projectedEmployeeCtrlNbr,
        string traceJson)
    {
        await ClearProjectionsAsync(uow, positionSlotCtrlNbr);

        var projection = DispatchProjection.Create(
            positionSlotCtrlNbr,
            asOfUtc,
            projectedEmployeeCtrlNbr,
            traceJson);
        uow.DispatchProjections.Add(projection);
    }

    private sealed record SlotVacancyContext(
        PositionSlotInstance Slot,
        ControlNumber CraftCtrlNbr,
        ControlNumber TargetCtrlNbr);

    private sealed record ImpactedShiftContext(
        ShiftInstance Shift,
        ControlNumber WorkAreaGroupCtrlNbr,
        DateTime WorkStartUtc);

    private sealed record OrderedSlotVacancyContext(
        int ShiftOrder,
        SlotVacancyContext Context);

    private static bool IsEligibleForVacancy(SkipContext context, DateTime vacancyOnDutyUtc)
    {
        if (context.HasActiveOnDuty)
            return false;

        if (context.IsMarkedOff)
            return false;

        if (!context.IsQualified)
            return false;

        if (context.RestedAtUtc is null)
            return true;

        var restedAtUtc = DateTime.SpecifyKind(context.RestedAtUtc.Value, DateTimeKind.Utc);
        return restedAtUtc <= DateTime.SpecifyKind(vacancyOnDutyUtc, DateTimeKind.Utc);
    }
}
