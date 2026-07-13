using CrewService.Domain.DomainEvents;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Bulletins;

/// <summary>
/// Controls how the youngest candidate is selected for a no-bid force assignment.
/// Stored per BulletinRule (craft-scoped, railroad-specific).
/// </summary>
public static class ForceAssignSelectionMode
{
    /// <summary>
    /// Junior-most employee on the extra board who is qualified for the position.
    /// Default for Engineer, Helper, and clerical crafts.
    /// </summary>
    public const string JuniorExtraBoard = "JuniorExtraBoard";

    /// <summary>
    /// Junior-most employee who is either on the extra board OR currently holding
    /// a Helper crew position (and qualified for Foreman). Used for Foreman vacancies.
    /// </summary>
    public const string JuniorHelperOrExtraBoard = "JuniorHelperOrExtraBoard";

    public static bool IsValid(string mode) =>
        mode is JuniorExtraBoard or JuniorHelperOrExtraBoard;
}

public sealed class BulletinAccessAudit : Entity
{
    public ControlNumber BulletinCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public DateTime ViewedAtUtc { get; private set; }

    private BulletinAccessAudit()
    {
        BulletinCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static BulletinAccessAudit Create(
        ControlNumber bulletinCtrlNbr,
        ControlNumber employeeCtrlNbr,
        DateTime viewedAtUtc)
    {
        return new BulletinAccessAudit
        {
            BulletinCtrlNbr = bulletinCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            ViewedAtUtc = viewedAtUtc
        };
    }
}

/// <summary>
/// Controls how the effective datetime of a force-assigned (no-bid) crew bulletin is
/// computed when the configured effective date lands on a work day versus an off day.
/// Stored per <see cref="BulletinRule"/> (craft-scoped, railroad-specific) so tenants can
/// model craft agreements without hardcoded craft names. Mirrors the legacy
/// <c>RailroadPositionBulletin.AssignDateTime</c> craft switch.
/// </summary>
public static class BulletinEffectiveTimeMode
{
    /// <summary>
    /// Always use the rule's configured effective time (e.g. 04:00), regardless of work/off day.
    /// Legacy parity: "Mechanical" craft. Default so existing behaviour is unchanged.
    /// </summary>
    public const string FixedEffectiveTime = "FixedEffectiveTime";

    /// <summary>
    /// Use the configured effective time when the effective date is a work day; when it is an
    /// off day, use the first work day's on-duty time minus <c>ForceAssignHours</c>.
    /// Legacy parity: "Engineer" craft (assigned at 04:00 unless the effective date is an off
    /// day, then <c>ForceAssignHours</c> before the start of the first work day).
    /// </summary>
    public const string EffectiveTimeUnlessOffDay = "EffectiveTimeUnlessOffDay";

    /// <summary>
    /// Always use the (next) work day's on-duty time minus <c>ForceAssignHours</c>.
    /// Legacy parity: default craft branch (on-duty minus forced-assign hours).
    /// </summary>
    public const string OnDutyMinusForceHours = "OnDutyMinusForceHours";

    /// <summary>
    /// Use the bulletin's bid-window close datetime as the effective datetime.
    /// Legacy parity: "Clerical" craft (assigned at close time).
    /// </summary>
    public const string BidWindowCloseTime = "BidWindowCloseTime";

    public static bool IsValid(string mode) =>
        mode is FixedEffectiveTime or EffectiveTimeUnlessOffDay or OnDutyMinusForceHours or BidWindowCloseTime;
}

/// <summary>
/// Maps domain status strings to Bootstrap badge CSS classes.
/// Single authoritative source — no badge logic should exist in any UI layer.
/// </summary>
public static class BulletinStatusBadge
{
    public static string ForBulletin(string status) => status switch
    {
        "Posted"    => "bg-primary",
        "Awarded"   => "bg-success",
        "Forced"    => "bg-warning text-dark",
        "NoBid"     => "bg-secondary",
        "Cancelled" => "bg-danger",
        _           => "bg-light text-dark"
    };

    public static string ForBid(string status) => status switch
    {
        "Submitted" => "bg-primary",
        "Winner"    => "bg-success",
        "Loser"     => "bg-secondary",
        "Withdrawn" => "bg-danger",
        _           => "bg-light text-dark"
    };

