using CrewService.Application.Notifications;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

/// <summary>
/// Thin gRPC facade over <see cref="NotificationQueryService"/>. All operations target the
/// current authenticated employee ("me"); the employee is resolved server-side from claims.
/// </summary>
public class NotificationsService(IServiceProvider serviceProvider)
    : NotificationsSrvc.NotificationsSrvcBase
{
    public override async Task<GetNotificationsResponse> GetMyNotifications(Empty request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<NotificationQueryService>();
        var items = await svc.GetMyNotificationsAsync(context.CancellationToken);
        return MapList(items);
    }

    public override async Task<GetNotificationsResponse> GetMyUnacknowledged(Empty request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<NotificationQueryService>();
        var items = await svc.GetMyUnacknowledgedAsync(context.CancellationToken);
        return MapList(items);
    }

    public override async Task<UnacknowledgedCountResponse> GetMyUnacknowledgedCount(Empty request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<NotificationQueryService>();
        var count = await svc.GetMyUnacknowledgedCountAsync(context.CancellationToken);
        return new UnacknowledgedCountResponse { Count = count };
    }

    public override async Task<NotificationResponse> AcknowledgeNotification(
        AcknowledgeNotificationRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<NotificationQueryService>();
        try
        {
            var notification = await svc.AcknowledgeAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapNotification(notification);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
    }

    private static GetNotificationsResponse MapList(IReadOnlyList<EmployeeNotification> items)
    {
        var resp = new GetNotificationsResponse();
        foreach (var n in items)
            resp.Notifications.Add(MapNotification(n));
        return resp;
    }

    private static NotificationResponse MapNotification(EmployeeNotification n)
    {
        var resp = new NotificationResponse
        {
            CtrlNbr = n.CtrlNbr.Value,
            RailroadCtrlNbr = n.RailroadCtrlNbr.Value,
            EmployeeCtrlNbr = n.EmployeeCtrlNbr.Value,
            Category = n.Category,
            Message = n.Message,
            RequiresAcknowledgement = n.RequiresAcknowledgement,
            IsAcknowledged = n.IsAcknowledged,
            CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(n.CreatedAtUtc, DateTimeKind.Utc)),
        };

        if (n.EffectiveAtUtc.HasValue)
            resp.EffectiveAt = Timestamp.FromDateTime(DateTime.SpecifyKind(n.EffectiveAtUtc.Value, DateTimeKind.Utc));

        if (n.Subject is not null)
        {
            resp.SubjectType = n.Subject.SubjectType;
            resp.SubjectCtrlNbr = n.Subject.SubjectCtrlNbr.Value;
        }

        return resp;
    }
}
