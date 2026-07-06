using System.Text.Json;
using CrewService.Application.Employees;
using CrewService.Application.Notifications;
using CrewService.Application.Qualifications;
using CrewService.Application.VacancyAssignment;
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
        var qualificationService = scope.ServiceProvider.GetRequiredService<QualificationReactiveService>();
        var employeeService = scope.ServiceProvider.GetRequiredService<EmployeeReactiveService>();
        var notificationDelivery = scope.ServiceProvider.GetRequiredService<INotificationDeliveryService>();
        var vacancyRepostService = scope.ServiceProvider.GetRequiredService<IVacancyRepostService>();

        foreach (var domainEvent in events)
        {
            switch (domainEvent.EventType)
            {
                case "EmployeeCreatedDomainEvent":
                    await employeeService.HandleEmployeeCreatedAsync(domainEvent, cancellationToken);
                    break;

                case "OnDutyRecordCreatedDomainEvent":
                {
                    var employeeCtrlNbr = TryGetLongProperty(domainEvent, "employeeCtrlNbr");
                    if (employeeCtrlNbr is null)
                        continue;

                    await qualificationService.HandleOnDutyRecordCreatedAsync(ControlNumber.Create(employeeCtrlNbr.Value), cancellationToken);
                    break;
                }

                case "EmployeeNotifiedDomainEvent":
                {
                    var request = TryBuildDeliveryRequest(domainEvent);
                    if (request is null)
                        continue;

                    await notificationDelivery.DeliverAsync(request, cancellationToken);
                    break;
                }

                case "PositionAssignmentVacatedDomainEvent":
                {
                    var staffablePositionCtrlNbr = TryGetLongProperty(domainEvent, "staffablePositionCtrlNbr");
                    if (staffablePositionCtrlNbr is null)
                        continue;

                    var previousIncumbentCtrlNbr = TryGetLongProperty(domainEvent, "employeeCtrlNbr");

                    await vacancyRepostService.RepostVacatedPositionAsync(
                        ControlNumber.Create(staffablePositionCtrlNbr.Value),
                        previousIncumbentCtrlNbr is null ? null : ControlNumber.Create(previousIncumbentCtrlNbr.Value),
                        cancellationToken);
                    break;
                }
            }
        }
    }

    private NotificationDeliveryRequest? TryBuildDeliveryRequest(DomainEvent domainEvent)
    {
        if (string.IsNullOrWhiteSpace(domainEvent.PayloadJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(domainEvent.PayloadJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("notificationCtrlNbr", out var notif) || !notif.TryGetInt64(out var notificationCtrlNbr))
                return null;
            if (!root.TryGetProperty("railroadCtrlNbr", out var rr) || !rr.TryGetInt64(out var railroadCtrlNbr))
                return null;
            if (!root.TryGetProperty("employeeCtrlNbr", out var emp) || !emp.TryGetInt64(out var employeeCtrlNbr))
                return null;

            var category = root.TryGetProperty("category", out var cat) ? cat.GetString() ?? string.Empty : string.Empty;
            var requiresAck = root.TryGetProperty("requiresAcknowledgement", out var ack) && ack.GetBoolean();

            return new NotificationDeliveryRequest(notificationCtrlNbr, railroadCtrlNbr, employeeCtrlNbr, category, requiresAck);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to build notification delivery request from domain event. EventType: {EventType}, EventId: {EventId}",
                domainEvent.EventType, domainEvent.EventId);
            return null;
        }
    }

    private (ControlNumber? Aggregate, ControlNumber? Client) TryGetAggregateAndClientCtrlNbr(DomainEvent domainEvent)
    {
        if (string.IsNullOrWhiteSpace(domainEvent.PayloadJson))
            return (null, null);

        try
        {
            using var doc = JsonDocument.Parse(domainEvent.PayloadJson);
            doc.RootElement.TryGetProperty("aggregateCtrlNbr", out var aggProp);
            doc.RootElement.TryGetProperty("clientCtrlNbr", out var clientProp);

            if (!aggProp.TryGetInt64(out var agg) || !clientProp.TryGetInt64(out var client))
                return (null, null);

            return (ControlNumber.Create(agg), ControlNumber.Create(client));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to parse payload from domain event. EventType: {EventType}, EventId: {EventId}",
                domainEvent.EventType, domainEvent.EventId);
            return (null, null);
        }
    }

    private long? TryGetLongProperty(DomainEvent domainEvent, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(domainEvent.PayloadJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(domainEvent.PayloadJson);
            if (!doc.RootElement.TryGetProperty(propertyName, out var prop))
                return null;

            return prop.TryGetInt64(out var value) ? value : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to parse {Property} from domain event. EventType: {EventType}, EventId: {EventId}",
                propertyName, domainEvent.EventType, domainEvent.EventId);
            return null;
        }
    }
}