    public static string ForVacancy(string status) => status switch
    {
        "Open"      => "bg-warning text-dark",
        "Bulletined"=> "bg-info text-dark",
        "Filled"    => "bg-success",
        "Abolished" => "bg-secondary",
        _           => "bg-light text-dark"
    };
}

public sealed class PositionVacancy : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public string TargetType { get; private set; } = string.Empty;
    public ControlNumber TargetCtrlNbr { get; private set; }
    /// <summary>
    /// Denormalized display name of the position target (e.g. "Conductor Crew — Position 1").
    /// Set at creation time by the caller who resolves it from the crew/board name.
    /// </summary>
    public string TargetName { get; private set; } = string.Empty;
    public ControlNumber CraftCtrlNbr { get; private set; }
    public string VacancyReasonCode { get; private set; } = string.Empty;
    public ControlNumber? PreviousIncumbentCtrlNbr { get; private set; }
    public string Status { get; private set; } = "Open";
    public DateTime OpenedUtc { get; private set; }
    public DateTime? ClosedUtc { get; private set; }

    private PositionVacancy() { WorkAreaGroupCtrlNbr = null!; TargetCtrlNbr = null!; CraftCtrlNbr = null!; }

    public static PositionVacancy Create(
        ControlNumber workAreaGroupCtrlNbr,
        string targetType,
        ControlNumber targetCtrlNbr,
        ControlNumber craftCtrlNbr,
        string vacancyReasonCode,
        ControlNumber? previousIncumbentCtrlNbr = null,
        string targetName = "")
    {
        if (!StaffablePositionType.IsValid(targetType))
            throw new ArgumentException(
                $"Invalid targetType '{targetType}'. Must be '{StaffablePositionType.Crew}' or '{StaffablePositionType.Board}'.",
                nameof(targetType));

        var vacancy = new PositionVacancy
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            TargetType = targetType,
            TargetCtrlNbr = targetCtrlNbr,
            TargetName = targetName,
            CraftCtrlNbr = craftCtrlNbr,
            VacancyReasonCode = vacancyReasonCode,
            PreviousIncumbentCtrlNbr = previousIncumbentCtrlNbr,
            OpenedUtc = DateTime.UtcNow
        };
        vacancy.Raise(new PositionVacancyCreatedDomainEvent(vacancy));
        return vacancy;
    }

    public void MarkBulletined()
    {
        Status = "Bulletined";
    }

    /// <summary>
    /// Returns a bulletined vacancy to the Open state when its bulletin is cancelled.
    /// The position survives and is re-postable; it is NOT auto-reposted by any worker
    /// (matches legacy behavior where cancelling a bulletin leaves the position unbulletined).
    /// </summary>
    public void Reopen()
    {
        Status = "Open";
        ClosedUtc = null;
    }

    public void Fill()
    {
        Status = "Filled";
        ClosedUtc = DateTime.UtcNow;
        Raise(new PositionVacancyFilledDomainEvent(this));
    }

    public void Abolish()
    {
        Status = "Abolished";
        ClosedUtc = DateTime.UtcNow;
        Raise(new PositionVacancyAbolishedDomainEvent(this));
    }
}

public sealed class Bulletin : Entity
{
    public ControlNumber PositionVacancyCtrlNbr { get; private set; }
    public ControlNumber CraftCtrlNbr { get; private set; }
    public DateTime BidWindowOpensUtc { get; private set; }
    public DateTime BidWindowClosesUtc { get; private set; }
    public DateTime EffectiveUtc { get; private set; }
    public string Status { get; private set; } = "Posted";
    public ControlNumber? AwardedEmployeeCtrlNbr { get; private set; }
    public string? AwardType { get; private set; }
    /// <summary>
    /// Set when bulletin transitions to NoBid on a crew position.
    /// Equals EffectiveUtc minus ForceAssignHours from the craft's BulletinRule.
    /// Null for extra-board positions (no force assign) or while still open.
    /// </summary>
    public DateTime? ForceAssignDeadlineUtc { get; private set; }

    private Bulletin() { PositionVacancyCtrlNbr = null!; CraftCtrlNbr = null!; }

    /// <summary>
    /// Whether this bulletin may be cancelled. Mirrors SA's <c>CanCancelBulletin =&gt; !IsClosed</c>:
    /// cancellation is allowed only while the bid window is still open and the bulletin has not
    /// been awarded/force-assigned. There is no admin override outside this window (legacy parity).
    /// </summary>
    public bool CanCancel(DateTime utcNow) =>
        Status == "Posted" && AwardedEmployeeCtrlNbr is null && utcNow <= BidWindowClosesUtc;

