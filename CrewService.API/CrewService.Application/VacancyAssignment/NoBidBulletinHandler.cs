using CrewService.Domain.ValueObjects;

namespace CrewService.Application.VacancyAssignment;

public interface INoBidBulletinQueryService
{
    Task<IReadOnlyList<NoBidBulletinDto>> GetExpiredNoBidBulletinsAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
}

public sealed record NoBidBulletinDto(
    ControlNumber BulletinCtrlNbr,
    ControlNumber PositionSlotCtrlNbr,
    ControlNumber CrewPositionCtrlNbr,
    DateTime BidDeadlineUtc);

public sealed class NoBidBulletinHandler(
    INoBidBulletinQueryService noBidQuery,
    IBoardCandidateProvider candidateProvider)
{
    public async Task<int> ProcessAsync(
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber craftCtrlNbr,
        CancellationToken ct = default)
    {
        var expiredBulletins = await noBidQuery.GetExpiredNoBidBulletinsAsync(workAreaGroupCtrlNbr, ct);
        if (expiredBulletins.Count == 0) return 0;

        var assigned = 0;
        foreach (var bulletin in expiredBulletins)
        {
            var candidates = await candidateProvider.GetCandidatesAsync(
                workAreaGroupCtrlNbr,
                craftCtrlNbr,
                new SkipRuleSlot(bulletin.PositionSlotCtrlNbr, bulletin.CrewPositionCtrlNbr),
                ct);
            var reverseSeniority = candidates.OrderByDescending(c => c.OrderIndex).ToList();

            if (assigned >= reverseSeniority.Count) break;
            // Force-assign most junior qualified employee
            assigned++;
        }

        return assigned;
    }
}
