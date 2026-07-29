using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using System.Text.Json;

namespace CrewService.Application.VacancyAssignment;

public interface IVacancyResolutionRunRepository
{
    Task AddAsync(VacancyResolutionRun run, CancellationToken ct = default);
}

public interface IBoardCandidateProvider
{
    Task<IReadOnlyList<SkipRuleCandidate>> GetCandidatesAsync(
        ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default);
}

public interface IBoardSnapshotSource
{
    Task<IReadOnlyList<BoardSnapshotSlot>> GetBoardSlotsAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default);
}

public sealed record BoardSnapshotSlot(
    ControlNumber BoardSlotInstanceCtrlNbr,
    ControlNumber ShiftInstanceCtrlNbr,
    ControlNumber RosterBoardCtrlNbr,
    ControlNumber? RosterBoardPositionCtrlNbr,
    ControlNumber EmployeeCtrlNbr,
    int BoardOrder,
    long CallSequence,
    DateTime? TieUpAtUtc,
    string Status,
    string BoardName,
    string EmployeeName,
    string PositionName);

public interface ISkipContextProvider
{
    Task<SkipContext> BuildAsync(SkipRuleCandidate candidate, SkipRuleSlot slot, CancellationToken ct = default);
}

public interface IOpenSlotProvider
{
    Task<IReadOnlyList<SkipRuleSlot>> GetOpenSlotsAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default);
}

public sealed record VacancyResolutionExecutionOptions(
    string TriggerSource,
    bool CaptureBoardSnapshots)
{
    public static VacancyResolutionExecutionOptions Default { get; } = new("VacancyResolutionEngine", true);
}

