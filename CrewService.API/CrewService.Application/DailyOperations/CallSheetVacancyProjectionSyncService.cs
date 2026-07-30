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
        CancellationToken ct = default)
    {
        var workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(anchorShift.WorkInstanceCtrlNbr, ct)
            ?? throw new InvalidOperationException($"Work instance {anchorShift.WorkInstanceCtrlNbr.Value} not found.");

        var shiftDefinitions = await uow.ShiftDefinitions.GetByWorkAreaAsync(workInstance.WorkAreaGroupCtrlNbr);
        var shiftsInWorkInstance = await uow.ShiftInstances.GetByWorkInstanceAsync(workInstance.CtrlNbr, ct);
        if (shiftsInWorkInstance.Count == 0)
            return;

        var orderedShifts = shiftsInWorkInstance
            .OrderBy(s => ResolveShiftDisplayOrder(shiftDefinitions, s.ShiftCode))
            .ThenBy(s => s.CtrlNbr.Value)
            .ToList();

        if (orderedShifts.All(s => s.CtrlNbr != anchorShift.CtrlNbr))
        {
            orderedShifts.Add(anchorShift);
            orderedShifts = orderedShifts
                .OrderBy(s => ResolveShiftDisplayOrder(shiftDefinitions, s.ShiftCode))
                .ThenBy(s => s.CtrlNbr.Value)
                .ToList();
        }

        var anchorIndex = orderedShifts.FindIndex(s => s.CtrlNbr == anchorShift.CtrlNbr);
        if (anchorIndex < 0)
            return;

        var targetDate = DateOnly.FromDateTime(DateTime.SpecifyKind(workInstance.StartUtc, DateTimeKind.Utc));
        var forwardShifts = orderedShifts.Skip(anchorIndex).ToList();

        foreach (var shift in forwardShifts)
        {
            _ = await vacancyEvaluationService.ApplyEvaluatedStateAsync(
                uow,
                shift,
                workInstance.WorkAreaGroupCtrlNbr,
                targetDate,
                ct);
        }

        await vacancyProjectionOrchestrator.ReconcileForShiftsAsync(
            uow,
            workInstance.WorkAreaGroupCtrlNbr,
            forwardShifts,
            ct);
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
