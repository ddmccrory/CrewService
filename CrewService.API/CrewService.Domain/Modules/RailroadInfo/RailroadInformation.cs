using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.RailroadInfo;

public sealed class RailroadInformation : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public string InformationType { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Draft";
    public DateTime? PublishedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    private RailroadInformation() { WorkAreaGroupCtrlNbr = null!; }

    public static RailroadInformation Create(
        long workAreaGroupCtrlNbr, string informationType, string subject, string body)
    {
        var info = new RailroadInformation
        {
            WorkAreaGroupCtrlNbr = ControlNumber.Create(workAreaGroupCtrlNbr),
            InformationType = informationType,
            Subject = subject,
            Body = body
        };
        info.Raise(new RailroadInformationCreatedDomainEvent(info));
        return info;
    }

    public void Update(string subject, string body, string informationType)
    {
        if (Status != "Draft")
            throw new InvalidOperationException("Only draft information records can be updated.");

        Subject = subject;
        Body = body;
        InformationType = informationType;
    }

    public void Publish()
    {
        if (Status != "Draft")
            throw new InvalidOperationException("Only draft information records can be published.");

        Status = "Published";
        PublishedAtUtc = DateTime.UtcNow;
        Raise(new RailroadInformationPublishedDomainEvent(this));
    }

    public void Close()
    {
        if (Status != "Published")
            throw new InvalidOperationException("Only published information records can be closed.");

        Status = "Closed";
        ClosedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is "Closed" or "Cancelled")
            throw new InvalidOperationException("Cannot cancel an already closed or cancelled record.");

        Status = "Cancelled";
        ClosedAtUtc = DateTime.UtcNow;
    }
}

public sealed record RailroadInformationCreatedDomainEvent : DomainEvent
{
    public RailroadInformationCreatedDomainEvent(RailroadInformation info)
        : base(nameof(RailroadInformation), info.CtrlNbr.Value,
            new { info.InformationType, info.Subject }) { }
}

public sealed record RailroadInformationPublishedDomainEvent : DomainEvent
{
    public RailroadInformationPublishedDomainEvent(RailroadInformation info)
        : base(nameof(RailroadInformation), info.CtrlNbr.Value,
            new { info.InformationType, info.Subject, info.PublishedAtUtc }) { }
}