    /// <summary>
    /// Whether the bid window has opened yet (open time reached), regardless of whether it has
    /// since closed. Mirrors legacy SA employee bulletin visibility (<c>Now &gt; OpenDateTime</c>):
    /// employees must not see a bulletin before its window opens. Dispatchers are not scoped by this.
    /// </summary>
    public bool HasBidWindowOpened(DateTime utcNow) =>
        utcNow >= BidWindowOpensUtc;

    /// <summary>
    /// Whether the bid window is currently open, independent of status. Mirrors legacy SA
    /// <c>RailroadPositionBulletin.IsOpen</c> plus the close-time bound applied by the employee
    /// bulletin collection queries (<c>Now &gt; OpenDateTime &amp;&amp; Now &lt;= CloseDateTime</c>):
    /// a bulletin whose open time is still in the future is not yet biddable.
    /// </summary>
    public bool IsBidWindowOpen(DateTime utcNow) =>
        utcNow >= BidWindowOpensUtc && utcNow <= BidWindowClosesUtc;

    /// <summary>
    /// Whether an employee may currently bid on this bulletin: it must be posted and its bid
    /// window must be open. Employees cannot see or bid on a bulletin before the window opens
    /// (legacy parity) — the application layer enforces this before accepting a bid.
    /// </summary>
    public bool IsBiddable(DateTime utcNow) =>
        Status == "Posted" && IsBidWindowOpen(utcNow);

    public static Bulletin Create(
        ControlNumber positionVacancyCtrlNbr,
        ControlNumber craftCtrlNbr,
        DateTime bidWindowOpensUtc,
        DateTime bidWindowClosesUtc,
        DateTime effectiveUtc)
    {
        var bulletin = new Bulletin
        {
            PositionVacancyCtrlNbr = positionVacancyCtrlNbr,
            CraftCtrlNbr = craftCtrlNbr,
            BidWindowOpensUtc = bidWindowOpensUtc,
            BidWindowClosesUtc = bidWindowClosesUtc,
            EffectiveUtc = effectiveUtc
        };
        bulletin.Raise(new BulletinPostedDomainEvent(bulletin));
        return bulletin;
    }

    public void Close()
    {
        Status = "Closed";
    }

    public void Award(ControlNumber employeeCtrlNbr)
    {
        AwardedEmployeeCtrlNbr = employeeCtrlNbr;
        AwardType = PositionAssignmentType.BulletinAssignment;
        Status = "Awarded";
        Raise(new PositionAwardedDomainEvent(this));
    }

    public void ForceAssign(ControlNumber employeeCtrlNbr)
    {
        AwardedEmployeeCtrlNbr = employeeCtrlNbr;
        AwardType = PositionAssignmentType.ForceAssignment;
        Status = "Forced";
        Raise(new PositionAwardedDomainEvent(this));
    }

    public void SetAsNoBid(DateTime? forceAssignDeadlineUtc = null)
    {
        Status = "NoBid";
        ForceAssignDeadlineUtc = forceAssignDeadlineUtc;
        Raise(new BulletinNoBidDomainEvent(this));
    }

    public void Complete()
    {
        Status = "Completed";
    }

    public void Cancel()
    {
        Status = "Cancelled";
    }
}

public sealed class BulletinBid : Entity
{
    public ControlNumber BulletinCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public int Priority { get; private set; }
    public DateTime SubmittedUtc { get; private set; }
    public DateTime SeniorityDate { get; private set; }
    public int SeniorityRank { get; private set; }
    public string Status { get; private set; } = "Submitted";

    private BulletinBid() { BulletinCtrlNbr = null!; EmployeeCtrlNbr = null!; }

    public static BulletinBid Create(ControlNumber bulletinCtrlNbr, ControlNumber employeeCtrlNbr, int priority, DateTime seniorityDate, int seniorityRank)
    {
        return new BulletinBid
        {
            BulletinCtrlNbr = bulletinCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            Priority = priority,
            SubmittedUtc = DateTime.UtcNow,
            SeniorityDate = seniorityDate,
            SeniorityRank = seniorityRank
        };
    }

