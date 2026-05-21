using CrewService.Application.BackgroundWorkers;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.Bulletins;

public sealed class BulletinsService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    ILogger<BulletinsService> logger,
    IBulletinScheduleSignal scheduleSignal)
{
    public async Task<IReadOnlyList<PositionVacancy>> GetOpenVacanciesAsync(ControlNumber? railroadCtrlNbr = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return railroadCtrlNbr is not null
            ? await uow.PositionVacancies.GetOpenByRailroadAsync(railroadCtrlNbr)
            : await uow.PositionVacancies.GetOpenAsync();
    }

    public async Task<PositionVacancy> GetVacancyAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PositionVacancies.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Vacancy {ctrlNbr} not found.");
    }

    public async Task<PositionVacancy> AbolishVacancyAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Vacancy {ctrlNbr} not found.");
        vacancy.Abolish();
        await uow.PositionVacancies.UpdateAsync(vacancy, ct);
        await uow.CommitAsync(ct);
        return vacancy;
    }

    public async Task<IReadOnlyList<Bulletin>> GetPostedBulletinsAsync(ControlNumber? railroadCtrlNbr = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return railroadCtrlNbr is not null
            ? await uow.Bulletins.GetPostedByRailroadAsync(railroadCtrlNbr)
            : await uow.Bulletins.GetPostedAsync();
    }

    public async Task<IReadOnlyList<Bulletin>> GetBulletinsInDateRangeAsync(DateTime fromUtc, ControlNumber? railroadCtrlNbr = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Bulletins.GetInDateRangeAsync(fromUtc, railroadCtrlNbr);
    }

        public async Task<IReadOnlyList<Bulletin>> GetActiveBulletinsAsync(ControlNumber? railroadCtrlNbr = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return railroadCtrlNbr is not null
            ? await uow.Bulletins.GetActiveByRailroadAsync(railroadCtrlNbr)
            : await uow.Bulletins.GetActiveAsync();
    }

    public async Task<IReadOnlyList<Bulletin>> GetPostedBulletinsByCraftAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Bulletins.GetPostedByCraftAsync(craftCtrlNbr);
    }

    public async Task<Bulletin?> GetBulletinByVacancyAsync(ControlNumber vacancyCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Bulletins.GetByVacancyAsync(vacancyCtrlNbr);
    }

    public async Task<Bulletin> GetBulletinAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Bulletins.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Bulletin {ctrlNbr} not found.");
    }

    public async Task<BulletinBid> SubmitBidAsync(long bulletinCtrlNbr, long employeeCtrlNbr, int priority, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var seniorityEntries = await uow.Seniority.GetByEmployeeCtrlNbrAsync(ControlNumber.Create(employeeCtrlNbr));
        var seniorityRank = seniorityEntries.FirstOrDefault(s => s.LastActiveRoster)?.Rank ?? 0;
        var bid = BulletinBid.Create(bulletinCtrlNbr, employeeCtrlNbr, priority, seniorityRank);
        await uow.BulletinBids.AddAsync(bid, ct);
        await uow.CommitAsync(ct);
        return bid;
    }

    public async Task<BulletinBid> WithdrawBidAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bid = await uow.BulletinBids.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Bid {ctrlNbr} not found.");
        bid.Withdraw();
        await uow.BulletinBids.UpdateAsync(bid, ct);
        await uow.CommitAsync(ct);
        return bid;
    }

    public async Task<IReadOnlyList<BulletinBid>> GetBidsByBulletinAsync(ControlNumber bulletinCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.BulletinBids.GetByBulletinAsync(bulletinCtrlNbr);
    }

    public async Task<IReadOnlyList<BulletinBid>> GetBidsByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.BulletinBids.GetByEmployeeAsync(employeeCtrlNbr);
    }

    public async Task<Bulletin> AwardBulletinAsync(ControlNumber ctrlNbr, ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bulletin = await uow.Bulletins.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Bulletin {ctrlNbr} not found.");
        bulletin.Award(employeeCtrlNbr);
        await FillBulletinAsync(uow, bulletin, employeeCtrlNbr, PositionAssignmentType.BulletinAssignment, ct);
        await uow.CommitAsync(ct);
        return bulletin;
    }

    /// <summary>
    /// Force assigns a NoBid bulletin. If <paramref name="overrideEmployee"/> is provided the
    /// dispatcher's explicit choice is used; otherwise the selection rules on the craft's
    /// BulletinRule are run to find the most junior eligible employee automatically.
    /// </summary>
    public async Task<Bulletin> ForceAssignBulletinAsync(ControlNumber ctrlNbr, ControlNumber? overrideEmployee = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bulletin = await uow.Bulletins.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Bulletin {ctrlNbr} not found.");
        if (bulletin.Status != "NoBid")
            throw new InvalidOperationException($"Cannot force assign bulletin {ctrlNbr}: status is '{bulletin.Status}'. Only NoBid bulletins can be force assigned.");

        ControlNumber? candidateCtrlNbr = overrideEmployee;
        if (candidateCtrlNbr is null)
        {
            var rule = await uow.BulletinRules.GetByCraftAsync(bulletin.CraftCtrlNbr)
                ?? throw new InvalidOperationException($"No BulletinRule configured for craft {bulletin.CraftCtrlNbr}.");
            candidateCtrlNbr = rule.ForceAssignSelectionMode switch
            {
                Domain.Modules.Bulletins.ForceAssignSelectionMode.JuniorHelperOrExtraBoard =>
                    await SelectJuniorHelperOrExtraBoardAsync(uow, bulletin, ct),
                _ =>
                    await SelectJuniorExtraBoardAsync(uow, bulletin, ct)
            };
            if (candidateCtrlNbr is null)
                throw new InvalidOperationException("No eligible candidate found for force assignment. Ensure extra board members exist for this craft.");
        }

        bulletin.ForceAssign(candidateCtrlNbr);
        await FillBulletinAsync(uow, bulletin, candidateCtrlNbr, PositionAssignmentType.ForceAssignment, ct);
        await uow.CommitAsync(ct);
        return bulletin;
    }
    public async Task<Bulletin> SetBulletinNoBidAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bulletin = await uow.Bulletins.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Bulletin {ctrlNbr} not found.");

        var activeBids = await uow.BulletinBids.GetByBulletinAsync(ctrlNbr);
        if (activeBids.Any(b => b.Status == "Submitted"))
            throw new InvalidOperationException("Cannot mark a bulletin as No Bid when there are active bids. Withdraw all bids first.");

        // For crew-position bulletins, compute a schedule-aware force-assign deadline.
        DateTime? forceAssignDeadline = null;
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        if (vacancy?.TargetType == StaffablePositionType.Crew)
        {
            var rule = await uow.BulletinRules.GetByCraftAsync(bulletin.CraftCtrlNbr);
            if (rule is not null)
                forceAssignDeadline = await CalculateScheduleAwareForceAssignDeadlineAsync(uow, bulletin, vacancy, rule, ct);
        }

        bulletin.SetAsNoBid(forceAssignDeadline);
        await uow.Bulletins.UpdateAsync(bulletin, ct);
        await uow.CommitAsync(ct);
        if (forceAssignDeadline.HasValue)
            scheduleSignal.Notify(forceAssignDeadline.Value);
        return bulletin;
    }

    // ── Vacancy creation ──────────────────────────────────────────────

    /// <summary>
    /// Primary path: opens a vacancy and immediately posts a bulletin using the
    /// craft's BulletinRule for timing. Used when a position becomes vacant due
    /// to an incumbent move or new position creation.
    /// </summary>
    public async Task<(PositionVacancy Vacancy, Bulletin Bulletin)> OpenVacancyAsync(
        ControlNumber workAreaGroupCtrlNbr,
        string targetType,
        ControlNumber targetCtrlNbr,
        ControlNumber craftCtrlNbr,
        string vacancyReasonCode,
        ControlNumber? previousIncumbentCtrlNbr = null,
        string targetName = "",
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var rule = await uow.BulletinRules.GetByCraftAsync(craftCtrlNbr)
            ?? throw new InvalidOperationException(
                $"No BulletinRule configured for craft {craftCtrlNbr}. Configure one before opening vacancies.");

        var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(workAreaGroupCtrlNbr);
        var tz = ResolveTimeZone(workArea?.TimeZoneId);

        var vacancy = PositionVacancy.Create(
            workAreaGroupCtrlNbr, targetType, targetCtrlNbr,
            craftCtrlNbr, vacancyReasonCode, previousIncumbentCtrlNbr,
            targetName);

        var (opens, closes, effective) = rule.CalculateBidWindow(DateTime.UtcNow, tz);
        var bulletin = Bulletin.Create(vacancy.CtrlNbr, craftCtrlNbr, opens, closes, effective);

        vacancy.MarkBulletined();

        await uow.PositionVacancies.AddAsync(vacancy, ct);
        await uow.Bulletins.AddAsync(bulletin, ct);
        await uow.CommitAsync(ct);

        scheduleSignal.Notify(closes);
        return (vacancy, bulletin);
    }

    /// <summary>
    /// Cutover path: registers a vacancy without posting a bulletin.
    /// Used during railroad implementation when positions exist before go-live.
    /// Use PostBulletinForVacancyAsync later to bulletin these positions.
    /// </summary>
    public async Task<PositionVacancy> RegisterVacancyNoBulletinAsync(
        ControlNumber workAreaGroupCtrlNbr,
        string targetType,
        ControlNumber targetCtrlNbr,
        ControlNumber craftCtrlNbr,
        string vacancyReasonCode,
        ControlNumber? previousIncumbentCtrlNbr = null,
        string targetName = "",
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var vacancy = PositionVacancy.Create(
            workAreaGroupCtrlNbr, targetType, targetCtrlNbr,
            craftCtrlNbr, vacancyReasonCode, previousIncumbentCtrlNbr,
            targetName);

        await uow.PositionVacancies.AddAsync(vacancy, ct);
        await uow.CommitAsync(ct);

        return vacancy;
    }

    /// <summary>
    /// Posts a bulletin for an existing unbulletined vacancy (cutover exception path).
    /// </summary>
    public async Task<Bulletin> PostBulletinForVacancyAsync(
        ControlNumber vacancyCtrlNbr,
        DateTime bidWindowOpensUtc,
        DateTime bidWindowClosesUtc,
        DateTime effectiveUtc,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(vacancyCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Vacancy {vacancyCtrlNbr} not found.");

        if (vacancy.Status != "Open")
            throw new InvalidOperationException($"Vacancy {vacancyCtrlNbr} is not open (status: {vacancy.Status}).");

        var existing = await uow.Bulletins.GetByVacancyAsync(vacancyCtrlNbr);
        if (existing is not null)
            throw new InvalidOperationException($"Vacancy {vacancyCtrlNbr} already has a bulletin.");

        var bulletin = Bulletin.Create(vacancyCtrlNbr, vacancy.CraftCtrlNbr,
            bidWindowOpensUtc, bidWindowClosesUtc, effectiveUtc);
        vacancy.MarkBulletined();

        await uow.Bulletins.AddAsync(bulletin, ct);
        await uow.PositionVacancies.UpdateAsync(vacancy, ct);
        await uow.CommitAsync(ct);

        scheduleSignal.Notify(bidWindowClosesUtc);
        return bulletin;
    }

    // ── WorkArea-scoped queries

    public async Task<IReadOnlyList<PositionVacancy>> GetVacanciesByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PositionVacancies.GetByWorkAreaAsync(workAreaGroupCtrlNbr);
    }

    public async Task<IReadOnlyList<Bulletin>> GetBulletinsByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Bulletins.GetByWorkAreaAsync(workAreaGroupCtrlNbr);
    }

    // ── BulletinRule management ───────────────────────────────────────

    public async Task<BulletinRule> GetBulletinRuleAsync(
        ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.BulletinRules.GetByCraftAsync(craftCtrlNbr)
            ?? throw new KeyNotFoundException($"No BulletinRule for craft {craftCtrlNbr}.");
    }

    public async Task<BulletinRule> SaveBulletinRuleAsync(
        ControlNumber craftCtrlNbr,
        int bidWindowHours,
        TimeSpan bidWindowStartTime,
        TimeSpan bidWindowCloseTime,
        int effectiveOffsetDays,
        TimeSpan effectiveTime,
        int forceAssignHours,
        string forceAssignSelectionMode = Domain.Modules.Bulletins.ForceAssignSelectionMode.JuniorExtraBoard,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var existing = await uow.BulletinRules.GetByCraftAsync(craftCtrlNbr);
        if (existing is not null)
        {
            existing.Update(bidWindowHours, bidWindowStartTime, bidWindowCloseTime,
                effectiveOffsetDays, effectiveTime, forceAssignHours, forceAssignSelectionMode);
            await uow.BulletinRules.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }

        var rule = BulletinRule.Create(craftCtrlNbr, bidWindowHours, bidWindowStartTime,
            bidWindowCloseTime, effectiveOffsetDays, effectiveTime, forceAssignHours, forceAssignSelectionMode);
        await uow.BulletinRules.AddAsync(rule, ct);
        await uow.CommitAsync(ct);
        return rule;
    }

    // ── Automated no-bid force assignment ─────────────────────────────

    /// <summary>
    /// Scans all crew bulletins in NoBid status whose ForceAssignDeadlineUtc has passed
    /// and automatically selects a candidate using the railroad-specific selection mode
    /// defined on the craft's BulletinRule.
    /// </summary>
    public async Task<IReadOnlyList<Bulletin>> AutoForceAssignNoBidsAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var overdue = await uow.Bulletins.GetNoBidPastDeadlineAsync(ct);
        var assigned = new List<Bulletin>();

        foreach (var bulletin in overdue)
        {
            var rule = await uow.BulletinRules.GetByCraftAsync(bulletin.CraftCtrlNbr);
            if (rule is null) continue;

            ControlNumber? candidateCtrlNbr = rule.ForceAssignSelectionMode switch
            {
                Domain.Modules.Bulletins.ForceAssignSelectionMode.JuniorHelperOrExtraBoard =>
                    await SelectJuniorHelperOrExtraBoardAsync(uow, bulletin, ct),
                _ =>
                    await SelectJuniorExtraBoardAsync(uow, bulletin, ct)
            };

            if (candidateCtrlNbr is null) continue;

            bulletin.ForceAssign(candidateCtrlNbr);
            await FillBulletinAsync(uow, bulletin, candidateCtrlNbr, PositionAssignmentType.ForceAssignment, ct);
            assigned.Add(bulletin);
        }

        if (assigned.Count > 0)
            await uow.CommitAsync(ct);

        return assigned;
    }

    /// <summary>
    /// Returns the earliest UTC time the bulletin worker needs to wake up to process a pending
    /// event — whichever comes first among bid-window closes (Posted bulletins) and force-assign
    /// deadlines (NoBid bulletins). Returns <c>null</c> when there are no pending events.
    /// </summary>
    public async Task<DateTime?> GetNextBulletinEventUtcAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Bulletins.GetNextPendingEventUtcAsync(ct);
    }

    /// <summary>
    /// Returns the next pending bulletin event time (UTC) and the work-area ctrl nbr of the
    /// bulletin that drives it, so the caller can convert to the correct local timezone.
    /// </summary>
    public async Task<(DateTime? NextUtc, long? WorkAreaCtrlNbr)> GetNextBulletinEventAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bulletin = await uow.Bulletins.GetNextPendingEventBulletinAsync(ct);
        if (bulletin is null) return (null, null);
        var eventUtc = bulletin.Status == "NoBid" ? bulletin.ForceAssignDeadlineUtc : (DateTime?)bulletin.BidWindowClosesUtc;
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        return (eventUtc, vacancy?.WorkAreaGroupCtrlNbr.Value);
    }

    /// <summary>
    /// Selects the youngest (most junior) employee on the extra board for the bulletin's craft.
    /// Mirrors the legacy default force-assign pool.
    /// </summary>
    private static async Task<ControlNumber?> SelectJuniorExtraBoardAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        Bulletin bulletin,
        CancellationToken ct)
    {
        var boards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(bulletin.CraftCtrlNbr, ct);
        var extraBoards = boards.Where(b => b.BoardType == Domain.Modules.Boards.BoardType.ExtraBoard && b.IsActive).ToList();
        if (extraBoards.Count == 0) return null;

        // All positions on active extra boards for this craft — youngest by RosterDate desc, then Rank desc
        var allPositions = extraBoards.SelectMany(b => b.Positions).ToList();
        if (allPositions.Count == 0) return null;

        var seniorityByEmployee = new Dictionary<ControlNumber, Domain.Models.Seniority.Seniority>();
        foreach (var pos in allPositions)
        {
            var entries = await uow.Seniority.GetByEmployeeCtrlNbrAsync(pos.EmployeeCtrlNbr);
            var relevant = entries.FirstOrDefault(s => s.LastActiveRoster);
            if (relevant is not null && !seniorityByEmployee.ContainsKey(pos.EmployeeCtrlNbr))
                seniorityByEmployee[pos.EmployeeCtrlNbr] = relevant;
        }

        return seniorityByEmployee.Values
            .OrderByDescending(s => s.RosterDate)
            .ThenByDescending(s => s.Rank)
            .FirstOrDefault()?.EmployeeCtrlNbr;
    }

    /// <summary>
    /// Selects the youngest employee who is either on the extra board OR currently holding
    /// an active Helper crew incumbency. Used for Foreman no-bid force assignment.
    /// Mirrors the legacy "case Foreman" pool from Collections.GetForceAssignmentSeniorityList.
    /// </summary>
    private static async Task<ControlNumber?> SelectJuniorHelperOrExtraBoardAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        Bulletin bulletin,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var candidateCtrlNbrs = new HashSet<ControlNumber>();

        // 1. Extra board members for this craft
        var boards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(bulletin.CraftCtrlNbr, ct);
        foreach (var board in boards.Where(b => b.BoardType == Domain.Modules.Boards.BoardType.ExtraBoard && b.IsActive))
            foreach (var pos in board.Positions)
                candidateCtrlNbrs.Add(pos.EmployeeCtrlNbr);

        // 2. Employees currently occupying an active Helper crew incumbency
        //    (a Helper CrewPosition has a TargetType == StaffablePositionType.Crew and craft == helper craft)
        //    We look for all active incumbencies whose position belongs to a crew scoped to the same railroad.
        //    Since we don't have a direct "Helper craft" reference here, we use the vacancy's WorkAreaGroupCtrlNbr
        //    to scope crew searches, then filter by craft role / position type if available.
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        if (vacancy is not null)
        {
            // Load all craft roles for this craft so we can identify helper vs foreman roles
            var craftRoles = await uow.CraftRoles.GetByCraftAsync(bulletin.CraftCtrlNbr);
            var helperRoleCtrlNbrs = craftRoles
                .Where(r => r.Name.Contains("Helper", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.CtrlNbr)
                .ToHashSet();

            if (helperRoleCtrlNbrs.Count > 0)
            {
                var crewsInWorkArea = await uow.Crews.GetByWorkAreaAsync(vacancy.WorkAreaGroupCtrlNbr);
                foreach (var crew in crewsInWorkArea)
                {
                    var positions = await uow.CrewPositions.GetByCrewAsync(crew.CtrlNbr);
                    foreach (var position in positions)
                    {
                        // Only include helper role positions
                        if (!helperRoleCtrlNbrs.Contains(position.CraftRoleCtrlNbr)) continue;

                        var incumbency = await uow.CrewIncumbencies.GetActiveByPositionAsync(position.CtrlNbr, now);
                        if (incumbency is not null)
                            candidateCtrlNbrs.Add(incumbency.EmployeeCtrlNbr);
                    }
                }
            }
        }

        if (candidateCtrlNbrs.Count == 0) return null;

        // Select youngest by reverse seniority (RosterDate desc, Rank desc)
        var seniorityByEmployee = new Dictionary<ControlNumber, Domain.Models.Seniority.Seniority>();
        foreach (var empCtrlNbr in candidateCtrlNbrs)
        {
            var entries = await uow.Seniority.GetByEmployeeCtrlNbrAsync(empCtrlNbr);
            var relevant = entries.FirstOrDefault(s => s.LastActiveRoster);
            if (relevant is not null)
                seniorityByEmployee[empCtrlNbr] = relevant;
        }

        return seniorityByEmployee.Values
            .OrderByDescending(s => s.RosterDate)
            .ThenByDescending(s => s.Rank)
            .FirstOrDefault()?.EmployeeCtrlNbr;
    }

    /// <summary>
    /// Attempts to resolve a <see cref="TimeZoneInfo"/> from the given IANA or Windows zone id.
    /// Returns <c>null</c> if the id is blank or unrecognised, which causes callers to fall
    /// back to naive UTC arithmetic.
    /// </summary>
    /// <summary>
    /// If the vacancy targets a crew position (TargetType == Crew), creates a
    /// CrewIncumbency and PositionAssignment for the awarded employee within the
    /// supplied unit-of-work. Safe to call inside any existing transaction.
    /// </summary>
    // Fills the bulletin vacancy, persists the update, and (for crew positions) creates the crew incumbency.
    private async Task FillBulletinAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        Bulletin bulletin,
        ControlNumber employeeCtrlNbr,
        string assignmentType,
        CancellationToken ct)
    {
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        if (vacancy is not null)
        {
            vacancy.Fill();
            await uow.PositionVacancies.UpdateAsync(vacancy, ct);
            await PlaceEmployeeOnCrewPositionAsync(uow, vacancy, employeeCtrlNbr, assignmentType, ct);
        }
        await uow.Bulletins.UpdateAsync(bulletin, ct);
    }

    private static async Task PlaceEmployeeOnCrewPositionAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        PositionVacancy vacancy,
        ControlNumber employeeCtrlNbr,
        string assignmentType,
        CancellationToken ct)
    {
        if (vacancy.TargetType != StaffablePositionType.Crew) return;

        var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(vacancy.TargetCtrlNbr);
        if (crewPosition is null) return;

        // End any existing active incumbency on this position first.
        var existingIncumbency = await uow.CrewIncumbencies.GetActiveByPositionAsync(crewPosition.CtrlNbr, DateTime.UtcNow);
        if (existingIncumbency is not null)
        {
            existingIncumbency.End(DateTime.UtcNow);
            uow.CrewIncumbencies.Update(existingIncumbency);
            var oldAssignment = await uow.PositionAssignments.GetByStaffablePositionAsync(crewPosition.StaffablePositionCtrlNbr);
            if (oldAssignment is not null) uow.PositionAssignments.Remove(oldAssignment);
        }

        var incumbency = CrewIncumbency.Create(crewPosition.CtrlNbr.Value, employeeCtrlNbr.Value, DateTime.UtcNow, null);
        var positionAssignment = PositionAssignment.Create(
            crewPosition.StaffablePositionCtrlNbr, employeeCtrlNbr, assignmentType, crewPosition.CtrlNbr);

        uow.CrewIncumbencies.Add(incumbency);
        uow.PositionAssignments.Add(positionAssignment);
    }
    /// <summary>
    /// Computes the force-assign deadline for a crew-position bulletin using the
    /// position's actual work schedule (AssignmentSchedule.OnDutyTime and OperatingDaysMask),
    /// mirroring the legacy AssignDateTime logic. Falls back to a flat offset if schedule
    /// data is unavailable.
    /// </summary>
    private static async Task<DateTime?> CalculateScheduleAwareForceAssignDeadlineAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        Bulletin bulletin,
        PositionVacancy vacancy,
        BulletinRule rule,
        CancellationToken ct)
    {
        // Try to get the crew position's schedule via: vacancy → crewPosition → crew → crewAssignment → assignment → schedule
        var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(vacancy.TargetCtrlNbr);
        if (crewPosition is not null)
        {
            var crewAssignments = await uow.CrewAssignments.GetByCrewAsync(crewPosition.CrewCtrlNbr);
            var activeCrewAssignment = crewAssignments
                .Where(ca => ca.StartUtc <= DateTime.UtcNow && (ca.EndUtc is null || ca.EndUtc > DateTime.UtcNow))
                .FirstOrDefault();

            if (activeCrewAssignment is not null)
            {
                var schedules = await uow.AssignmentSchedules.GetByAssignmentAsync(activeCrewAssignment.AssignmentCtrlNbr);
                var schedule = schedules.FirstOrDefault();
                if (schedule is not null)
                {
                    // Find the next work date on or after the effective date using the OperatingDaysMask
                    var effectiveLocal = bulletin.EffectiveUtc.Date;
                    DateTime nextWorkDate = effectiveLocal;
                    for (int i = 0; i < 14; i++)
                    {
                        int dayBit = 1 << (int)nextWorkDate.DayOfWeek;
                        if ((schedule.OperatingDaysMask & dayBit) != 0) break;
                        nextWorkDate = nextWorkDate.AddDays(1);
                    }

                    // The force-assign deadline = first on-duty time on the next work day minus ForceAssignHours
                    var onDutyUtc = new DateTime(nextWorkDate.Year, nextWorkDate.Month, nextWorkDate.Day,
                        schedule.OnDutyTime.Hour, schedule.OnDutyTime.Minute, schedule.OnDutyTime.Second,
                        DateTimeKind.Utc);
                    return onDutyUtc.AddHours(-rule.ForceAssignHours);
                }
            }
        }

        // Fallback: flat offset from effective date
        return rule.CalculateForceAssignDeadline(bulletin.EffectiveUtc);
    }

    /// <summary>
    /// Selects the winning bidder for a closed bulletin using seniority order (senior first),
    /// respecting bid <c>Priority</c> preference chains. If an employee bids on this bulletin
    /// at a lower priority than another bulletin, that higher-preference bulletin is checked
    /// first; if the employee already won their preferred bulletin they are skipped here.
    /// Employees who have lost qualification since submitting their bid are skipped with a
    /// warning log rather than a hard error.
    /// Returns the winning <see cref="BulletinBid"/>, or <c>null</c> if no qualified bidder exists.
    /// </summary>
    private async Task<BulletinBid?> SelectBulletinWinnerAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        Bulletin bulletin,
        CancellationToken ct)
    {
        var bids = await uow.BulletinBids.GetByBulletinAsync(bulletin.CtrlNbr);
        var activeBids = bids
            .Where(b => b.Status == "Submitted")
            .OrderBy(b => b.SeniorityRank)
            .ToList();

        if (activeBids.Count == 0) return null;

        // Load required qualification types for this craft once
        var requiredQualTypes = await uow.QualificationTypes.GetActiveByCraftCtrlNbrAsync(bulletin.CraftCtrlNbr);
        var requiredQualTypeCtrlNbrs = requiredQualTypes
            .Where(q => q.IsBlocking)
            .Select(q => q.CtrlNbr)
            .ToHashSet();

        foreach (var bid in activeBids)
        {
            // Qualification safety net: check the employee still has all required qualifications
            if (requiredQualTypeCtrlNbrs.Count > 0)
            {
                var empQuals = await uow.EmployeeQualifications.GetActiveByEmployeeCtrlNbrAsync(bid.EmployeeCtrlNbr);
                var empQualTypeCtrlNbrs = empQuals.Select(q => q.QualificationTypeCtrlNbr).ToHashSet();
                var missing = requiredQualTypeCtrlNbrs.Except(empQualTypeCtrlNbrs).ToList();
                if (missing.Count > 0)
                {
                    logger.LogWarning(
                        "Bulletin {BulletinCtrlNbr}: Employee {EmployeeCtrlNbr} bid skipped at award time — " +
                        "lost required qualification(s) [{MissingQuals}] since bid was submitted.",
                        bulletin.CtrlNbr, bid.EmployeeCtrlNbr, string.Join(", ", missing));
                    continue;
                }
            }

            // Preference chain: if this bid has Priority > 1, check whether the employee has
            // a higher-priority (lower Priority number) bid on another bulletin that is ready
            // to award. If so, process that one first; if it awards to this employee, skip here.
            if (bid.Priority > 1)
            {
                var higherPriorityBids = await uow.BulletinBids.GetActiveByEmployeeAsync(bid.EmployeeCtrlNbr);
                var higherPref = higherPriorityBids
                    .Where(b => b.BulletinCtrlNbr != bulletin.CtrlNbr && b.Priority < bid.Priority && b.Status == "Submitted")
                    .ToList();

                foreach (var prefBid in higherPref)
                {
                    var prefBulletin = await uow.Bulletins.GetByCtrlNbrAsync(prefBid.BulletinCtrlNbr, ct);
                    if (prefBulletin is null) continue;
                    // Only process if that bulletin's bid window is also closed and not yet awarded
                    if (prefBulletin.BidWindowClosesUtc > DateTime.UtcNow) continue;
                    if (prefBulletin.AwardedEmployeeCtrlNbr is not null) continue;

                    // Recursively try to award the higher-preference bulletin
                    var prefWinner = await SelectBulletinWinnerAsync(uow, prefBulletin, ct);
                    if (prefWinner is not null && prefWinner.EmployeeCtrlNbr == bid.EmployeeCtrlNbr)
                    {
                        // Employee won their preferred bulletin — award it and skip them here
                        prefBulletin.Award(bid.EmployeeCtrlNbr);
                        await FillBulletinAsync(uow, prefBulletin, bid.EmployeeCtrlNbr, PositionAssignmentType.BulletinAssignment, ct);
                        logger.LogInformation(
                            "Bulletin {BulletinCtrlNbr}: Employee {EmployeeCtrlNbr} won higher-preference bulletin {PreferredBulletinCtrlNbr} — skipped on this bulletin.",
                            bulletin.CtrlNbr, bid.EmployeeCtrlNbr, prefBulletin.CtrlNbr);
                        goto nextBid;
                    }
                }
            }

            return bid;

            nextBid:;
        }

        return null;
    }

    /// <summary>
    /// Processes all Posted bulletins whose bid window has closed and that have not yet been
    /// awarded. For each: selects a winner, awards the position, or transitions to NoBid if
    /// no qualified bidder exists. Returns the list of bulletins that were acted on.
    /// </summary>
    public async Task<IReadOnlyList<Bulletin>> AutoAwardClosedBulletinsAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var candidates = await uow.Bulletins.GetClosedUnawardedAsync(ct);
        var processed = new List<Bulletin>();

        // Process in effective-date ascending order — seniority/preference chains work best
        // when earlier-effective bulletins are resolved first.
        foreach (var bulletin in candidates.OrderBy(b => b.EffectiveUtc))
        {
            // Skip bulletins already acted on in this batch (e.g., awarded via preference chain)
            if (bulletin.AwardedEmployeeCtrlNbr is not null || bulletin.Status == "NoBid") continue;

            var winner = await SelectBulletinWinnerAsync(uow, bulletin, ct);
            if (winner is not null)
            {
                winner.MarkWinner();
                await uow.BulletinBids.UpdateAsync(winner, ct);

                // Mark all other bids as losers
                var allBids = await uow.BulletinBids.GetByBulletinAsync(bulletin.CtrlNbr);
                foreach (var loserBid in allBids.Where(b => b.CtrlNbr != winner.CtrlNbr && b.Status == "Submitted"))
                {
                    loserBid.MarkLoser();
                    await uow.BulletinBids.UpdateAsync(loserBid, ct);
                }

                bulletin.Award(winner.EmployeeCtrlNbr);
                await FillBulletinAsync(uow, bulletin, winner.EmployeeCtrlNbr, PositionAssignmentType.BulletinAssignment, ct);
                logger.LogInformation(
                    "Bulletin {BulletinCtrlNbr}: Auto-awarded to employee {EmployeeCtrlNbr}.",
                    bulletin.CtrlNbr, winner.EmployeeCtrlNbr);
            }
            else
            {
                // No qualified winner — transition to NoBid with schedule-aware force-assign deadline
                var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
                DateTime? forceAssignDeadline = null;
                if (vacancy?.TargetType == StaffablePositionType.Crew)
                {
                    var rule = await uow.BulletinRules.GetByCraftAsync(bulletin.CraftCtrlNbr);
                    if (rule is not null)
                        forceAssignDeadline = await CalculateScheduleAwareForceAssignDeadlineAsync(uow, bulletin, vacancy, rule, ct);
                }

                bulletin.SetAsNoBid(forceAssignDeadline);
                await uow.Bulletins.UpdateAsync(bulletin, ct);
                logger.LogInformation(
                    "Bulletin {BulletinCtrlNbr}: No qualified winner — transitioned to NoBid. Force-assign deadline: {Deadline}.",
                    bulletin.CtrlNbr, forceAssignDeadline?.ToString("u") ?? "none");
            }

            processed.Add(bulletin);
        }

        if (processed.Count > 0)
        {
            await uow.CommitAsync(ct);
            // Notify the worker of any newly assigned force-assign deadlines so it wakes precisely.
            foreach (var b in processed.Where(b => b.Status == "NoBid" && b.ForceAssignDeadlineUtc.HasValue))
                scheduleSignal.Notify(b.ForceAssignDeadlineUtc!.Value);
        }

        return processed;
    }

    private static TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return null;
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { return null; }
    }
}
