using CrewService.Domain.Interfaces;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents;

public sealed record EmailAddressTypeCreatedDomainEvent(ControlNumber ClientCtrlNbr) : IDomainEvent;