    public void MarkWinner()
    {
        Status = "Winner";
    }

    public void MarkLoser()
    {
        Status = "Loser";
    }

    public void Withdraw()
    {
        Status = "Withdrawn";
        Raise(new BulletinBidWithdrawnDomainEvent(this));
    }
}

// ──────────────────────────────────────────────────────────────────
// BulletinRule — timing configuration per craft
// ──────────────────────────────────────────────────────────────────

/// <summary>
/// Defines bulletin timing rules for a craft. Controls how long a bulletin is open,
/// when it opens/closes each day, when the position becomes effective, and the
/// force-assign window for no-bid crew positions.
/// </summary>
public sealed class BulletinRule : Entity
{
    public ControlNumber CraftCtrlNbr { get; private set; }

    /// <summary>Total hours the bidding window stays open.</summary>
    public int BidWindowHours { get; private set; }

    /// <summary>Time of day (UTC offset from midnight) the bid window opens.</summary>
    public TimeSpan BidWindowStartTime { get; private set; }

    /// <summary>Time of day (UTC offset from midnight) the bid window closes.</summary>
    public TimeSpan BidWindowCloseTime { get; private set; }

    /// <summary>
    /// If a vacancy is opened after this time of day (local work-area time), the bid
    /// window rolls to the next posting cycle (open date advances by one day).
    /// When null, there is no cutoff and bulletins always open on the same day.
    /// </summary>
    public TimeSpan? BulletinCutOffTime { get; private set; }

    /// <summary>Calendar days after close date before the position becomes effective.</summary>
    public int EffectiveOffsetDays { get; private set; }

    /// <summary>Time of day (UTC offset from midnight) the effective date begins.</summary>
    public TimeSpan EffectiveTime { get; private set; }

    /// <summary>Hours before the effective on-duty time to force-assign a no-bid crew position.</summary>
    public int ForceAssignHours { get; private set; }

    /// <summary>
    /// Determines how the youngest candidate is selected when a crew bulletin receives no bids.
    /// See <see cref="ForceAssignSelectionMode"/> for valid values.
    /// </summary>
    public string ForceAssignSelectionMode { get; private set; } = Bulletins.ForceAssignSelectionMode.JuniorExtraBoard;

    /// <summary>
    /// Determines how the effective datetime of a force-assigned (no-bid) crew bulletin is
    /// computed relative to the position's work schedule. See <see cref="BulletinEffectiveTimeMode"/>.
    /// </summary>
    public string EffectiveTimeMode { get; private set; } = Bulletins.BulletinEffectiveTimeMode.FixedEffectiveTime;

    private BulletinRule() { CraftCtrlNbr = null!; }

    public static BulletinRule Create(
        ControlNumber craftCtrlNbr,
        int bidWindowHours,
        TimeSpan bidWindowStartTime,
        TimeSpan bidWindowCloseTime,
        int effectiveOffsetDays,
        TimeSpan effectiveTime,
        int forceAssignHours,
        string forceAssignSelectionMode = Bulletins.ForceAssignSelectionMode.JuniorExtraBoard,
        TimeSpan? bulletinCutOffTime = null,
        string effectiveTimeMode = Bulletins.BulletinEffectiveTimeMode.FixedEffectiveTime)
    {
        if (!Bulletins.ForceAssignSelectionMode.IsValid(forceAssignSelectionMode))
            throw new ArgumentException($"Invalid ForceAssignSelectionMode '{forceAssignSelectionMode}'.", nameof(forceAssignSelectionMode));
        if (!Bulletins.BulletinEffectiveTimeMode.IsValid(effectiveTimeMode))
            throw new ArgumentException($"Invalid EffectiveTimeMode '{effectiveTimeMode}'.", nameof(effectiveTimeMode));

        var rule = new BulletinRule
        {
            CraftCtrlNbr = craftCtrlNbr,
            BidWindowHours = bidWindowHours,
            BidWindowStartTime = bidWindowStartTime,
            BidWindowCloseTime = bidWindowCloseTime,
            EffectiveOffsetDays = effectiveOffsetDays,
            EffectiveTime = effectiveTime,
            ForceAssignHours = forceAssignHours,
            ForceAssignSelectionMode = forceAssignSelectionMode,
            BulletinCutOffTime = bulletinCutOffTime,
            EffectiveTimeMode = effectiveTimeMode
        };
        rule.Raise(new BulletinRuleCreatedDomainEvent(rule));
        return rule;
    }

