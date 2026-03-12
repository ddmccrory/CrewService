using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;

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

public interface ISkipContextProvider
{
    Task<SkipContext> BuildAsync(SkipRuleCandidate candidate, SkipRuleSlot slot, CancellationToken ct = default);
}

public interface IOpenSlotProvider
{
    Task<IReadOnlyList<SkipRuleSlot>> GetOpenSlotsAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default);
}

public sealed class VacancyResolutionEngine(
    IOpenSlotProvider openSlotProvider,
    IBoardCandidateProvider candidateProvider,
    ISkipContextProvider skipContextProvider,
    IVacancyResolutionRunRepository runRepo,
    IEnumerable<ISkipRule> skipRules,
    IAssignmentStrategy assignmentStrategy)
{
    public async Task<VacancyResolutionRun> ExecuteAsync(
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber shiftInstanceCtrlNbr,
        ControlNumber craftCtrlNbr,
        CancellationToken ct = default)
    {
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

                foreach (var candidate in candidates)
                {
                    var ctx = await skipContextProvider.BuildAsync(candidate, slot, ct);
                    var skipped = false;

                    foreach (var rule in skipRules)
                    {
                        if (rule.ShouldSkip(candidate, slot, ctx))
                        {
                            skipped = true;
                            break;
                        }
                    }

                    if (skipped) continue;

                    var assignCtx = new AssignmentContext { NowUtc = DateTime.UtcNow };
                    var result = assignmentStrategy.TryAssign(candidate, slot, assignCtx);

                    if (result.Success)
                    {
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
}
