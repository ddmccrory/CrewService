using CrewService.Application.VacancyAssignment;
using CrewService.Application.Qualifications;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Services;

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
            .Where(p => p.Status == PositionSlotStatus.Open && !p.IsAnnulled && !p.IsDoNotFill)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new SkipRuleSlot(p.CtrlNbr, p.CrewPositionCtrlNbr))];
    }
}

internal sealed class BoardCandidateProvider(CrewServiceDbContext dbContext) : IBoardCandidateProvider
{
    public async Task<IReadOnlyList<SkipRuleCandidate>> GetCandidatesAsync(
        ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        var boards = await dbContext.Set<RosterBoard>()
            .Include(b => b.Positions)
            .Where(b => b.CraftCtrlNbr == craftCtrlNbr
                        && b.IsActive
                        && b.BoardType == BoardType.ExtraBoard)
            .ToListAsync(ct);

        if (boards.Count == 0) return [];

        var positions = boards
            .SelectMany(b => b.Positions)
            .Where(p => p.HangoutStatus == "Active")
            .OrderBy(p => p.PositionOrder)
            .ToList();

        return [.. positions
            .Select(p => new SkipRuleCandidate(p.EmployeeCtrlNbr, p.CtrlNbr, p.PositionOrder))];
    }
}

internal sealed class SkipContextProvider(
    CrewServiceDbContext dbContext,
    EmployeeEligibilityService eligibilityService) : ISkipContextProvider
{
    public async Task<SkipContext> BuildAsync(
        SkipRuleCandidate candidate, SkipRuleSlot slot, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var empCtrl = candidate.EmployeeCtrlNbr;

        var hasActiveOnDuty = await dbContext.Set<OnDutyRecord>()
            .AnyAsync(r => r.EmployeeCtrlNbr == empCtrl && r.Status == OnDutyStatus.OnDuty, ct);

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

        var eligibility = await eligibilityService.CheckEligibilityAsync(empCtrl, slot.PositionSlotCtrlNbr, ct);

        return new SkipContext
        {
            NowUtc = now,
            HasActiveOnDuty = hasActiveOnDuty,
            IsMarkedOff = isMarkedOff,
            IsRested = isRested,
            IsQualified = eligibility.IsEligible,
            QualificationBlockingReasons = eligibility.BlockingReasons
                .Select(r => $"{r.RuleCode}: {r.Description}")
                .ToList(),
            RecentOnDutyCount = recentOnDuty.Count,
            WeeklyHoursWorked = 0m,
            WeeklyHoursCap = 0m,
            WorkedDayCap = 0,
            RestedAtUtc = restedAt
        };
    }
}
