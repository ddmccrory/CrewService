using CrewService.Application.BackgroundWorkers;
using CrewService.Application.Notifications;
using CrewService.Application.Qualifications;
using CrewService.Application.TenantConfig;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.Bulletins;

public sealed class BulletinsService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    ILogger<BulletinsService> logger,
    IBulletinScheduleSignal scheduleSignal,
    EmployeeNotificationService notifications,
    IRailroadResolver railroadResolver,
    EmployeeEligibilityService eligibility)
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

    public async Task<IReadOnlyList<Bulletin>> GetBulletinsInDateRangeAsync(DateTime fromUtc, ControlNumber? railroadCtrlNbr = null, bool employeeScoped = false, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bulletins = await uow.Bulletins.GetInDateRangeAsync(fromUtc, railroadCtrlNbr);
        // Employees must not see bulletins whose bid window has not opened yet (legacy parity).
        // Past/closed bulletins remain visible in history; only not-yet-open ones are hidden.
        if (employeeScoped)
        {
            var now = DateTime.UtcNow;
            bulletins = [.. bulletins.Where(b => b.HasBidWindowOpened(now))];
        }
        return bulletins;
    }

        public async Task<IReadOnlyList<Bulletin>> GetActiveBulletinsAsync(ControlNumber? railroadCtrlNbr = null, bool employeeScoped = false, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bulletins = railroadCtrlNbr is not null
            ? await uow.Bulletins.GetActiveByRailroadAsync(railroadCtrlNbr)
            : await uow.Bulletins.GetActiveAsync();
        // Employees must not see bulletins whose bid window has not opened yet (legacy parity:
        // SA employee bulletin queries require Now > OpenDateTime). Dispatchers see all.
        if (employeeScoped)
        {
            var now = DateTime.UtcNow;
            bulletins = [.. bulletins.Where(b => b.HasBidWindowOpened(now))];
        }
        return bulletins;
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

        // Enforce the bid-window gate. Employees cannot see or bid on a bulletin until its window
        // opens, and cannot bid after it closes (legacy parity: SA's employee bulletin collection
        // queries require Now > OpenDateTime && Now <= CloseDateTime). This is the authoritative
        // server-side guard; the UI only mirrors it for presentation.
        var bulletin = await uow.Bulletins.GetByCtrlNbrAsync(ControlNumber.Create(bulletinCtrlNbr), ct)
            ?? throw new KeyNotFoundException($"Bulletin {bulletinCtrlNbr} not found.");
        var now = DateTime.UtcNow;
        if (bulletin.Status != "Posted")
            throw new InvalidOperationException(
                $"Cannot bid on bulletin {bulletinCtrlNbr}: status is '{bulletin.Status}'. Only posted bulletins accept bids.");
        if (now < bulletin.BidWindowOpensUtc)
            throw new InvalidOperationException(
                $"Cannot bid on bulletin {bulletinCtrlNbr}: the bid window has not opened yet.");
        if (now > bulletin.BidWindowClosesUtc)
            throw new InvalidOperationException(
                $"Cannot bid on bulletin {bulletinCtrlNbr}: the bid window has closed.");

        // Enforce board-level bulletin bidding restriction.
        // If the employee is currently assigned to a roster board position, check AllowBulletinBidding.
        // Crew positions are always permitted to bid.
        var empCtrlNbr = ControlNumber.Create(employeeCtrlNbr);
        var positionAssignments = await uow.PositionAssignments.GetByEmployeeAsync(empCtrlNbr);
        foreach (var pa in positionAssignments)
        {
            var pos = await uow.StaffablePositions.GetByCtrlNbrAsync(pa.StaffablePositionCtrlNbr, ct);
            if (pos?.PositionType != StaffablePositionType.Board) continue;
            if (pa.AssignmentSourceCtrlNbr is null) continue;
            var board = await uow.RosterBoards.GetByPositionCtrlNbrAsync(pa.AssignmentSourceCtrlNbr, ct);
            if (board is not null && !board.AllowBulletinBidding)
                throw new InvalidOperationException(
                    $"Employees on the '{board.Name}' board are not permitted to bid on bulletins.");
        }

        var seniorityEntries = await uow.Seniority.GetByEmployeeCtrlNbrAsync(empCtrlNbr);
        var activeEntry = seniorityEntries.FirstOrDefault(s => s.LastActiveRoster);
        var seniorityDate = activeEntry?.RosterDate ?? DateTime.MinValue;
        var seniorityRank = activeEntry?.Rank ?? 0;
        var bid = BulletinBid.Create(bulletinCtrlNbr, employeeCtrlNbr, priority, seniorityDate, seniorityRank);
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
        // Mark any other submitted bids for this employee on other bulletins as Loser (cross-bulletin resolution)
        await MarkOtherEmployeeBidsAsLoserAsync(uow, ctrlNbr, employeeCtrlNbr, ct);
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
            var candidates = await SelectForceAssignCandidatesAsync(uow, bulletin, rule, ct);
            candidateCtrlNbr = candidates.FirstOrDefault();
            if (candidateCtrlNbr is null)
                throw new InvalidOperationException("No eligible candidate found for force assignment. Ensure qualified extra board or subordinate-tier members exist for this craft.");
        }

        bulletin.ForceAssign(candidateCtrlNbr);
        await FillBulletinAsync(uow, bulletin, candidateCtrlNbr, PositionAssignmentType.ForceAssignment, ct);
        // Mark any other submitted bids for this employee on other bulletins as Loser (cross-bulletin resolution)
        await MarkOtherEmployeeBidsAsLoserAsync(uow, ctrlNbr, candidateCtrlNbr, ct);
        await uow.CommitAsync(ct);
        return bulletin;
    }

    /// <summary>
    /// Previews the force-assign candidate for a NoBid bulletin without committing any changes.
    /// Returns the candidate's ControlNumber, or null if no eligible candidate exists.
    /// </summary>
    public async Task<ControlNumber?> GetForceAssignCandidateAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bulletin = await uow.Bulletins.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Bulletin {ctrlNbr} not found.");

        var rule = await uow.BulletinRules.GetByCraftAsync(bulletin.CraftCtrlNbr);
        if (rule is null) return null;

        var candidates = await SelectForceAssignCandidatesAsync(uow, bulletin, rule, ct);
        return candidates.FirstOrDefault();
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

        // Notify the prospective force-assign candidate that the bulletin closed with no bid.
        var noBidRule = await uow.BulletinRules.GetByCraftAsync(bulletin.CraftCtrlNbr);
        if (noBidRule is not null)
        {
            var candidates = await SelectForceAssignCandidatesAsync(uow, bulletin, noBidRule, ct);
            var candidate = candidates.FirstOrDefault();
            if (candidate is not null)
                await notifications.NotifyBulletinNoBidAsync(uow, bulletin, candidate, ct);
        }

        await uow.CommitAsync(ct);
        if (forceAssignDeadline.HasValue)
            scheduleSignal.Notify(forceAssignDeadline.Value);
        return bulletin;
    }

    /// <summary>
    /// Cancels a posted bulletin while its bid window is still open and it has not been awarded.
    /// Mirrors legacy SA bulletin deletion (<c>RemoveRailroadPositionBulletins</c>): outstanding
    /// bids are withdrawn and the underlying <see cref="PositionVacancy"/> is reopened so the
    /// position survives un-bulletined (no worker auto-reposts it). The bulletin record is kept
    /// in <c>Cancelled</c> status for audit rather than hard-deleted. Bidder notifications are
    /// handled separately by the notification pipeline.
    /// </summary>
    public async Task<Bulletin> CancelBulletinAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bulletin = await uow.Bulletins.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Bulletin {ctrlNbr} not found.");

        if (!bulletin.CanCancel(DateTime.UtcNow))
            throw new InvalidOperationException(
                $"Cannot cancel bulletin {ctrlNbr}: status is '{bulletin.Status}' and the bid window must still be open and the bulletin un-awarded.");

        // Withdraw any outstanding bids so they no longer compete for the (now reopened) position.
        var bids = await uow.BulletinBids.GetByBulletinAsync(ctrlNbr);
        foreach (var bid in bids.Where(b => b.Status == "Submitted"))
        {
            bid.Withdraw();
            await uow.BulletinBids.UpdateAsync(bid, ct);
            // Notify each bidder their bulletin was cancelled (legacy RemoveRailroadPositionBulletin fan-out).
            await notifications.NotifyBulletinCancelledAsync(uow, bulletin, bid.EmployeeCtrlNbr, ct);
        }

        bulletin.Cancel();
        await uow.Bulletins.UpdateAsync(bulletin, ct);

        // Reopen the vacancy so the position is re-postable (legacy leaves it unbulletined).
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        if (vacancy is not null)
        {
            vacancy.Reopen();
            await uow.PositionVacancies.UpdateAsync(vacancy, ct);
        }

        await uow.CommitAsync(ct);
        logger.LogInformation(
            "Bulletin {BulletinCtrlNbr}: Cancelled and vacancy {VacancyCtrlNbr} reopened.",
            ctrlNbr, bulletin.PositionVacancyCtrlNbr);
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
        CancellationToken ct = default,
        TimeSpan? bulletinCutOffTime = null,
        string effectiveTimeMode = Domain.Modules.Bulletins.BulletinEffectiveTimeMode.FixedEffectiveTime)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var existing = await uow.BulletinRules.GetByCraftAsync(craftCtrlNbr);
        if (existing is not null)
        {
            existing.Update(bidWindowHours, bidWindowStartTime, bidWindowCloseTime,
                effectiveOffsetDays, effectiveTime, forceAssignHours, forceAssignSelectionMode, bulletinCutOffTime, effectiveTimeMode);
            await uow.BulletinRules.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }

        var rule = BulletinRule.Create(craftCtrlNbr, bidWindowHours, bidWindowStartTime,
            bidWindowCloseTime, effectiveOffsetDays, effectiveTime, forceAssignHours, forceAssignSelectionMode, bulletinCutOffTime, effectiveTimeMode);
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

            var candidates = await SelectForceAssignCandidatesAsync(uow, bulletin, rule, ct);
            var candidateCtrlNbr = candidates.FirstOrDefault();

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
    /// Marks all submitted bids on other bulletins for the given employee as Loser.
    /// Called after awarding or force-assigning a bulletin so an employee's remaining
    /// active bids are resolved in the same transaction.
    /// </summary>
    private static async Task MarkOtherEmployeeBidsAsLoserAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        ControlNumber awardedBulletinCtrlNbr,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct)
    {
        var allBids = await uow.BulletinBids.GetByEmployeeAsync(employeeCtrlNbr);
        foreach (var bid in allBids)
        {
            if (bid.Status != "Submitted") continue;
            if (bid.BulletinCtrlNbr == awardedBulletinCtrlNbr) continue;
            bid.MarkLoser();
            await uow.BulletinBids.UpdateAsync(bid, ct);
        }
    }

    /// <summary>
    /// Produces the ordered, qualification-filtered force-assign candidate list for a bulletin,
    /// honouring the craft's configured <see cref="Domain.Modules.Bulletins.ForceAssignSelectionMode"/>.
    /// The first element is the preferred (junior-most eligible) candidate; callers may fall through
    /// to later entries. Returns an empty list when no eligible candidate exists.
    /// </summary>
    private async Task<IReadOnlyList<ControlNumber>> SelectForceAssignCandidatesAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        Bulletin bulletin,
        BulletinRule rule,
        CancellationToken ct)
    {
        var targetRole = await ResolveTargetCraftRoleAsync(uow, bulletin, ct);
        return rule.ForceAssignSelectionMode switch
        {
            Domain.Modules.Bulletins.ForceAssignSelectionMode.JuniorHelperOrExtraBoard =>
                await SelectJuniorHelperOrExtraBoardAsync(uow, bulletin, targetRole, ct),
            _ =>
                await SelectJuniorExtraBoardAsync(uow, bulletin, targetRole, ct)
        };
    }

    /// <summary>
    /// Resolves the <see cref="Domain.Modules.WorkManagement.CraftRole"/> a bulletin is filling
    /// when it targets a crew position. Board (extra-board) vacancies are not tied to a single
    /// craft role, so this returns <c>null</c> for them. The resolved role anchors hierarchy-driven
    /// candidate pooling and qualification filtering during force assignment, replacing legacy
    /// hardcoded role-name matching.
    /// Path: bulletin → vacancy → crew position (by staffable position) → craft role.
    /// </summary>
    private static async Task<Domain.Modules.WorkManagement.CraftRole?> ResolveTargetCraftRoleAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        Bulletin bulletin,
        CancellationToken ct)
    {
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        if (vacancy is null || vacancy.TargetType != StaffablePositionType.Crew) return null;

        var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(vacancy.TargetCtrlNbr);
        if (crewPosition is null) return null;

        return await uow.CraftRoles.GetByCtrlNbrAsync(crewPosition.CraftRoleCtrlNbr, ct);
    }

    /// <summary>
    /// Builds the default force-assign candidate pool: every active board member whose board is
    /// flagged <see cref="Domain.Modules.Boards.RosterBoard.AllowForceAssign"/> for the bulletin's
    /// craft, ordered junior-first (RosterDate desc, then Rank desc) and filtered to those qualified
    /// for <paramref name="targetRole"/>. Board eligibility is tenant-configured (mirroring legacy
    /// <c>RosterBoard.ForceAssign</c>) rather than hardcoded to a board type, so hangout or other
    /// boards can participate when a railroad opts in. Returns an ordered list so callers can fall
    /// through to the next eligible candidate. When <paramref name="targetRole"/> is null
    /// (board vacancy / unresolved role), no qualification filter is applied.
    /// </summary>
    private async Task<IReadOnlyList<ControlNumber>> SelectJuniorExtraBoardAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        Bulletin bulletin,
        Domain.Modules.WorkManagement.CraftRole? targetRole,
        CancellationToken ct)
    {
        var boards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(bulletin.CraftCtrlNbr, ct);
        var forceAssignMembers = boards
            .Where(b => b.AllowForceAssign && b.IsActive)
            .SelectMany(b => b.Positions)
            .Select(p => p.EmployeeCtrlNbr);

        return await OrderJuniorFirstAndFilterAsync(uow, forceAssignMembers, targetRole, ct);
    }

    /// <summary>
    /// Orders a candidate set junior-first (RosterDate desc, then Rank desc) using each employee's
    /// active-roster seniority, then — when <paramref name="targetRole"/> is supplied — filters out
    /// anyone not qualified for that role via <see cref="EmployeeEligibilityService"/>. Employees
    /// without an active-roster seniority entry are excluded, mirroring legacy behaviour. Returns a
    /// deduplicated, ordered list of eligible candidates (junior-most first).
    /// </summary>
    private async Task<IReadOnlyList<ControlNumber>> OrderJuniorFirstAndFilterAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        IEnumerable<ControlNumber> candidateCtrlNbrs,
        Domain.Modules.WorkManagement.CraftRole? targetRole,
        CancellationToken ct)
    {
        var seniorityByEmployee = new Dictionary<ControlNumber, Domain.Models.Seniority.Seniority>();
        foreach (var empCtrlNbr in candidateCtrlNbrs)
        {
            if (seniorityByEmployee.ContainsKey(empCtrlNbr)) continue;
            var entries = await uow.Seniority.GetByEmployeeCtrlNbrAsync(empCtrlNbr);
            var relevant = entries.FirstOrDefault(s => s.LastActiveRoster);
            if (relevant is not null)
                seniorityByEmployee[empCtrlNbr] = relevant;
        }

        var ordered = seniorityByEmployee
            .OrderByDescending(kvp => kvp.Value.RosterDate)
            .ThenByDescending(kvp => kvp.Value.Rank)
            .Select(kvp => kvp.Key)
            .ToList();

        if (targetRole is null) return ordered;

        var qualified = new List<ControlNumber>();
        foreach (var empCtrlNbr in ordered)
        {
            var result = await eligibility.CheckEligibilityByCraftRoleAsync(uow, empCtrlNbr, targetRole.CtrlNbr, ct);
            if (result.IsEligible)
                qualified.Add(empCtrlNbr);
        }
        return qualified;
    }

    /// <summary>
    /// Builds the "subordinate tier or extra board" force-assign pool used when a higher-tier
    /// crew position (e.g. Foreman) goes no-bid. The pool is the union of:
    /// <list type="number">
    ///   <item>active members of boards flagged
    ///   <see cref="Domain.Modules.Boards.RosterBoard.AllowForceAssign"/> for the craft, and</item>
    ///   <item>employees currently holding an active crew incumbency in a <em>subordinate</em> role —
    ///   any craft role whose <see cref="Domain.Modules.WorkManagement.CraftRole.HierarchyLevel"/>
    ///   is strictly lower than the target role's.</item>
    /// </list>
    /// Board eligibility and subordinate roles are both derived from tenant-configured data rather
    /// than hardcoded board types or role names, so the logic works for any craft/railroad. The
    /// result is ordered junior-first and filtered to employees qualified for
    /// <paramref name="targetRole"/>. If the target role is unknown its hierarchy cannot be
    /// evaluated, so this degrades to the force-assign-board-only pool.
    /// </summary>
    private async Task<IReadOnlyList<ControlNumber>> SelectJuniorHelperOrExtraBoardAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        Bulletin bulletin,
        Domain.Modules.WorkManagement.CraftRole? targetRole,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var candidateCtrlNbrs = new HashSet<ControlNumber>();

        // 1. Force-assign-eligible board members for this craft (tenant-configured per board).
        var boards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(bulletin.CraftCtrlNbr, ct);
        foreach (var board in boards.Where(b => b.AllowForceAssign && b.IsActive))
            foreach (var pos in board.Positions)
                candidateCtrlNbrs.Add(pos.EmployeeCtrlNbr);

        // 2. Employees currently occupying an active crew incumbency in a subordinate role.
        //    Subordinate roles are tenant-configured: any role in the same craft whose
        //    HierarchyLevel is strictly below the target role's. This replaces the legacy
        //    hardcoded "Helper" name match so the rule holds for any craft/railroad.
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        if (vacancy is not null && targetRole is not null)
        {
            var craftRoles = await uow.CraftRoles.GetByCraftAsync(bulletin.CraftCtrlNbr);
            var subordinateRoleCtrlNbrs = craftRoles
                .Where(r => r.HierarchyLevel < targetRole.HierarchyLevel)
                .Select(r => r.CtrlNbr)
                .ToHashSet();

            if (subordinateRoleCtrlNbrs.Count > 0)
            {
                var crewsInWorkArea = await uow.Crews.GetByWorkAreaAsync(vacancy.WorkAreaGroupCtrlNbr);
                foreach (var crew in crewsInWorkArea)
                {
                    var positions = await uow.CrewPositions.GetByCrewAsync(crew.CtrlNbr);
                    foreach (var position in positions)
                    {
                        // Only include subordinate-tier role positions
                        if (!subordinateRoleCtrlNbrs.Contains(position.CraftRoleCtrlNbr)) continue;

                        var incumbency = await uow.CrewIncumbencies.GetActiveByPositionAsync(position.CtrlNbr, now);
                        if (incumbency is not null)
                            candidateCtrlNbrs.Add(incumbency.EmployeeCtrlNbr);
                    }
                }
            }
        }

        return await OrderJuniorFirstAndFilterAsync(uow, candidateCtrlNbrs, targetRole, ct);
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
        var forceAssigned = assignmentType == PositionAssignmentType.ForceAssignment;
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        if (vacancy is not null)
        {
            vacancy.Fill();
            await uow.PositionVacancies.UpdateAsync(vacancy, ct);

            // For a force-assignment, adopt the schedule-aware computed effective datetime
            // (stored as ForceAssignDeadlineUtc when the bulletin transitioned to NoBid), mirroring
            // legacy RailroadPositionBulletin.AssignDateTime where the trigger time IS the effective
            // time. Awarded (bid) assignments keep the default "effective now" behaviour.
            var effectiveUtc = forceAssigned ? bulletin.ForceAssignDeadlineUtc : null;
            var displacedEmployeeCtrlNbr = await PlaceEmployeeOnCrewPositionAsync(uow, vacancy, employeeCtrlNbr, assignmentType, effectiveUtc, ct);

            // Notify the employee displaced from the crew position (if any).
            if (displacedEmployeeCtrlNbr is not null && displacedEmployeeCtrlNbr != employeeCtrlNbr)
            {
                var displacedRailroad = await railroadResolver.ResolveFromWorkAreaAsync(uow, vacancy.WorkAreaGroupCtrlNbr, ct);
                if (displacedRailroad is not null)
                {
                    await notifications.NotifyDisplacedAsync(
                        uow, displacedRailroad, displacedEmployeeCtrlNbr,
                        Domain.Modules.Notifications.NotificationSubject.Create(
                            Domain.Modules.Notifications.NotificationSubjectTypes.Bulletin, bulletin.CtrlNbr),
                        ct);
                }
            }
        }

        // Notify the awarded/force-assigned employee.
        await notifications.NotifyBulletinAwardedAsync(uow, bulletin, employeeCtrlNbr, forceAssigned, ct);

        // A bulletin award/force-assign supersedes any pending voluntary seniority moves the
        // awarded employee had outstanding (legacy: RailroadPoolEmployee.RemoveUnassignedSeniorityMoves).
        await SupersedePendingSeniorityMovesAsync(uow, employeeCtrlNbr, bulletin.CtrlNbr, ct);
        await uow.Bulletins.UpdateAsync(bulletin, ct);
    }

    /// <summary>
    /// Cancels the employee's still-pending voluntary seniority moves because a bulletin award
    /// or force-assignment now governs their position. Mirrors SA's
    /// <c>RailroadPoolEmployee.RemoveUnassignedSeniorityMoves</c> (unassigned + voluntary).
    /// Force-assign moves are left untouched.
    /// </summary>
    private async Task SupersedePendingSeniorityMovesAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        ControlNumber bulletinCtrlNbr,
        CancellationToken ct)
    {
        var moves = await uow.SeniorityMoves.GetByEmployeeAsync(employeeCtrlNbr, ct);
        foreach (var move in moves)
        {
            if (move.Status != SeniorityMoveStatus.Pending) continue;
            if (move.MoveType != SeniorityMoveType.Voluntary) continue;

            move.Cancel($"Superseded by bulletin {bulletinCtrlNbr.Value}.");
            await uow.SeniorityMoves.UpdateAsync(move, ct);
            // Notify the previously-bumped employee the move is off, and clear the stale bump notice.
            await notifications.NotifySeniorityMoveCancelledAsync(uow, move, ct);
            logger.LogInformation(
                "Bulletin {BulletinCtrlNbr}: superseded pending seniority move {MoveCtrlNbr} for employee {EmployeeCtrlNbr}.",
                bulletinCtrlNbr.Value, move.CtrlNbr.Value, employeeCtrlNbr.Value);
        }
    }

    private static async Task<ControlNumber?> PlaceEmployeeOnCrewPositionAsync(
        Domain.Interfaces.IOrchestrationUnitOfWork uow,
        PositionVacancy vacancy,
        ControlNumber employeeCtrlNbr,
        string assignmentType,
        DateTime? effectiveUtc,
        CancellationToken ct)
    {
        if (vacancy.TargetType != StaffablePositionType.Crew) return null;

        var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(vacancy.TargetCtrlNbr);
        if (crewPosition is null) return null;

        // The effective datetime governs when the incoming assignment takes effect and when the
        // outgoing incumbency ends. Defaults to "now" for awarded assignments.
        var effectiveDate = effectiveUtc ?? DateTime.UtcNow;

        ControlNumber? displacedEmployeeCtrlNbr = null;

        // End any existing active incumbency on this position first.
        var existingIncumbency = await uow.CrewIncumbencies.GetActiveByPositionAsync(crewPosition.CtrlNbr, DateTime.UtcNow);
        if (existingIncumbency is not null)
        {
            displacedEmployeeCtrlNbr = existingIncumbency.EmployeeCtrlNbr;
            existingIncumbency.End(effectiveDate);
            uow.CrewIncumbencies.Update(existingIncumbency);
            var oldAssignment = await uow.PositionAssignments.GetByStaffablePositionAsync(crewPosition.StaffablePositionCtrlNbr);
            if (oldAssignment is not null) uow.PositionAssignments.Remove(oldAssignment);
        }

        var incumbency = CrewIncumbency.Create(crewPosition.CtrlNbr.Value, employeeCtrlNbr.Value, effectiveDate, null);
        var positionAssignment = PositionAssignment.Create(
            crewPosition.StaffablePositionCtrlNbr, employeeCtrlNbr, assignmentType, crewPosition.CtrlNbr, effectiveDate);

        uow.CrewIncumbencies.Add(incumbency);
        uow.PositionAssignments.Add(positionAssignment);

        return displacedEmployeeCtrlNbr;
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
        // Resolve the work-area timezone so day-of-week evaluation and on-duty localisation are
        // performed in work-area local time with DST awareness (consistent with CalculateBidWindow).
        var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(vacancy.WorkAreaGroupCtrlNbr, ct);
        var tz = ResolveTimeZone(workArea?.TimeZoneId);

        // Resolve the crew position's active schedule via:
        // vacancy → crewPosition → crew → active crewAssignment → assignment → schedule.
        int? operatingDaysMask = null;
        TimeOnly? onDutyTime = null;
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
                    operatingDaysMask = schedule.OperatingDaysMask;
                    onDutyTime = schedule.OnDutyTime;
                }
            }
        }

        // Delegate the mode-specific computation (work day vs off day; fixed effective time vs
        // on-duty-minus-force-hours vs bid-window close) to the domain rule. When schedule data is
        // unavailable, the domain falls back to the configured effective datetime. This value is
        // stored as ForceAssignDeadlineUtc and is also adopted as the assignment's effective time,
        // mirroring the legacy RailroadPositionBulletin.AssignDateTime (trigger == effective).
        return rule.CalculateForceAssignEffectiveUtc(
            bulletin.EffectiveUtc, bulletin.BidWindowClosesUtc, operatingDaysMask, onDutyTime, tz);
    }

    /// <summary>
    /// Runs the full winner-selection process for the given bulletin without committing any
    /// changes. Returns the winning <see cref="BulletinBid"/> (seniority order + preference
    /// chain), or <c>null</c> if no qualified bidder exists. Safe to call for preview / UI
    /// pre-selection before the dispatcher confirms the award.
    /// </summary>
    public async Task<BulletinBid?> GetBulletinWinnerAsync(ControlNumber bulletinCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bulletin = await uow.Bulletins.GetByCtrlNbrAsync(bulletinCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Bulletin {bulletinCtrlNbr} not found.");
        return await SelectBulletinWinnerAsync(uow, bulletin, ct);
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
            .OrderBy(b => b.SeniorityDate)
            .ThenBy(b => b.SeniorityRank)
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
                    // Notify losers (legacy parity); informational, no acknowledgement required.
                    await notifications.NotifyBulletinLostAsync(uow, bulletin, loserBid.EmployeeCtrlNbr, ct);
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
