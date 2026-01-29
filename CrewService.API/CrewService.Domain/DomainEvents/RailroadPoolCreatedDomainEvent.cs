using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Railroads;

namespace CrewService.Domain.DomainEvents;

public sealed class RailroadPoolCreatedDomainEvent(RailroadPool railroadPool) : IDomainEvent
{
    public RailroadPool RailroadPool { get; } = railroadPool;
}