    public void Update(
        int bidWindowHours,
        TimeSpan bidWindowStartTime,
        TimeSpan bidWindowCloseTime,
        int effectiveOffsetDays,
        TimeSpan effectiveTime,
        int forceAssignHours,
        string forceAssignSelectionMode = Bulletins.ForceAssignSelectionMode.JuniorExtraBoard,
        TimeSpan? bulletinCutOffTime = null,
        string effectiveTimeMode = Bulletins.BulletinEffectiveTimeMode.FixedEffectiveTime)
    {
        if (!Bulletins.ForceAssignSelectionMode.IsValid(forceAssignSelectionMode))
            throw new ArgumentException($"Invalid ForceAssignSelectionMode '{forceAssignSelectionMode}'.", nameof(forceAssignSelectionMode));
        if (!Bulletins.BulletinEffectiveTimeMode.IsValid(effectiveTimeMode))
            throw new ArgumentException($"Invalid EffectiveTimeMode '{effectiveTimeMode}'.", nameof(effectiveTimeMode));

        BidWindowHours = bidWindowHours;
        BidWindowStartTime = bidWindowStartTime;
        BidWindowCloseTime = bidWindowCloseTime;
        EffectiveOffsetDays = effectiveOffsetDays;
        EffectiveTime = effectiveTime;
        ForceAssignHours = forceAssignHours;
        ForceAssignSelectionMode = forceAssignSelectionMode;
        BulletinCutOffTime = bulletinCutOffTime;
        EffectiveTimeMode = effectiveTimeMode;
        Raise(new BulletinRuleUpdatedDomainEvent(this));
    }

