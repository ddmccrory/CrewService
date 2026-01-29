using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;

namespace CrewService.Domain.DomainEvents;

/// <summary>
/// Domain event that is raised when a SeniorityState is created.
/// </summary>
public sealed class SeniorityStateCreatedDomainEvent : IDomainEvent
{
    public SeniorityStateCreatedDomainEvent(SeniorityState seniorityState)
    {
        SeniorityState = seniorityState;
    }

    public SeniorityState SeniorityState { get; }
}