public sealed class VacancyResolutionEngine(
    IOpenSlotProvider openSlotProvider,
    IBoardCandidateProvider candidateProvider,
    IBoardSnapshotSource boardSnapshotSource,
    ISkipContextProvider skipContextProvider,
    IVacancyResolutionRunRepository runRepo,
    IDispatchDecisionLogRepository decisionLogRepository,
    IBoardSnapshotRepository boardSnapshotRepository,
    IBoardSelectionDecisionRepository boardSelectionDecisionRepository,
    IEnumerable<ISkipRule> skipRules,
    IAssignmentStrategy assignmentStrategy)
{
    public async Task<VacancyResolutionRun> ExecuteAsync(
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber shiftInstanceCtrlNbr,
        ControlNumber craftCtrlNbr,
        VacancyResolutionExecutionOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= VacancyResolutionExecutionOptions.Default;
        var run = VacancyResolutionRun.Start(workAreaGroupCtrlNbr, shiftInstanceCtrlNbr);

        try
        {
            var openSlots = await openSlotProvider.GetOpenSlotsAsync(shiftInstanceCtrlNbr, ct);
            var candidates = await candidateProvider.GetCandidatesAsync(workAreaGroupCtrlNbr, craftCtrlNbr, ct);

            var slotsEvaluated = 0;
            var slotsFilled = 0;

            foreach (var slot in openSlots)
            {
                slotsEvaluated++;
                var filled = false;
                var nowUtc = DateTime.UtcNow;
                var decisionSequence = await boardSnapshotRepository.GetNextDecisionSequenceAsync(shiftInstanceCtrlNbr, ct);
                var boardSlots = options.CaptureBoardSnapshots
                    ? await boardSnapshotSource.GetBoardSlotsAsync(shiftInstanceCtrlNbr, ct)
                    : [];
                var boardSlotsByEmployee = boardSlots
                    .GroupBy(s => s.EmployeeCtrlNbr)
                    .ToDictionary(g => g.Key, g => g.OrderBy(x => x.BoardOrder).ThenBy(x => x.CallSequence).First());

                BoardSnapshot? snapshot = null;
                if (options.CaptureBoardSnapshots)
                {
                    snapshot = BoardSnapshot.Create(
                        shiftInstanceCtrlNbr,
                        nowUtc,
                        options.TriggerSource,
                        decisionSequence,
                        slot.PositionSlotCtrlNbr);

                    foreach (var boardSlot in boardSlots.OrderBy(s => s.BoardOrder).ThenBy(s => s.CallSequence).ThenBy(s => s.BoardSlotInstanceCtrlNbr.Value))
                    {
                        snapshot.AddRow(BoardSnapshotRow.Create(
                            snapshot.CtrlNbr,
                            boardSlot.BoardSlotInstanceCtrlNbr,
                            boardSlot.ShiftInstanceCtrlNbr,
                            boardSlot.RosterBoardCtrlNbr,
                            boardSlot.EmployeeCtrlNbr,
                            boardSlot.BoardOrder,
                            boardSlot.CallSequence,
                            boardSlot.TieUpAtUtc,
                            boardSlot.Status,
                            boardSlot.BoardName,
                            boardSlot.EmployeeName,
                            boardSlot.PositionName,
                            boardSlot.RosterBoardPositionCtrlNbr));
                    }

                    boardSnapshotRepository.Add(snapshot);
                }

                foreach (var candidate in candidates)
                {
                    var ctx = await skipContextProvider.BuildAsync(candidate, slot, ct);
                    var skipped = false;

                    foreach (var rule in skipRules)
                    {
                        if (rule.ShouldSkip(candidate, slot, ctx))
                        {
                            var decisionJson = BuildSkipDecisionJson(rule.RuleCode, ctx);
                            boardSlotsByEmployee.TryGetValue(candidate.EmployeeCtrlNbr, out var skippedBoardSlot);

                            var skipLog = DispatchDecisionLog.Create(
                                slot.PositionSlotCtrlNbr,
                                nowUtc,
                                "Skip",
                                candidate.EmployeeCtrlNbr,
                                "VacancyResolutionEngine",
                                decisionJson);
                            decisionLogRepository.Add(skipLog);

                            var skipDecision = BoardSelectionDecision.Create(
                                shiftInstanceCtrlNbr,
                                slot.PositionSlotCtrlNbr,
                                nowUtc,
                                decisionSequence,
                                options.TriggerSource,
                                "Skip",
                                snapshotCtrlNbr: snapshot?.CtrlNbr,
                                selectedBoardSlotInstanceCtrlNbr: skippedBoardSlot?.BoardSlotInstanceCtrlNbr,
                                selectedEmployeeCtrlNbr: candidate.EmployeeCtrlNbr,
                                decisionJson: decisionJson);
                            boardSelectionDecisionRepository.Add(skipDecision);

                            skipped = true;
                            break;
                        }
                    }

                    if (skipped) continue;

                    var assignCtx = new AssignmentContext { NowUtc = DateTime.UtcNow };
                    var result = assignmentStrategy.TryAssign(candidate, slot, assignCtx);

                    if (result.Success)
                    {
                        boardSlotsByEmployee.TryGetValue(candidate.EmployeeCtrlNbr, out var selectedBoardSlot);

                        var selectLog = DispatchDecisionLog.Create(
                            slot.PositionSlotCtrlNbr,
                            nowUtc,
                            "Select",
                            result.AssignedEmployeeCtrlNbr,
                            "VacancyResolutionEngine",
                            JsonSerializer.Serialize(new { RuleCode = "ASSIGNED", candidate.OrderIndex }));
                        decisionLogRepository.Add(selectLog);

                        var selectDecision = BoardSelectionDecision.Create(
                            shiftInstanceCtrlNbr,
                            slot.PositionSlotCtrlNbr,
                            nowUtc,
                            decisionSequence,
                            options.TriggerSource,
                            "Select",
                            snapshotCtrlNbr: snapshot?.CtrlNbr,
                            selectedBoardSlotInstanceCtrlNbr: selectedBoardSlot?.BoardSlotInstanceCtrlNbr,
                            selectedEmployeeCtrlNbr: result.AssignedEmployeeCtrlNbr,
                            decisionJson: JsonSerializer.Serialize(new { RuleCode = "ASSIGNED", candidate.OrderIndex }));
                        boardSelectionDecisionRepository.Add(selectDecision);

                        slotsFilled++;
                        filled = true;
                        break;
                    }
                }

                if (!filled) { /* slot remains open */ }
            }

            run.Complete(slotsEvaluated, slotsFilled);
        }
        catch
        {
            run.Fail();
            throw;
        }
        finally
        {
            await runRepo.AddAsync(run, ct);
        }

        return run;
    }

    private static string BuildSkipDecisionJson(string ruleCode, SkipContext ctx)
    {
        if (ruleCode == "NOT_QUALIFIED")
        {
            return JsonSerializer.Serialize(new
            {
                RuleCode = ruleCode,
                QualificationBlockingReasons = ctx.QualificationBlockingReasons
            });
        }

        return JsonSerializer.Serialize(new
        {
            RuleCode = ruleCode
        });
    }
}
