using CrewService.Application.VacancyAssignment;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Dispatching;

internal sealed class OpenSlotProvider(CrewServiceDbContext dbContext) : IOpenSlotProvider
{
    public async Task<IReadOnlyList<SkipRuleSlot>> GetOpenSlotsAsync(
        ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default)
    {
        var shift = await dbContext.Set<ShiftInstance>()
            .Include(s => s.PositionSlots)
            .SingleOrDefaultAsync(s => s.CtrlNbr == shiftInstanceCtrlNbr, ct);

        if (shift is null) return [];

        return [.. shift.PositionSlots
            .Where(p => p.Status == "Open" && !p.IsAnnulled && !p.IsDoNotFill)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new SkipRuleSlot(p.CtrlNbr, p.CrewPositionCtrlNbr))];
    }
}

internal sealed class BoardCandidateProvider(CrewServiceDbContext dbContext) : IBoardCandidateProvider
{
    public async Task<IReadOnlyList<SkipRuleCandidate>> GetCandidatesAsync(
        ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var boardIds = await dbContext.Set<ExtraBoard>()
            .Where(b => b.CraftCtrlNbr == craftCtrlNbr && b.IsActive)
            .Select(b => b.CtrlNbr)
            .ToListAsync(ct);

        if (boardIds.Count == 0) return [];

        var members = await dbContext.Set<BoardMember>()
            .Where(m => boardIds.Contains(m.ExtraBoardCtrlNbr)
                        && m.StartUtc <= now
                        && (m.EndUtc == null || m.EndUtc > now))
            .OrderBy(m => m.OrderIndex)
            .ToListAsync(ct);

        return [.. members
            .Select(m => new SkipRuleCandidate(m.EmployeeCtrlNbr, m.CtrlNbr, m.OrderIndex))];
    }
}

internal sealed class SkipContextProvider(CrewServiceDbContext dbContext) : ISkipContextProvider
{
    public async Task<SkipContext> BuildAsync(
        SkipRuleCandidate candidate, SkipRuleSlot slot, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var empCtrl = candidate.EmployeeCtrlNbr;

        var hasActiveOnDuty = await dbContext.Set<OnDutyRecord>()
            .AnyAsync(r => r.EmployeeCtrlNbr == empCtrl && r.Status == "OnDuty", ct);

        var isMarkedOff = await dbContext.Set<AbsenceRequest>()
            .AnyAsync(a => a.EmployeeCtrlNbr == empCtrl
                           && a.Status == "APPROVED"
                           && a.StartUtc <= now
                           && (a.EndUtc == null || a.EndUtc > now), ct);

        var lastOff = await dbContext.Set<OffDutyRecord>()
            .Where(r => r.EmployeeCtrlNbr == empCtrl)
            .OrderByDescending(r => r.OffDutyTimeUtc)
            .FirstOrDefaultAsync(ct);

        var isRested = lastOff is null || lastOff.RestedAtUtc <= now;
        var restedAt = lastOff?.RestedAtUtc;

        var sevenDaysAgo = now.AddDays(-7);
        var recentOnDuty = await dbContext.Set<OnDutyRecord>()
            .Where(r => r.EmployeeCtrlNbr == empCtrl && r.OnDutyTimeUtc >= sevenDaysAgo)
            .ToListAsync(ct);

        return new SkipContext
        {
            NowUtc = now,
            HasActiveOnDuty = hasActiveOnDuty,
            IsMarkedOff = isMarkedOff,
            IsRested = isRested,
            IsQualified = true,
            RecentOnDutyCount = recentOnDuty.Count,
            WeeklyHoursWorked = 0m,
            WeeklyHoursCap = 0m,
            WorkedDayCap = 0,
            RestedAtUtc = restedAt
        };
    }
}
