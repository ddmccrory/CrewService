using CrewService.Domain.Interfaces;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Primitives;
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public List<IDomainEvent> DomainEvents => [.. _domainEvents];

    public ControlNumber CtrlNbr { get; init; } = ControlNumber.Create();

    public AuditStamp? CreatedBy { get; set; }

    public AuditStamp? ModifiedBy { get; set; }

    protected void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}