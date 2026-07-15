using CrewService.Domain.Interfaces;

namespace CrewService.Application.Notifications;

public interface INotificationAcknowledgementEnforcer
{
    Task<int> GetBlockingOpenCountAsync(string grpcMethod, CancellationToken ct = default);
}

public sealed class NotificationAcknowledgementEnforcer(
    NotificationQueryService notificationQueryService,
    ICurrentUserService currentUserService)
    : INotificationAcknowledgementEnforcer
{
    private static readonly string[] ExemptServices =
    [
        "NotificationsSrvc",
        "BootstrapSrvc",
        "AuthSrvc",
        "AuthorizationSrvc"
    ];

    private static readonly string[] ExemptServiceMethods =
    [
        "EmployeeSrvc/GetEmployeeByNumber"
    ];

    public async Task<int> GetBlockingOpenCountAsync(string grpcMethod, CancellationToken ct = default)
    {
        if (IsExempt(grpcMethod))
            return 0;

        if (currentUserService.GetUserId() == Guid.Empty)
            return 0;

        try
        {
            return await notificationQueryService.GetMyUnacknowledgedCountAsync(ct);
        }
        catch (InvalidOperationException)
        {
            // Non-employee accounts are not subject to acknowledgement gating.
            return 0;
        }
    }

    private static bool IsExempt(string grpcMethod)
    {
        var method = grpcMethod.TrimStart('/');

        for (var i = 0; i < ExemptServiceMethods.Length; i++)
        {
            if (method.EndsWith(ExemptServiceMethods[i], StringComparison.Ordinal))
                return true;
        }

        for (var i = 0; i < ExemptServices.Length; i++)
        {
            if (method.Contains($"{ExemptServices[i]}/", StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
