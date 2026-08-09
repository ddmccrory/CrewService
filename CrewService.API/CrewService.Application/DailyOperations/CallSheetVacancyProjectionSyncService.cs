using CrewService.Application.VacancyAssignment;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed class CallSheetVacancyProjectionSyncService(
    CallSheetSlotVacancyEvaluationService vacancyEvaluationService,
    VacancyProjectionOrchestratorService vacancyProjectionOrchestrator)
{
    public async Task ReconcileFromStaffablePositionChangeAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber staffablePositionCtrlNbr,
        CancellationToken ct = default)
    {
        var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(staffablePositionCtrlNbr);
        if (crewPosition is null)
            return;

        var impactedShifts = await uow.ShiftInstances.GetIncompleteByCrewPositionAsync(crewPosition.CtrlNbr, ct);
        if (impactedShifts.Count == 0)
            return;

        foreach (var workInstanceGroup in impactedShifts.GroupBy(s => s.WorkInstanceCtrlNbr))
        {
            var workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(workInstanceGroup.Key, ct);
            if (workInstance is null)
                continue;

            var shiftDefinitions = await uow.ShiftDefinitions.GetByWorkAreaAsync(workInstance.WorkAreaGroupCtrlNbr);
            var anchorShift = workInstanceGroup
                .OrderBy(s => ResolveShiftDisplayOrder(shiftDefinitions, s.ShiftCode))
                .ThenBy(s => s.CtrlNbr.Value)
                .First();

            await ReconcileFromShiftChangeAsync(uow, anchorShift, ct);
        }
    }

    public async Task ReconcileFromShiftChangeAsync(
        IOrchestrationUnitOfWork uow,
        ShiftInstance anchorShift,
        ControlNumber? anchorPositionSlotCtrlNbr,
        CancellationToken ct = default)
    {
        var workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(anchorShift.WorkInstanceCtrlNbr, ct)
            ?? throw new InvalidOperationException($"Work instance {anchorShift.WorkInstanceCtrlNbr.Value} not found.");

        var workAreaGroupCtrlNbr = workInstance.WorkAreaGroupCtrlNbr;
        var shiftDefinitions = await uow.ShiftDefinitions.GetByWorkAreaAsync(workAreaGroupCtrlNbr);
        var incompleteShifts = await uow.ShiftInstances.GetIncompleteByWorkAreaAsync(workAreaGroupCtrlNbr, ct);
        if (incompleteShifts.Count == 0)
            return;

        var workInstanceByCtrlNbr = new Dictionary<ControlNumber, WorkInstance>();
        foreach (var workInstanceCtrlNbr in incompleteShifts.Select(s => s.WorkInstanceCtrlNbr).Distinct())
        {
            var resolvedWorkInstance = await uow.WorkInstances.GetByCtrlNbrAsync(workInstanceCtrlNbr, ct);
            if (resolvedWorkInstance is not null)
                workInstanceByCtrlNbr[workInstanceCtrlNbr] = resolvedWorkInstance;
        }

        var orderedShifts = incompleteShifts
            .Where(s => workInstanceByCtrlNbr.ContainsKey(s.WorkInstanceCtrlNbr))
            .OrderBy(s => DateTime.SpecifyKind(workInstanceByCtrlNbr[s.WorkInstanceCtrlNbr].StartUtc, DateTimeKind.Utc))
            .ThenBy(s => ResolveShiftDisplayOrder(shiftDefinitions, s.ShiftCode))
            .ThenBy(s => s.CtrlNbr.Value)
            .ToList();

        if (orderedShifts.Count == 0)
            return;

        var anchorIndex = orderedShifts.FindIndex(s => s.CtrlNbr == anchorShift.CtrlNbr);
        if (anchorIndex < 0)
            return;

        var forwardShifts = orderedShifts.Skip(anchorIndex).ToList();
        if (forwardShifts.Count == 0)
            return;

        foreach (var shift in forwardShifts)
        {
            var shiftWorkInstance = workInstanceByCtrlNbr[shift.WorkInstanceCtrlNbr];
            var targetDate = DateOnly.FromDateTime(DateTime.SpecifyKind(shiftWorkInstance.StartUtc, DateTimeKind.Utc));

            _ = await vacancyEvaluationService.ApplyEvaluatedStateAsync(
                uow,
                shift,
                workAreaGroupCtrlNbr,
                targetDate,
                ct);
        }

        await vacancyProjectionOrchestrator.ReconcileForShiftsAsync(
            uow,
            workAreaGroupCtrlNbr,
            forwardShifts,
            anchorPositionSlotCtrlNbr,
            ct);
    }

    public Task ReconcileFromShiftChangeAsync(
        IOrchestrationUnitOfWork uow,
        ShiftInstance anchorShift,
        CancellationToken ct = default)
    {
        return ReconcileFromShiftChangeAsync(uow, anchorShift, anchorPositionSlotCtrlNbr: null, ct);
    }

    private static int ResolveShiftDisplayOrder(
        IEnumerable<ShiftDefinition> shiftDefinitions,
        string shiftCode)
    {
        var match = shiftDefinitions
            .Where(sd => string.Equals(sd.ShiftCode, shiftCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(sd => sd.DisplayOrder)
            .FirstOrDefault();

        return match?.DisplayOrder ?? int.MaxValue;
    }
}
