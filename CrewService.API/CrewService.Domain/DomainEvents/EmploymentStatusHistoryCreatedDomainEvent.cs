using CrewService.Domain.Interfaces;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents;

public sealed record EmploymentStatusHistoryCreatedDomainEvent(ControlNumber EmployeeCtrlNbr) : IDomainEvent;