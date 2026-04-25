using System.Text.Json;
using CrewService.Application.Qualifications;
using CrewService.Domain.DomainEvents;
using CrewService.Domain.Interfaces;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CrewService.Infrastructure.Reactive;

public sealed class DomainEventReactor(
    IServiceScopeFactory scopeFactory,
    ILogger<DomainEventReactor> logger) : IDomainEventReactor
{
    public async Task ReactAsync(IReadOnlyList<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
            return;

        using var scope = scopeFactory.CreateScope();
        var reactiveService = scope.ServiceProvider.GetRequiredService<QualificationReactiveService>();

        foreach (var domainEvent in events)
        {
            switch (domainEvent.EventType)
            {
                case "OnDutyRecordCreatedDomainEvent":
                {
                    var employeeCtrlNbr = TryGetEmployeeCtrlNbr(domainEvent);
                    if (employeeCtrlNbr is null)
                        continue;

                    await reactiveService.HandleOnDutyRecordCreatedAsync(employeeCtrlNbr, cancellationToken);
                    break;
                }

            }
        }
    }

    private ControlNumber? TryGetEmployeeCtrlNbr(DomainEvent domainEvent)
    {
        if (string.IsNullOrWhiteSpace(domainEvent.PayloadJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(domainEvent.PayloadJson);
            if (!doc.RootElement.TryGetProperty("employeeCtrlNbr", out var employeeProp))
                return null;

            if (!employeeProp.TryGetInt64(out var employeeCtrlNbr))
                return null;

            return ControlNumber.Create(employeeCtrlNbr);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to parse employee control number from domain event payload. EventType: {EventType}, EventId: {EventId}",
                domainEvent.EventType,
                domainEvent.EventId);
            return null;
        }
    }
}
