using CrewService.Application.Notifications;
using CrewService.Application.Authorization;
using CrewService.Domain.Models.UserAccess;
using CrewService.Application.Time;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;
using CrewService.Presentation.Services;
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
    private static readonly string[] NotificationReviewRoles =
    [
        Roles.SystemAdmin,
        Roles.ParentAdmin,
        Roles.RailroadAdmin,
        "CrewManager",
        "Dispatcher"
    ];

    public override async Task<GetNotificationsResponse> GetMyNotifications(Empty request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<NotificationQueryService>();
        var items = await svc.GetMyNotificationsAsync(context.CancellationToken);
        return await MapListAsync(items, context.CancellationToken);
    }

    public override async Task<GetNotificationsResponse> GetMyUnacknowledged(Empty request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<NotificationQueryService>();
        var items = await svc.GetMyUnacknowledgedAsync(context.CancellationToken);
        return await MapListAsync(items, context.CancellationToken);
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
            var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();
            var tz = await clock.GetWorkAreaTimeZoneAsync(notification.RailroadCtrlNbr, context.CancellationToken);
            return MapNotification(notification, clock, tz);
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

    public override async Task<NotificationResponse> RecordManualAcknowledgement(
        RecordManualAcknowledgementRequest request,
        ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "CtrlNbr must be greater than zero."));

        if (!TryParseAcknowledgementMethod(request.Method, out var method))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Method must be PhoneCall, ReturnCall, CalledIn, Verbal, Automatic, or Electronic."));

        var svc = serviceProvider.GetRequiredService<NotificationQueryService>();
        var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();

        try
        {
            var notification = await svc.RecordManualAcknowledgementAsync(
                ControlNumber.Create(request.CtrlNbr),
                method,
                request.Confirmed,
                string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber,
                string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes,
                context.CancellationToken);

            var tz = await clock.GetWorkAreaTimeZoneAsync(notification.RailroadCtrlNbr, context.CancellationToken);
            return MapNotification(notification, clock, tz);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GetNotificationsResponse> GetRailroadNotifications(RailroadNotificationsRequest request, ServerCallContext context)
    {
        EnsureNotificationReviewAccess(context, "You do not have permission to review railroad notifications.");

        if (request.RailroadCtrlNbr <= 0) return new GetNotificationsResponse();

        var svc = serviceProvider.GetRequiredService<NotificationQueryService>();
        var nameService = serviceProvider.GetRequiredService<EmployeeNameService>();
        var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();
        var items = await svc.GetRailroadNotificationsAsync(
            ControlNumber.Create(request.RailroadCtrlNbr), context.CancellationToken);

        var names = await nameService.GetEmployeeInfoBatchAsync(items.Select(n => n.EmployeeCtrlNbr));
        var zoneCache = new Dictionary<long, TimeZoneInfo?>();

        var resp = new GetNotificationsResponse();
        foreach (var n in items)
        {
            var tz = await ResolveZoneAsync(clock, zoneCache, n.RailroadCtrlNbr, context.CancellationToken);
            var mapped = MapNotification(n, clock, tz);
            mapped.EmployeeName = names.TryGetValue(n.EmployeeCtrlNbr, out var info) ? info.FullNameLnf : string.Empty;
            resp.Notifications.Add(mapped);
        }
        return resp;
    }

    public override async Task<GetNotificationTypeConfigsResponse> GetNotificationTypeConfigs(
        NotificationTypeConfigsRequest request,
        ServerCallContext context)
    {
        EnsureAdmin(context, "Only administrators can view notification type configuration.");

        if (request.RailroadCtrlNbr <= 0)
            return new GetNotificationTypeConfigsResponse();

        var svc = serviceProvider.GetRequiredService<NotificationTypeConfigAppService>();
        var items = await svc.GetByRailroadAsync(ControlNumber.Create(request.RailroadCtrlNbr), context.CancellationToken);

        var response = new GetNotificationTypeConfigsResponse();
        response.Configs.AddRange(items.Select(MapTypeConfig));
        return response;
    }

    public override async Task<NotificationTypeConfigResponse> UpsertNotificationTypeConfig(
        UpsertNotificationTypeConfigRequest request,
        ServerCallContext context)
    {
        EnsureAdmin(context, "Only administrators can update notification type configuration.");

        if (request.RailroadCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "RailroadCtrlNbr must be greater than zero."));

        if (string.IsNullOrWhiteSpace(request.Key))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Key is required."));

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "DisplayName is required."));

        if (!System.Enum.TryParse<NotificationAudience>(request.Audience, ignoreCase: true, out var audience))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Audience must be Employee, Dispatcher, or Both."));

        var svc = serviceProvider.GetRequiredService<NotificationTypeConfigAppService>();
        var config = await svc.UpsertAsync(
            ControlNumber.Create(request.RailroadCtrlNbr),
            request.Key,
            request.DisplayName,
            request.IsEnabled,
            request.RequiresAcknowledgementDefault,
            audience,
            request.SendInApp,
            request.SendEmail,
            request.SendText,
            request.SendExternalApi,
            context.CancellationToken);

        return MapTypeConfig(config);
    }

    public override async Task<UnacknowledgedCountResponse> GetRailroadUnacknowledgedCount(RailroadNotificationsRequest request, ServerCallContext context)
    {
        EnsureNotificationReviewAccess(context, "You do not have permission to review railroad notifications.");

        if (request.RailroadCtrlNbr <= 0) return new UnacknowledgedCountResponse { Count = 0 };

        var svc = serviceProvider.GetRequiredService<NotificationQueryService>();
        var count = await svc.GetRailroadUnacknowledgedCountAsync(
            ControlNumber.Create(request.RailroadCtrlNbr), context.CancellationToken);
        return new UnacknowledgedCountResponse { Count = count };
    }

    public override async Task<GetNotificationsResponse> GetEmployeeNotifications(EmployeeNotificationsRequest request, ServerCallContext context)
    {
        if (request.EmployeeCtrlNbr <= 0) return new GetNotificationsResponse();

        await EnsureEmployeeNotificationAccessAsync(
            request.EmployeeCtrlNbr,
            context,
            "You do not have permission to review employee notifications.");

        var svc = serviceProvider.GetRequiredService<NotificationQueryService>();
        var nameService = serviceProvider.GetRequiredService<EmployeeNameService>();
        var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();
        var items = await svc.GetEmployeeNotificationsAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);

        var names = await nameService.GetEmployeeInfoBatchAsync(items.Select(n => n.EmployeeCtrlNbr));
        var zoneCache = new Dictionary<long, TimeZoneInfo?>();

        var resp = new GetNotificationsResponse();
        foreach (var n in items)
        {
            var tz = await ResolveZoneAsync(clock, zoneCache, n.RailroadCtrlNbr, context.CancellationToken);
            var mapped = MapNotification(n, clock, tz);
            mapped.EmployeeName = names.TryGetValue(n.EmployeeCtrlNbr, out var info) ? info.FullNameLnf : string.Empty;
            resp.Notifications.Add(mapped);
        }
        return resp;
    }

    private async Task EnsureEmployeeNotificationAccessAsync(long requestedEmployeeCtrlNbr, ServerCallContext context, string message)
    {
        if (requestedEmployeeCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "EmployeeCtrlNbr must be greater than zero."));

        var user = context.GetHttpContext().User;
        var allowOnBehalf = NotificationReviewRoles.Any(user.IsInRole);

        var actorContextResolver = serviceProvider.GetRequiredService<IRequestActorContextResolver>();
        var actorPolicy = serviceProvider.GetRequiredService<IRequestActorContextPolicy>();
        var actorContext = await actorContextResolver.ResolveAsync(
            requestedEmployeeCtrlNbr,
            ct: context.CancellationToken);

        if (!actorPolicy.CanAccessRequestedEmployee(actorContext, allowOnBehalf))
            throw new RpcException(new Status(StatusCode.PermissionDenied, message));
    }

    private async Task<GetNotificationsResponse> MapListAsync(
        IReadOnlyList<EmployeeNotification> items, CancellationToken ct)
    {
        var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();
        var zoneCache = new Dictionary<long, TimeZoneInfo?>();
        var resp = new GetNotificationsResponse();
        foreach (var n in items)
        {
            var tz = await ResolveZoneAsync(clock, zoneCache, n.RailroadCtrlNbr, ct);
            resp.Notifications.Add(MapNotification(n, clock, tz));
        }
        return resp;
    }

    private static async Task<TimeZoneInfo?> ResolveZoneAsync(
        IWorkAreaClock clock, Dictionary<long, TimeZoneInfo?> cache, ControlNumber railroadCtrlNbr, CancellationToken ct)
    {
        if (cache.TryGetValue(railroadCtrlNbr.Value, out var cached))
            return cached;
        var tz = await clock.GetWorkAreaTimeZoneAsync(railroadCtrlNbr, ct);
        cache[railroadCtrlNbr.Value] = tz;
        return tz;
    }

    private static NotificationResponse MapNotification(EmployeeNotification n, IWorkAreaClock clock, TimeZoneInfo? tz)
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
            CreatedAtLocal = clock.FormatLocalIso(n.CreatedAtUtc, tz),
            AttemptCount = n.Acknowledgements.Count,
        };

        if (n.EffectiveAtUtc.HasValue)
        {
            resp.EffectiveAt = Timestamp.FromDateTime(DateTime.SpecifyKind(n.EffectiveAtUtc.Value, DateTimeKind.Utc));
            resp.EffectiveAtLocal = clock.FormatLocalIso(n.EffectiveAtUtc.Value, tz);
        }

        var acceptedAtUtc = n.Acknowledgements
            .Where(a => a.Confirmed)
            .OrderByDescending(a => a.NotifiedAtUtc)
            .Select(a => (DateTime?)a.NotifiedAtUtc)
            .FirstOrDefault();
        if (acceptedAtUtc.HasValue)
        {
            resp.AcceptedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(acceptedAtUtc.Value, DateTimeKind.Utc));
            resp.AcceptedAtLocal = clock.FormatLocalIso(acceptedAtUtc.Value, tz);
        }

        if (n.Subject is not null)
        {
            resp.SubjectType = n.Subject.SubjectType;
            resp.SubjectCtrlNbr = n.Subject.SubjectCtrlNbr.Value;
        }

        return resp;
    }

    private static NotificationTypeConfigResponse MapTypeConfig(NotificationTypeConfig config) => new()
    {
        CtrlNbr = config.CtrlNbr.Value,
        RailroadCtrlNbr = config.RailroadCtrlNbr.Value,
        Key = config.Key,
        DisplayName = config.DisplayName,
        IsEnabled = config.IsEnabled,
        RequiresAcknowledgementDefault = config.RequiresAcknowledgementDefault,
        Audience = config.Audience.ToString(),
        SendInApp = config.SendInApp,
        SendEmail = config.SendEmail,
        SendText = config.SendText,
        SendExternalApi = config.SendExternalApi
    };

    private static bool TryParseAcknowledgementMethod(string input, out AcknowledgementMethod method)
    {
        method = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input.Trim().Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);
        return System.Enum.TryParse(normalized, ignoreCase: true, out method);
    }

    private static void EnsureAdmin(ServerCallContext context, string message)
    {
        var user = context.GetHttpContext().User;
        if (!user.IsInRole(Roles.SystemAdmin)
            && !user.IsInRole(Roles.ParentAdmin)
            && !user.IsInRole(Roles.RailroadAdmin))
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, message));
        }
    }

    private static void EnsureNotificationReviewAccess(ServerCallContext context, string message)
    {
        var user = context.GetHttpContext().User;
        if (!NotificationReviewRoles.Any(user.IsInRole))
            throw new RpcException(new Status(StatusCode.PermissionDenied, message));
    }
}
