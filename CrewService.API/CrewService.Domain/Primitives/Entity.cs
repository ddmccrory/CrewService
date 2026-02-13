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

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public AuditStamp? DeletedBy { get; private set; }

    public void SoftDelete(string deletedByUser)
    {
        if (IsDeleted) return; // Idempotent

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = AuditStamp.Create(deletedByUser);
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }

    protected void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}