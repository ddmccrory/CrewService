using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Railroads;

namespace CrewService.Domain.DomainEvents;

public sealed class RailroadPoolEmployeeCreatedDomainEvent(RailroadPoolEmployee railroadPoolEmployee) : IDomainEvent
{
    public RailroadPoolEmployee RailroadPoolEmployee { get; } = railroadPoolEmployee;
}