    /// <summary>
    /// Calculates the Open, Close, and Effective datetimes (UTC) for a new bulletin
    /// based on this rule, given the day the vacancy occurs (in UTC).
    /// When <paramref name="workAreaTimeZone"/> is provided, the configured times
    /// (BidWindowStartTime, BidWindowCloseTime, EffectiveTime) are treated as
    /// local work-area times and converted to UTC with full DST awareness.
    /// When null, times are treated as UTC offsets (legacy/fallback behaviour).
    /// </summary>
    public (DateTime Opens, DateTime Closes, DateTime Effective) CalculateBidWindow(
        DateTime vacancyDateUtc, TimeZoneInfo? workAreaTimeZone = null)
    {
        if (workAreaTimeZone is not null)
        {
            // Work in the work-area's local date so DST transitions are handled correctly.
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(vacancyDateUtc, workAreaTimeZone);
            // If a cutoff time is configured and it's already past that time, roll to tomorrow.
            var localOpenDate = BulletinCutOffTime.HasValue && localNow.TimeOfDay > BulletinCutOffTime.Value
                ? localNow.Date.AddDays(1)
                : localNow.Date;

            // Build local DateTimeOffset values so ConvertTimeToUtc can resolve DST ambiguity.
            var localOpens = localOpenDate + BidWindowStartTime;
            var localCloses = localOpens.AddHours(BidWindowHours);
            var localCloseDate = localCloses.Date;
            localCloses = localCloseDate + BidWindowCloseTime;
            var localEffective = localCloseDate.AddDays(EffectiveOffsetDays) + EffectiveTime;

            // Convert each local datetime to UTC individually; this handles DST gaps/folds per value.
            return (
                TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localOpens, DateTimeKind.Unspecified), workAreaTimeZone),
                TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localCloses, DateTimeKind.Unspecified), workAreaTimeZone),
                TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localEffective, DateTimeKind.Unspecified), workAreaTimeZone));
        }

        // Fallback: naive UTC arithmetic (no work-area timezone configured).
        var openDate = BulletinCutOffTime.HasValue && vacancyDateUtc.TimeOfDay > BulletinCutOffTime.Value
            ? vacancyDateUtc.Date.AddDays(1)
            : vacancyDateUtc.Date;
        var opens = openDate + BidWindowStartTime;
        var closes = opens.AddHours(BidWindowHours);
        var closeDate = closes.Date;
        closes = closeDate + BidWindowCloseTime;
        var effective = closes.Date.AddDays(EffectiveOffsetDays) + EffectiveTime;
        return (opens, closes, effective);
    }

    /// <summary>
    /// Computes when a force-assign should execute for a no-bid crew position:
    /// ForceAssignHours hours before the effective UTC datetime.
    /// </summary>
    public DateTime CalculateForceAssignDeadline(DateTime effectiveUtc) =>
        effectiveUtc.AddHours(-ForceAssignHours);

    /// <summary>
    /// Computes the effective (assignment) UTC datetime for a force-assigned no-bid crew
    /// bulletin according to this rule's <see cref="EffectiveTimeMode"/>. Mirrors the legacy
    /// <c>RailroadPositionBulletin.AssignDateTime</c> craft switch:
    /// <list type="bullet">
    /// <item><see cref="BulletinEffectiveTimeMode.FixedEffectiveTime"/> — the configured effective datetime.</item>
    /// <item><see cref="BulletinEffectiveTimeMode.BidWindowCloseTime"/> — the bid-window close datetime.</item>
    /// <item><see cref="BulletinEffectiveTimeMode.OnDutyMinusForceHours"/> — the (next) work day's on-duty time minus <see cref="ForceAssignHours"/>.</item>
    /// <item><see cref="BulletinEffectiveTimeMode.EffectiveTimeUnlessOffDay"/> — the configured effective datetime when the
    /// effective date is a work day, otherwise the first work day's on-duty time minus <see cref="ForceAssignHours"/>.</item>
    /// </list>
    /// Schedule-dependent modes require <paramref name="operatingDaysMask"/> and <paramref name="onDutyTime"/>; when either is
    /// unavailable the method falls back to the configured <paramref name="effectiveUtc"/>. When <paramref name="workAreaTimeZone"/>
    /// is supplied, day-of-week evaluation and on-duty localisation are performed in work-area local time with DST awareness.
    /// </summary>
    /// <param name="effectiveUtc">The bulletin's configured effective datetime (UTC), as produced by <see cref="CalculateBidWindow"/>.</param>
    /// <param name="bidWindowClosesUtc">The bulletin's bid-window close datetime (UTC).</param>
    /// <param name="operatingDaysMask">Bitmask of the position's operating days (bit <c>1 &lt;&lt; (int)DayOfWeek</c>, Sunday = 0).</param>
    /// <param name="onDutyTime">The position's local on-duty time of day.</param>
    /// <param name="workAreaTimeZone">The work-area timezone; when null, arithmetic is performed in UTC.</param>
    public DateTime CalculateForceAssignEffectiveUtc(
        DateTime effectiveUtc,
        DateTime bidWindowClosesUtc,
        int? operatingDaysMask = null,
        TimeOnly? onDutyTime = null,
        TimeZoneInfo? workAreaTimeZone = null)
    {
        switch (EffectiveTimeMode)
        {
            case Bulletins.BulletinEffectiveTimeMode.BidWindowCloseTime:
                return bidWindowClosesUtc;

            case Bulletins.BulletinEffectiveTimeMode.OnDutyMinusForceHours
                when operatingDaysMask is int mask && onDutyTime is TimeOnly onDuty:
                return OnDutyMinusForceHoursUtc(effectiveUtc, mask, onDuty, workAreaTimeZone);

            case Bulletins.BulletinEffectiveTimeMode.EffectiveTimeUnlessOffDay
                when operatingDaysMask is int mask && onDutyTime is TimeOnly onDuty:
                var effectiveLocalDate = ToLocalDate(effectiveUtc, workAreaTimeZone);
                var isWorkDay = (mask & (1 << (int)effectiveLocalDate.DayOfWeek)) != 0;
                return isWorkDay
                    ? effectiveUtc
                    : OnDutyMinusForceHoursUtc(effectiveUtc, mask, onDuty, workAreaTimeZone);

            // FixedEffectiveTime, or a schedule-dependent mode without schedule data:
            // fall back to the configured effective datetime (the safe "work day" branch).
            default:
                return effectiveUtc;
        }
    }

    /// <summary>
    /// Resolves the UTC datetime of the first work day's on-duty time (on or after the
    /// <paramref name="effectiveUtc"/> local date) minus <see cref="ForceAssignHours"/>.
    /// </summary>
    private DateTime OnDutyMinusForceHoursUtc(
        DateTime effectiveUtc, int operatingDaysMask, TimeOnly onDutyTime, TimeZoneInfo? workAreaTimeZone)
    {
        var workDate = ToLocalDate(effectiveUtc, workAreaTimeZone);
        for (int i = 0; i < 14; i++)
        {
            if ((operatingDaysMask & (1 << (int)workDate.DayOfWeek)) != 0) break;
            workDate = workDate.AddDays(1);
        }

        var localOnDuty = workDate + onDutyTime.ToTimeSpan();
        var onDutyUtc = workAreaTimeZone is null
            ? DateTime.SpecifyKind(localOnDuty, DateTimeKind.Utc)
            : TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localOnDuty, DateTimeKind.Unspecified), workAreaTimeZone);
        return onDutyUtc.AddHours(-ForceAssignHours);
    }

    private static DateTime ToLocalDate(DateTime utc, TimeZoneInfo? workAreaTimeZone) =>
        (workAreaTimeZone is null ? utc : TimeZoneInfo.ConvertTimeFromUtc(utc, workAreaTimeZone)).Date;
}

