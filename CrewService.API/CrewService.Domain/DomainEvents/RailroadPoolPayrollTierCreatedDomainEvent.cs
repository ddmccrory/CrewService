using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Railroads;

namespace CrewService.Domain.DomainEvents;

public sealed class RailroadPoolPayrollTierCreatedDomainEvent(RailroadPoolPayrollTier railroadPoolPayrollTier) : IDomainEvent
{
    public RailroadPoolPayrollTier RailroadPoolPayrollTier { get; } = railroadPoolPayrollTier;
}