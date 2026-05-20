using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Bulletins;

public sealed class BulletinsService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<IReadOnlyList<PositionVacancy>> GetOpenVacanciesAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PositionVacancies.GetOpenAsync();
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

    public async Task<IReadOnlyList<Bulletin>> GetPostedBulletinsAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Bulletins.GetPostedAsync();
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

    public async Task<BulletinBid> SubmitBidAsync(long bulletinCtrlNbr, long employeeCtrlNbr, int priority, int seniorityRank, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
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
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        vacancy?.Fill();
        if (vacancy is not null) await uow.PositionVacancies.UpdateAsync(vacancy, ct);
        await uow.Bulletins.UpdateAsync(bulletin, ct);
        await uow.CommitAsync(ct);
        return bulletin;
    }

    public async Task<Bulletin> ForceAssignBulletinAsync(ControlNumber ctrlNbr, ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bulletin = await uow.Bulletins.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Bulletin {ctrlNbr} not found.");
        bulletin.ForceAssign(employeeCtrlNbr);
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        vacancy?.Fill();
        if (vacancy is not null) await uow.PositionVacancies.UpdateAsync(vacancy, ct);
        await uow.Bulletins.UpdateAsync(bulletin, ct);
        await uow.CommitAsync(ct);
        return bulletin;
    }

    public async Task<Bulletin> SetBulletinNoBidAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bulletin = await uow.Bulletins.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Bulletin {ctrlNbr} not found.");

        // For crew-position bulletins, compute the force-assign deadline from the bulletin rule.
        DateTime? forceAssignDeadline = null;
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        if (vacancy?.TargetType == StaffablePositionType.Crew)
        {
            var rule = await uow.BulletinRules.GetByCraftAsync(bulletin.CraftCtrlNbr);
            if (rule is not null)
                forceAssignDeadline = rule.CalculateForceAssignDeadline(bulletin.EffectiveUtc);
        }

        bulletin.SetAsNoBid(forceAssignDeadline);
        await uow.Bulletins.UpdateAsync(bulletin, ct);
        await uow.CommitAsync(ct);
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

        return bulletin;
    }

    // ── WorkArea-scoped queries ───────────────────────────────────────

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
            var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
            vacancy?.Fill();
            if (vacancy is not null) await uow.PositionVacancies.UpdateAsync(vacancy, ct);
            await uow.Bulletins.UpdateAsync(bulletin, ct);
            assigned.Add(bulletin);
        }

        if (assigned.Count > 0)
            await uow.CommitAsync(ct);

        return assigned;
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
    private static TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return null;
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { return null; }
    }
}