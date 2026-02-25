using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Bulletins;

public sealed class PositionVacancy : Entity
{
    public string TargetType { get; private set; } = string.Empty;
    public ControlNumber TargetCtrlNbr { get; private set; }
    public ControlNumber CraftCtrlNbr { get; private set; }
    public string VacancyReasonCode { get; private set; } = string.Empty;
    public ControlNumber? PreviousIncumbentCtrlNbr { get; private set; }
    public string Status { get; private set; } = "Open";
    public DateTime OpenedUtc { get; private set; }
    public DateTime? ClosedUtc { get; private set; }

    private PositionVacancy() { TargetCtrlNbr = null!; CraftCtrlNbr = null!; }

    public static PositionVacancy Create(string targetType, long targetCtrlNbr, long craftCtrlNbr,
        string vacancyReasonCode, long? previousIncumbentCtrlNbr = null)
    {
        var vacancy = new PositionVacancy
        {
            TargetType = targetType,
            TargetCtrlNbr = ControlNumber.Create(targetCtrlNbr),
            CraftCtrlNbr = ControlNumber.Create(craftCtrlNbr),
            VacancyReasonCode = vacancyReasonCode,
            PreviousIncumbentCtrlNbr = previousIncumbentCtrlNbr.HasValue
                ? ControlNumber.Create(previousIncumbentCtrlNbr.Value)
                : null,
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
    public string Status { get; private set; } = "Posted";
    public ControlNumber? AwardedEmployeeCtrlNbr { get; private set; }
    public string? AwardType { get; private set; }

    private Bulletin() { PositionVacancyCtrlNbr = null!; CraftCtrlNbr = null!; }

    public static Bulletin Create(long positionVacancyCtrlNbr, long craftCtrlNbr,
        DateTime bidWindowOpensUtc, DateTime bidWindowClosesUtc)
    {
        var bulletin = new Bulletin
        {
            PositionVacancyCtrlNbr = ControlNumber.Create(positionVacancyCtrlNbr),
            CraftCtrlNbr = ControlNumber.Create(craftCtrlNbr),
            BidWindowOpensUtc = bidWindowOpensUtc,
            BidWindowClosesUtc = bidWindowClosesUtc
        };
        bulletin.Raise(new BulletinPostedDomainEvent(bulletin));
        return bulletin;
    }

    public void Close()
    {
        Status = "Closed";
    }

    public void Award(long employeeCtrlNbr)
    {
        AwardedEmployeeCtrlNbr = ControlNumber.Create(employeeCtrlNbr);
        AwardType = "BID";
        Status = "Awarded";
        Raise(new PositionAwardedDomainEvent(this));
    }

    public void ForceAssign(long employeeCtrlNbr)
    {
        AwardedEmployeeCtrlNbr = ControlNumber.Create(employeeCtrlNbr);
        AwardType = "FORCED";
        Status = "Forced";
        Raise(new PositionAwardedDomainEvent(this));
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

    public static BulletinBid Create(long bulletinCtrlNbr, long employeeCtrlNbr, int priority, int seniorityRank)
    {
        return new BulletinBid
        {
            BulletinCtrlNbr = ControlNumber.Create(bulletinCtrlNbr),
            EmployeeCtrlNbr = ControlNumber.Create(employeeCtrlNbr),
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

// Domain Events
public sealed record PositionVacancyCreatedDomainEvent : DomainEvent
{
    public PositionVacancyCreatedDomainEvent(PositionVacancy v)
        : base(nameof(PositionVacancy), v.CtrlNbr.Value, new { v.TargetType, TargetCtrlNbr = v.TargetCtrlNbr.Value, CraftCtrlNbr = v.CraftCtrlNbr.Value, v.VacancyReasonCode }) { }
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
