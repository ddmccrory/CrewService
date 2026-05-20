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
    public int SeniorityRank { get; private set; }
    public string Status { get; private set; } = "Submitted";

    private BulletinBid() { BulletinCtrlNbr = null!; EmployeeCtrlNbr = null!; }

    public static BulletinBid Create(ControlNumber bulletinCtrlNbr, ControlNumber employeeCtrlNbr, int priority, int seniorityRank)
    {
        return new BulletinBid
        {
            BulletinCtrlNbr = bulletinCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            Priority = priority,
            SubmittedUtc = DateTime.UtcNow,
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

    private BulletinRule() { CraftCtrlNbr = null!; }

    public static BulletinRule Create(
        ControlNumber craftCtrlNbr,
        int bidWindowHours,
        TimeSpan bidWindowStartTime,
        TimeSpan bidWindowCloseTime,
        int effectiveOffsetDays,
        TimeSpan effectiveTime,
        int forceAssignHours,
        string forceAssignSelectionMode = Bulletins.ForceAssignSelectionMode.JuniorExtraBoard)
    {
        if (!Bulletins.ForceAssignSelectionMode.IsValid(forceAssignSelectionMode))
            throw new ArgumentException($"Invalid ForceAssignSelectionMode '{forceAssignSelectionMode}'.", nameof(forceAssignSelectionMode));

        var rule = new BulletinRule
        {
            CraftCtrlNbr = craftCtrlNbr,
            BidWindowHours = bidWindowHours,
            BidWindowStartTime = bidWindowStartTime,
            BidWindowCloseTime = bidWindowCloseTime,
            EffectiveOffsetDays = effectiveOffsetDays,
            EffectiveTime = effectiveTime,
            ForceAssignHours = forceAssignHours,
            ForceAssignSelectionMode = forceAssignSelectionMode
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
        string forceAssignSelectionMode = Bulletins.ForceAssignSelectionMode.JuniorExtraBoard)
    {
        if (!Bulletins.ForceAssignSelectionMode.IsValid(forceAssignSelectionMode))
            throw new ArgumentException($"Invalid ForceAssignSelectionMode '{forceAssignSelectionMode}'.", nameof(forceAssignSelectionMode));

        BidWindowHours = bidWindowHours;
        BidWindowStartTime = bidWindowStartTime;
        BidWindowCloseTime = bidWindowCloseTime;
        EffectiveOffsetDays = effectiveOffsetDays;
        EffectiveTime = effectiveTime;
        ForceAssignHours = forceAssignHours;
        ForceAssignSelectionMode = forceAssignSelectionMode;
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
            var localOpenDate = localNow.Date;

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
        var openDate = vacancyDateUtc.Date;
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
