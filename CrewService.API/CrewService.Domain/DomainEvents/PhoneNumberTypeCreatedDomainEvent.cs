using CrewService.Domain.Interfaces;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents;

public sealed record PhoneNumberTypeCreatedDomainEvent(ControlNumber ClientCtrlNbr) : IDomainEvent;