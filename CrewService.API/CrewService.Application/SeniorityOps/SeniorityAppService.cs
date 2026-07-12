using CrewService.Application.Qualifications;
using CrewService.Application.BackgroundWorkers;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.SeniorityOps;

public sealed class SeniorityAppService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    RequirementEvaluationService requirementEvaluationService,
    QualificationReactiveService qualificationReactiveService,
    SeniorityStateVacancyConfigService vacancyConfigService,
    ISeniorityStateChangeSignal seniorityStateChangeSignal)
{
    public sealed record SeniorityListItem(
        Domain.Models.Seniority.Seniority Seniority,
        string EmployeeNumber,
        string? EmployeeUserId,
        string FullNameLnf,
        string SeniorityStateName,
        List<string> RestrictionLabels,
        string PositionName = "",
        string PositionType = "",
        long StaffablePositionCtrlNbr = 0,
        bool CanExerciseSeniority = false);

    public async Task<List<SeniorityListItem>> GetAllAsync(
        ControlNumber? rosterCtrlNbr = null, ControlNumber? railroadCtrlNbr = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var seniorities = rosterCtrlNbr is not null
            ? await uow.Seniority.GetByRosterCtrlNbrAsync(rosterCtrlNbr)
            : await uow.Seniority.GetAllAsync(ct);

        if (seniorities.Count == 0) return [];

        var uniqueEmpCtrlNbrs = seniorities.Select(s => s.EmployeeCtrlNbr).Distinct().ToList();
        var employees = await uow.Employees.GetByCtrlNbrsAsync(uniqueEmpCtrlNbrs, ct);
        var employeeMap = employees.ToDictionary(e => e.CtrlNbr.Value);

        var uniqueStateCtrlNbrs = seniorities.Select(s => s.SeniorityStateCtrlNbr).Distinct().ToList();
        var stateMap = new Dictionary<long, string>();
        foreach (var stateCtrlNbr in uniqueStateCtrlNbrs)
        {
            var state = await uow.SeniorityStates.GetByCtrlNbrAsync(stateCtrlNbr, ct);
            stateMap[stateCtrlNbr.Value] = state?.StateDescription ?? string.Empty;
        }

        var empRestrictionLabels = new Dictionary<ControlNumber, List<string>>();
        int? policyEligibilityDays = null; // null = no policy configured = ineligible
        int  policyRequestHours     = 0;    // used for early-submission window
        var firstRosterCtrlNbr = seniorities.Select(s => s.RosterCtrlNbr).FirstOrDefault();
        if (firstRosterCtrlNbr is not null)
        {
            var roster = await uow.Rosters.GetByCtrlNbrAsync(firstRosterCtrlNbr, ct);
            if (roster is not null)
            {
                var policy = railroadCtrlNbr is not null
                    ? await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(railroadCtrlNbr, roster.CraftCtrlNbr)
                    : null;
                if (policy is not null)
                {
                    policyEligibilityDays = Math.Max(
                        policy.CrewToCrewEligibilityDays,
                        Math.Max(
                            policy.CrewToBoardEligibilityDays,
                            Math.Max(
                                policy.ExtraBoardToCrewEligibilityDays,
                                Math.Max(
                                    policy.HangoutToCrewEligibilityDays,
                                    Math.Max(
                                        policy.ExtendedAbsenceToCrewEligibilityDays,
                                        Math.Max(policy.TrainingToCrewEligibilityDays, policy.NewHireToCrewEligibilityDays))))));
                    policyRequestHours   = policy.RequestHours;
                }
                var restrictingQualTypes = (await uow.QualificationTypes.GetActiveByCraftCtrlNbrAsync(roster.CraftCtrlNbr))
                    .Where(qt => qt.RestrictionLabel is not null).ToList();

                if (restrictingQualTypes.Count > 0)
                {
                    var empManualQuals = await uow.EmployeeQualifications.GetActiveByEmployeeCtrlNbrsAsync(uniqueEmpCtrlNbrs);
                    var empActiveManualQualTypes = empManualQuals
                        .GroupBy(eq => eq.EmployeeCtrlNbr)
                        .ToDictionary(g => g.Key, g => g.Select(eq => eq.QualificationTypeCtrlNbr!).ToHashSet());

                    var computedQualificationSatisfiedMap = new Dictionary<(long EmployeeCtrlNbr, long QualificationTypeCtrlNbr), bool>();

                    foreach (var empCtrlNbr in uniqueEmpCtrlNbrs)
                    {
                        empActiveManualQualTypes.TryGetValue(empCtrlNbr, out var heldManualQuals);
                        heldManualQuals ??= [];

                        foreach (var qt in restrictingQualTypes)
                        {
                            var isHeld = string.Equals(qt.EvaluationStrategy, EvaluationStrategies.Manual, StringComparison.OrdinalIgnoreCase)
                                ? heldManualQuals.Contains(qt.CtrlNbr)
                                : await IsComputedQualificationSatisfiedAsync(empCtrlNbr, qt, uow, computedQualificationSatisfiedMap, ct);

                            if (!isHeld)
                            {
                                if (!empRestrictionLabels.TryGetValue(empCtrlNbr, out var labels))
                                {
                                    labels = [];
                                    empRestrictionLabels[empCtrlNbr] = labels;
                                }
                                labels.Add(qt.RestrictionLabel!);
                            }
                        }
                    }
                }
            }
        }

        // Resolve current position for each employee (crew or board position name)
        var empPositionMap = new Dictionary<ControlNumber, (string PositionName, string PositionType, long StaffablePositionCtrlNbr, DateTime AssignedDateUtc)>();
        foreach (var empCtrlNbr in uniqueEmpCtrlNbrs)
        {
            var positionAssignments = await uow.PositionAssignments.GetByEmployeeAsync(empCtrlNbr);
            if (positionAssignments.Count == 0) continue;
            var pa = positionAssignments[0];
            var staffPos = await uow.StaffablePositions.GetByCtrlNbrAsync(pa.StaffablePositionCtrlNbr, ct);
            if (staffPos is null) continue;

            string posName;
            if (staffPos.PositionType == StaffablePositionType.Board)
            {
                RosterBoard? board = null;
                if (pa.AssignmentSourceCtrlNbr is not null)
                    board = await uow.RosterBoards.GetByPositionCtrlNbrAsync(pa.AssignmentSourceCtrlNbr, ct);
                posName = board?.Name ?? staffPos.PositionType;
            }
            else
            {
                CrewPosition? crewPos = null;
                if (pa.AssignmentSourceCtrlNbr is not null)
                    crewPos = await uow.CrewPositions.GetByCtrlNbrAsync(pa.AssignmentSourceCtrlNbr, ct);
                crewPos ??= await uow.CrewPositions.GetByStaffablePositionAsync(pa.StaffablePositionCtrlNbr);

                if (crewPos is not null)
                {
                    var crew      = await uow.Crews.GetByCtrlNbrAsync(crewPos.CrewCtrlNbr, ct);
                    var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPos.CraftRoleCtrlNbr, ct);
                    var crewName  = crew?.Name ?? string.Empty;
                    var roleName  = craftRole?.Name ?? string.Empty;
                    posName = (crewName, roleName) switch
                    {
                        ({ Length: > 0 }, { Length: > 0 }) => $"{crewName} / {roleName}",
                        ({ Length: > 0 }, _)               => crewName,
                        (_, { Length: > 0 })               => roleName,
                        _                                  => staffPos.PositionType
                    };
                }
                else
                {
                    posName = staffPos.PositionType;
                }
            }

            empPositionMap[empCtrlNbr] = (posName, staffPos.PositionType, pa.StaffablePositionCtrlNbr.Value, pa.AssignedDateUtc);
        }

        return seniorities.Select(s =>
        {
            employeeMap.TryGetValue(s.EmployeeCtrlNbr.Value, out var emp);
            var stateName = stateMap.GetValueOrDefault(s.SeniorityStateCtrlNbr.Value, string.Empty);
            empRestrictionLabels.TryGetValue(s.EmployeeCtrlNbr, out var restrictionLabels);
            empPositionMap.TryGetValue(s.EmployeeCtrlNbr, out var pos);
            var hasPosition = pos.AssignedDateUtc != default;
            var daysOnPosition = hasPosition
                ? (int)(DateTime.UtcNow - pos.AssignedDateUtc).TotalDays
                : 0;
            // Employee can submit (RequestHours/24) days before fully qualifying (legacy early-submission window).
            var earlySubmitDays = policyRequestHours / 24;
            var canExercise = hasPosition
                && policyEligibilityDays is not null
                && daysOnPosition >= (policyEligibilityDays.Value - earlySubmitDays);
            return new SeniorityListItem(
                s,
                emp?.EmployeeNumber ?? string.Empty,
                emp?.UserId,
                string.Empty, // name resolved in presentation via EmployeeNameService
                stateName,
                restrictionLabels ?? [],
                pos.PositionName ?? string.Empty,
                pos.PositionType ?? string.Empty,
                pos.StaffablePositionCtrlNbr,
                canExercise);
        }).ToList();
    }

    private async Task<bool> IsComputedQualificationSatisfiedAsync(
        ControlNumber employeeCtrlNbr,
        Domain.Modules.Employees.QualificationType qualificationType,
        IOrchestrationUnitOfWork uow,
        Dictionary<(long EmployeeCtrlNbr, long QualificationTypeCtrlNbr), bool> computedQualificationSatisfiedMap,
        CancellationToken ct)
    {
        var key = (employeeCtrlNbr.Value, qualificationType.CtrlNbr.Value);
        if (computedQualificationSatisfiedMap.TryGetValue(key, out var cached))
            return cached;

        var evaluation = await requirementEvaluationService.EvaluateAsync(employeeCtrlNbr, qualificationType, uow, ct);
        var isSatisfied = evaluation.AllSatisfied && !evaluation.IsSuspended;
        computedQualificationSatisfiedMap[key] = isSatisfied;
        return isSatisfied;
    }

    public async Task<Domain.Models.Seniority.Seniority> GetAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Seniority.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority {ctrlNbr.Value} not found.");
    }

    public async Task<Domain.Models.Seniority.Seniority> CreateAsync(
        ControlNumber rosterCtrlNbr, ControlNumber employeeCtrlNbr,
        bool lastActiveRoster, DateTime rosterDate, int rank,
        ControlNumber seniorityStateCtrlNbr, bool canTrain,
        CancellationToken ct = default)
    {
        var seniority = Domain.Models.Seniority.Seniority.Create(
            rosterCtrlNbr, employeeCtrlNbr, lastActiveRoster, rosterDate, rank, seniorityStateCtrlNbr, canTrain);

        ControlNumber? craftCtrlNbr = null;
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.Seniority.Add(seniority);
        var roster = await uow.Rosters.GetByCtrlNbrAsync(rosterCtrlNbr, ct);
        if (roster is not null)
            craftCtrlNbr = roster.CraftCtrlNbr;
        await uow.CommitAsync(ct);

        if (craftCtrlNbr is not null)
            await qualificationReactiveService.HandleAddedToRosterAsync(seniority.EmployeeCtrlNbr, craftCtrlNbr, ct);

        return seniority;
    }

    public async Task<Domain.Models.Seniority.Seniority> UpdateAsync(
        ControlNumber ctrlNbr, bool lastActiveRoster, DateTime rosterDate, int rank,
        ControlNumber seniorityStateCtrlNbr, bool canTrain, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var seniority = await uow.Seniority.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority {ctrlNbr.Value} not found.");

        var newState = await uow.SeniorityStates.GetByCtrlNbrAsync(seniorityStateCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority state {seniorityStateCtrlNbr.Value} not found.");

        var previousStateCtrlNbr = seniority.SeniorityStateCtrlNbr;
        seniority.Update(lastActiveRoster, rosterDate, rank, seniorityStateCtrlNbr, canTrain);

        if (newState.StateType == StateType.OffProperty)
        {
            // Off-property applies employee-wide: every seniority record is end-dated and moved into
            // the off-property state, all individual qualifications are deleted, and every live
            // certification is administratively cancelled.
            await ApplyOffPropertyCascadeAsync(uow, seniority.EmployeeCtrlNbr, seniorityStateCtrlNbr, DateTime.UtcNow, ct);
        }
        else
        {
            // Any non-off-property state re-opens this record's seniority (reactivation clears the end date).
            seniority.ClearEndDate();
            uow.Seniority.Update(seniority);
        }

        await uow.CommitAsync(ct);

        // Apply the configured vacancy action once the seniority state changes. Runs after the UoW is
        // committed and disposed so the canonical vacate/repost services each open their own
        // transaction on the shared connection. Positions are collected employee-wide, so a single
        // call detaches the employee from every position they currently hold.
        if (previousStateCtrlNbr != seniorityStateCtrlNbr)
        {
            await vacancyConfigService.ApplyVacancyActionAsync(
                seniority.EmployeeCtrlNbr, seniorityStateCtrlNbr, seniority.RosterCtrlNbr, ct);
        }

        return seniority;
    }

    private const string OffPropertyCancellationReason = "Employee off property";

    /// <summary>
    /// Applies the employee-wide off-property cascade inside the supplied unit of work: every
    /// seniority record for the employee is moved into the off-property state and end-dated, all
    /// individual (manually granted) qualifications are removed, and every certification that is
    /// still live is administratively cancelled. Position vacating is handled separately by the
    /// caller via <see cref="SeniorityStateVacancyConfigService.ApplyVacancyActionAsync"/>.
    /// </summary>
    private static async Task ApplyOffPropertyCascadeAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        ControlNumber offPropertyStateCtrlNbr,
        DateTime endDate,
        CancellationToken ct)
    {
        var seniorityRecords = await uow.Seniority.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
        foreach (var record in seniorityRecords)
        {
            if (record.SeniorityStateCtrlNbr != offPropertyStateCtrlNbr)
                record.Update(seniorityStateCtrlNbr: offPropertyStateCtrlNbr);
            record.SetEndDate(endDate);
            uow.Seniority.Update(record);
        }

        var qualifications = await uow.EmployeeQualifications.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
        foreach (var qualification in qualifications)
            uow.EmployeeQualifications.Remove(qualification);

        var certifications = await uow.EmployeeCertifications.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr, ct);
        foreach (var certification in certifications)
        {
            if (IsCancellableCertificationStatus(certification.Status))
            {
                certification.Cancel(OffPropertyCancellationReason);
                await uow.EmployeeCertifications.UpdateAsync(certification, ct);
            }
        }
    }

    /// <summary>
    /// A certification is cancellable while it is still live. Statuses that already represent a
    /// terminal outcome (Cancelled, Revoked, Expired) are left untouched - in particular Revoked is
    /// an FRA due-process outcome that must not be overwritten by an administrative cancellation.
    /// </summary>
    private static bool IsCancellableCertificationStatus(string status) =>
        status is not (CertificationStatuses.Cancelled
            or CertificationStatuses.Revoked
            or CertificationStatuses.Expired);

    public async Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var seniority = await uow.Seniority.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority {ctrlNbr.Value} not found.");
        await uow.Seniority.DeleteAsync(seniority.CtrlNbr, ct);
        await uow.CommitAsync(ct);
    }

    public async Task<(bool Found, long CraftCtrlNbr, string CraftName)> GetActiveCraftForEmployeeAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var seniorityRecords = await uow.Seniority.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
        var activeRecord = seniorityRecords.FirstOrDefault(s => s.LastActiveRoster);
        if (activeRecord is null) return (false, 0, string.Empty);

        var roster = await uow.Rosters.GetByCtrlNbrAsync(activeRecord.RosterCtrlNbr, ct);
        if (roster is null) return (false, 0, string.Empty);

        var craft = await uow.Crafts.GetByCtrlNbrAsync(roster.CraftCtrlNbr, ct);
        if (craft is null) return (false, 0, string.Empty);

        return (true, craft.CtrlNbr.Value, craft.CraftName);
    }

    // ──────────────────────────────────────────────────────────────────
    // Pending / Scheduled state changes
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Schedules a future state change. Throws if <paramref name="effectiveDateUtc"/> is in the past
    /// or if the employee already has a pending change.
    /// </summary>
    public async Task<Domain.Models.Seniority.PendingSeniorityStateChange> ScheduleStateChangeAsync(
        ControlNumber seniorityCtrlNbr,
        ControlNumber toStateCtrlNbr,
        DateTime effectiveDateUtc,
        string scheduledByUserId,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var seniority = await uow.Seniority.GetByCtrlNbrAsync(seniorityCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority {seniorityCtrlNbr.Value} not found.");

        var existing = await uow.PendingSeniorityStateChanges
            .GetPendingByEmployeeAsync(seniority.EmployeeCtrlNbr, ct);
        if (existing is not null)
            throw new InvalidOperationException(
                "This employee already has a pending state change scheduled. Cancel it before scheduling a new one.");

        var pending = Domain.Models.Seniority.PendingSeniorityStateChange.Schedule(
            seniorityCtrlNbr,
            seniority.EmployeeCtrlNbr,
            seniority.SeniorityStateCtrlNbr,
            toStateCtrlNbr,
            effectiveDateUtc,
            scheduledByUserId);

        uow.PendingSeniorityStateChanges.Add(pending);
        await uow.CommitAsync(ct);
        seniorityStateChangeSignal.Notify(pending.EffectiveDateUtc);
        return pending;
    }

    /// <summary>
    /// Returns the pending change for the given employee's active seniority record (or null),
    /// along with the work-area timezone id resolved via the employee's active seniority roster
    /// so the presentation layer can localize the effective/scheduled dates to the work area.
    /// </summary>
    public async Task<(Domain.Models.Seniority.PendingSeniorityStateChange? Pending, string? WorkAreaTimeZoneId)> GetPendingChangeAsync(
        ControlNumber seniorityCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var seniority = await uow.Seniority.GetByCtrlNbrAsync(seniorityCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority {seniorityCtrlNbr.Value} not found.");
        var pending = await uow.PendingSeniorityStateChanges
            .GetPendingByEmployeeAsync(seniority.EmployeeCtrlNbr, ct);
        if (pending is null)
            return (null, null);

        var tzId = await ResolveWorkAreaTimeZoneIdAsync(uow, seniority.EmployeeCtrlNbr, ct);
        return (pending, tzId);
    }

    /// <summary>
    /// Resolves the work-area time zone id for the given seniority record by walking the
    /// employee's active seniority roster → work-area dynamic group, or null when unavailable.
    /// </summary>
    public async Task<string?> GetSeniorityWorkAreaTimeZoneIdAsync(
        ControlNumber seniorityCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var seniority = await uow.Seniority.GetByCtrlNbrAsync(seniorityCtrlNbr, ct);
        if (seniority is null) return null;
        return await ResolveWorkAreaTimeZoneIdAsync(uow, seniority.EmployeeCtrlNbr, ct);
    }

    private static async Task<string?> ResolveWorkAreaTimeZoneIdAsync(
        IOrchestrationUnitOfWork uow, ControlNumber employeeCtrlNbr, CancellationToken ct)
    {
        var seniorityEntries = await uow.Seniority.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
        var activeEntry = seniorityEntries.FirstOrDefault(s => s.LastActiveRoster) ?? seniorityEntries.FirstOrDefault();
        if (activeEntry is null) return null;
        var roster = await uow.Rosters.GetByCtrlNbrAsync(activeEntry.RosterCtrlNbr, ct);
        if (roster is null) return null;
        var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr, ct);
        return workArea?.TimeZoneId;
    }

    /// <summary>
    /// Admin-only: cancels a pending state change.
    /// </summary>
    public async Task CancelPendingChangeAsync(
        ControlNumber pendingChangeCtrlNbr,
        string cancelledByUserId,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var pending = await uow.PendingSeniorityStateChanges.GetByCtrlNbrAsync(pendingChangeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"PendingSeniorityStateChange {pendingChangeCtrlNbr.Value} not found.");

        pending.Cancel(cancelledByUserId);
        uow.PendingSeniorityStateChanges.Update(pending);
        await uow.CommitAsync(ct);
    }

    /// <summary>
    /// Called by the worker: applies all pending changes whose effective date has passed.
    /// </summary>
    public async Task<int> ApplyDuePendingChangesAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var due = await uow.PendingSeniorityStateChanges.GetDueAsync(DateTime.UtcNow, ct);
        if (due.Count == 0) return 0;

        foreach (var pending in due)
        {
            var seniority = await uow.Seniority.GetByCtrlNbrAsync(pending.SeniorityCtrlNbr, ct);
            if (seniority is null)
            {
                pending.Cancel("system:seniority-not-found");
                uow.PendingSeniorityStateChanges.Update(pending);
                await uow.CommitAsync(ct);
                continue;
            }

            var newState = await uow.SeniorityStates.GetByCtrlNbrAsync(pending.ToSeniorityStateCtrlNbr, ct);

            var previousState = seniority.SeniorityStateCtrlNbr;
            seniority.Update(
                seniority.LastActiveRoster,
                seniority.RosterDate,
                seniority.Rank,
                pending.ToSeniorityStateCtrlNbr,
                seniority.CanTrain);

            if (newState?.StateType == StateType.OffProperty)
                await ApplyOffPropertyCascadeAsync(uow, seniority.EmployeeCtrlNbr, pending.ToSeniorityStateCtrlNbr, DateTime.UtcNow, ct);
            else
                seniority.ClearEndDate();

            uow.Seniority.Update(seniority);

            pending.MarkApplied();
            uow.PendingSeniorityStateChanges.Update(pending);

            await uow.CommitAsync(ct);

            if (previousState != pending.ToSeniorityStateCtrlNbr)
            {
                await vacancyConfigService.ApplyVacancyActionAsync(
                    seniority.EmployeeCtrlNbr,
                    pending.ToSeniorityStateCtrlNbr,
                    seniority.RosterCtrlNbr,
                    ct);
            }
        }

        return due.Count;
    }

    /// <summary>
    /// Returns the earliest UTC effective date of any pending change — used by the worker to seed its signal.
    /// </summary>
    public async Task<DateTime?> GetNextPendingChangeUtcAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PendingSeniorityStateChanges.GetNextEffectiveDateUtcAsync(ct);
    }

    /// <summary>
    /// Returns the UTC datetime and resolved work-area for the next pending
    /// state change within the given railroad.
    /// </summary>
    public async Task<(DateTime? EffectiveDateUtc, ControlNumber? WorkAreaGroupCtrlNbr, string? WorkAreaTimeZoneId)> GetNextPendingChangeForRailroadAsync(
        ControlNumber railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var pending = await uow.PendingSeniorityStateChanges.GetAllPendingByRailroadAsync(railroadCtrlNbr, ct);
        var earliest = pending.OrderBy(p => p.EffectiveDateUtc).FirstOrDefault();
        if (earliest is null) return (null, null, null);

        // Resolve work area via the employee's active seniority roster.
        ControlNumber? workAreaCtrlNbr = null;
        string? workAreaTimeZoneId = null;
        var seniorityEntries = await uow.Seniority.GetByEmployeeCtrlNbrAsync(earliest.EmployeeCtrlNbr);
        var activeEntry = seniorityEntries.FirstOrDefault(s => s.LastActiveRoster) ?? seniorityEntries.FirstOrDefault();
        if (activeEntry is not null)
        {
            var roster = await uow.Rosters.GetByCtrlNbrAsync(activeEntry.RosterCtrlNbr, ct);
            if (roster is not null)
            {
                workAreaCtrlNbr = roster.WorkAreaGroupCtrlNbr;
                var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr, ct);
                workAreaTimeZoneId = workArea?.TimeZoneId;
            }
        }

        return (earliest.EffectiveDateUtc, workAreaCtrlNbr, workAreaTimeZoneId);
    }

    public sealed record PendingChangeListItem(
        Domain.Models.Seniority.PendingSeniorityStateChange Pending,
        string EmployeeNumber,
        string EmployeeUserId,
        string FromStateName,
        string ToStateName,
        string? WorkAreaTimeZoneId = null);

    /// <summary>
    /// Returns all pending scheduled state changes for the given railroad, enriched with display names.
    /// </summary>
    public async Task<List<PendingChangeListItem>> GetAllPendingAsync(
        ControlNumber railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var pending = await uow.PendingSeniorityStateChanges.GetAllPendingByRailroadAsync(railroadCtrlNbr, ct);
        if (pending.Count == 0) return [];

        var employeeCtrlNbrs = pending.Select(p => p.EmployeeCtrlNbr).Distinct().ToList();
        var employees = await uow.Employees.GetByCtrlNbrsAsync(employeeCtrlNbrs, ct);
        var employeeMap = employees.ToDictionary(e => e.CtrlNbr.Value);

        // Resolve work area timezone per employee via their active seniority roster
        var tzMap = new Dictionary<long, string?>();
        foreach (var empCtrlNbr in employeeCtrlNbrs)
        {
            var seniorityEntries = await uow.Seniority.GetByEmployeeCtrlNbrAsync(empCtrlNbr);
            var activeEntry = seniorityEntries.FirstOrDefault(s => s.LastActiveRoster) ?? seniorityEntries.FirstOrDefault();
            if (activeEntry is not null)
            {
                var roster = await uow.Rosters.GetByCtrlNbrAsync(activeEntry.RosterCtrlNbr, ct);
                if (roster is not null)
                {
                    var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr, ct);
                    tzMap[empCtrlNbr.Value] = workArea?.TimeZoneId;
                }
            }
        }

        var stateCtrlNbrs = pending
            .SelectMany(p => new[] { p.FromSeniorityStateCtrlNbr, p.ToSeniorityStateCtrlNbr })
            .Distinct().ToList();
        var stateMap = new Dictionary<long, string>();
        foreach (var stateCtrlNbr in stateCtrlNbrs)
        {
            var state = await uow.SeniorityStates.GetByCtrlNbrAsync(stateCtrlNbr, ct);
            stateMap[stateCtrlNbr.Value] = state?.StateDescription ?? stateCtrlNbr.Value.ToString();
        }

        return [.. pending.Select(p =>
        {
            employeeMap.TryGetValue(p.EmployeeCtrlNbr.Value, out var emp);
            tzMap.TryGetValue(p.EmployeeCtrlNbr.Value, out var tzId);
            return new PendingChangeListItem(
                p,
                emp?.EmployeeNumber ?? string.Empty,
                emp?.UserId ?? string.Empty,
                stateMap.GetValueOrDefault(p.FromSeniorityStateCtrlNbr.Value, string.Empty),
                stateMap.GetValueOrDefault(p.ToSeniorityStateCtrlNbr.Value, string.Empty),
                tzId);
        })];
    }
}