// Domain Events
public sealed record PositionVacancyCreatedDomainEvent : DomainEvent
{
    public PositionVacancyCreatedDomainEvent(PositionVacancy v)
        : base(nameof(PositionVacancy), v.CtrlNbr.Value, new { WorkAreaGroupCtrlNbr = v.WorkAreaGroupCtrlNbr.Value, v.TargetType, TargetCtrlNbr = v.TargetCtrlNbr.Value, CraftCtrlNbr = v.CraftCtrlNbr.Value, v.VacancyReasonCode }) { }
}

public sealed record PositionVacancyFilledDomainEvent : DomainEvent
{
    public PositionVacancyFilledDomainEvent(PositionVacancy v)
        : base(nameof(PositionVacancy), v.CtrlNbr.Value, new { v.TargetType, TargetCtrlNbr = v.TargetCtrlNbr.Value }) { }
}

public sealed record PositionVacancyAbolishedDomainEvent : DomainEvent
{
    public PositionVacancyAbolishedDomainEvent(PositionVacancy v)
        : base(nameof(PositionVacancy), v.CtrlNbr.Value, new { v.TargetType, TargetCtrlNbr = v.TargetCtrlNbr.Value }) { }
}

public sealed record BulletinPostedDomainEvent : DomainEvent
{
    public BulletinPostedDomainEvent(Bulletin b)
        : base(nameof(Bulletin), b.CtrlNbr.Value, new { PositionVacancyCtrlNbr = b.PositionVacancyCtrlNbr.Value, CraftCtrlNbr = b.CraftCtrlNbr.Value }) { }
}

public sealed record PositionAwardedDomainEvent : DomainEvent
{
    public PositionAwardedDomainEvent(Bulletin b)
        : base(nameof(Bulletin), b.CtrlNbr.Value, new { AwardedEmployeeCtrlNbr = b.AwardedEmployeeCtrlNbr!.Value, b.AwardType, PositionVacancyCtrlNbr = b.PositionVacancyCtrlNbr.Value }) { }
}

public sealed record BulletinBidWithdrawnDomainEvent : DomainEvent
{
    public BulletinBidWithdrawnDomainEvent(BulletinBid bid)
        : base(nameof(BulletinBid), bid.CtrlNbr.Value, new { BulletinCtrlNbr = bid.BulletinCtrlNbr.Value, EmployeeCtrlNbr = bid.EmployeeCtrlNbr.Value }) { }
}

public sealed record BulletinNoBidDomainEvent : DomainEvent
{
    public BulletinNoBidDomainEvent(Bulletin b)
        : base(nameof(Bulletin), b.CtrlNbr.Value, new { PositionVacancyCtrlNbr = b.PositionVacancyCtrlNbr.Value, CraftCtrlNbr = b.CraftCtrlNbr.Value }) { }
}

public sealed record BulletinRuleCreatedDomainEvent : DomainEvent
{
    public BulletinRuleCreatedDomainEvent(BulletinRule r)
        : base(nameof(BulletinRule), r.CtrlNbr.Value, new { CraftCtrlNbr = r.CraftCtrlNbr.Value }) { }
}

public sealed record BulletinRuleUpdatedDomainEvent : DomainEvent
{
    public BulletinRuleUpdatedDomainEvent(BulletinRule r)
        : base(nameof(BulletinRule), r.CtrlNbr.Value, new { CraftCtrlNbr = r.CraftCtrlNbr.Value }) { }
}
