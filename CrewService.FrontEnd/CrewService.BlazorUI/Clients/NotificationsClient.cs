using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Google.Protobuf.WellKnownTypes;

namespace CrewService.BlazorUI.Clients;

/// <summary>
/// gRPC client for the recipient-facing notification surface. All operations target the
/// current authenticated employee ("me"); the employee is resolved server-side from claims,
/// so no employee id is supplied by the client.
/// </summary>
public sealed class NotificationsClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<NotificationsClient> logger)
    : BaseGrpcClient<NotificationsSrvc.NotificationsSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new NotificationsSrvc.NotificationsSrvcClient(callInvoker), logger)
{
    /// <summary>Full notification history for the current employee, newest first.</summary>
    public async Task<GetNotificationsResponse> GetMyNotificationsAsync()
    {
        try
        {
            return await _client.GetMyNotificationsAsync(new Empty());
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    /// <summary>Open notices for the current employee (acknowledgement required, unconfirmed).</summary>
    public async Task<GetNotificationsResponse> GetMyUnacknowledgedAsync()
    {
        try
        {
            return await _client.GetMyUnacknowledgedAsync(new Empty());
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    /// <summary>Count of open notices for the current employee (notification badge).</summary>
    public async Task<int> GetMyUnacknowledgedCountAsync()
    {
        try
        {
            var response = await _client.GetMyUnacknowledgedCountAsync(new Empty());
            return response.Count;
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    /// <summary>Records the current employee's electronic acknowledgement of one of their notices.</summary>
    public async Task<NotificationResponse> AcknowledgeNotificationAsync(long ctrlNbr)
    {
        try
        {
            return await _client.AcknowledgeNotificationAsync(new AcknowledgeNotificationRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
