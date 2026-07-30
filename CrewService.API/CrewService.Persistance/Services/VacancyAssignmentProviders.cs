using CrewService.Application.VacancyAssignment;
using CrewService.Application.Qualifications;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Crews;
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

internal sealed class BoardSnapshotSource(CrewServiceDbContext dbContext) : IBoardSnapshotSource
{
    public async Task<IReadOnlyList<BoardSnapshotSlot>> GetBoardSlotsAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default)
    {
        return await dbContext.Set<ShiftInstance>()
            .Where(s => s.CtrlNbr == shiftInstanceCtrlNbr)
            .SelectMany(s => s.BoardSlots)
            .OrderBy(b => b.BoardOrder)
            .ThenBy(b => b.CallSequence)
            .Select(b => new BoardSnapshotSlot(
                b.CtrlNbr,
                b.ShiftInstanceCtrlNbr,
                b.RosterBoardCtrlNbr,
                b.RosterBoardPositionCtrlNbr,
                b.EmployeeCtrlNbr,
                b.BoardOrder,
                b.CallSequence,
                b.TieUpAtUtc,
                b.Status.ToString(),
                b.BoardName,
                b.EmployeeName,
                b.PositionName))
            .ToListAsync(ct);
    }
}

internal sealed class BoardCandidateProvider(CrewServiceDbContext dbContext) : IBoardCandidateProvider
{
    public async Task<IReadOnlyList<SkipRuleCandidate>> GetCandidatesAsync(
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber craftCtrlNbr,
        SkipRuleSlot slot,
        CancellationToken ct = default)
    {
        ControlNumber? scopedBoardCtrlNbr = null;

        if (slot.CrewPositionCtrlNbr is { } crewPositionCtrlNbr)
        {
            var craftRoleCtrlNbr = await dbContext.Set<CrewPosition>()
                .Where(p => p.CtrlNbr == crewPositionCtrlNbr)
                .Select(p => (ControlNumber?)p.CraftRoleCtrlNbr)
                .SingleOrDefaultAsync(ct);

            if (craftRoleCtrlNbr is not null)
            {
                scopedBoardCtrlNbr = await dbContext.Set<CraftRole>()
                    .Where(r => r.CtrlNbr == craftRoleCtrlNbr)
                    .Select(r => r.DefaultRosterBoardCtrlNbr)
                    .SingleOrDefaultAsync(ct);
            }
        }

        var boards = await dbContext.Set<RosterBoard>()
            .Include(b => b.Positions)
            .Where(b => b.CraftCtrlNbr == craftCtrlNbr
                        && b.IsActive
                        && b.BoardType == BoardType.ExtraBoard
                        && (scopedBoardCtrlNbr == null || b.CtrlNbr == scopedBoardCtrlNbr))
            .ToListAsync(ct);

        if (boards.Count == 0) return [];

        var boardPositionCtrlNbrs = boards
            .SelectMany(b => b.Positions)
            .Select(p => p.CtrlNbr)
            .ToHashSet();

        var nowUtc = DateTime.UtcNow;
        var latestBoardSlotByPosition = await dbContext.Set<BoardSlotInstance>()
            .Where(bs => bs.RosterBoardPositionCtrlNbr != null
                && boardPositionCtrlNbrs.Contains(bs.RosterBoardPositionCtrlNbr!))
            .GroupBy(bs => bs.RosterBoardPositionCtrlNbr!)
            .Select(g => g
                .OrderByDescending(bs => bs.CallSequence)
                .ThenBy(bs => bs.BoardOrder)
                .Select(bs => new
                {
                    PositionCtrlNbr = bs.RosterBoardPositionCtrlNbr!,
                    bs.Status,
                    bs.RestAvailableAtUtc
                })
                .First())
            .ToListAsync(ct);

        var latestBoardSlotByPositionCtrlNbr = latestBoardSlotByPosition
            .ToDictionary(x => x.PositionCtrlNbr, x => (x.Status, x.RestAvailableAtUtc));

        var positions = boards
            .SelectMany(b => b.Positions)
            .Where(p => IsCallBoardRested(
                latestBoardSlotByPositionCtrlNbr.TryGetValue(p.CtrlNbr, out var boardSlot)
                    ? boardSlot.Status
                    : (BoardSlotStatus?)null,
                latestBoardSlotByPositionCtrlNbr.TryGetValue(p.CtrlNbr, out boardSlot)
                    ? boardSlot.RestAvailableAtUtc
                    : null,
                nowUtc))
            .OrderBy(p => p.PositionOrder)
            .ThenBy(p => p.CtrlNbr.Value)
            .ToList();

        return [.. positions
            .Select(p => new SkipRuleCandidate(p.EmployeeCtrlNbr, p.CtrlNbr, p.PositionOrder))];
    }

    private static bool IsCallBoardRested(
        BoardSlotStatus? status,
        DateTime? restAvailableAtUtc,
        DateTime nowUtc)
    {
        if (status is null)
            return true;

        if (status is BoardSlotStatus.Called
            or BoardSlotStatus.OnDuty
            or BoardSlotStatus.MarkedOff
            or BoardSlotStatus.Unavailable)
        {
            return false;
        }

        if (restAvailableAtUtc is null)
            return true;

        var restUtc = DateTime.SpecifyKind(restAvailableAtUtc.Value, DateTimeKind.Utc);
        return restUtc <= nowUtc;
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
                            && a.ApprovedAtUtc != null
                            && a.DeniedAtUtc == null
                            && a.CancelledAtUtc == null
                           && a.ScheduledStartUtc <= now
                            && dbContext.Set<AbsenceStartRecord>().Any(s => s.AbsenceRequestCtrlNbr == a.CtrlNbr)
                            && !dbContext.Set<AbsenceEndRecord>().Any(e => e.AbsenceRequestCtrlNbr == a.CtrlNbr), ct);

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
