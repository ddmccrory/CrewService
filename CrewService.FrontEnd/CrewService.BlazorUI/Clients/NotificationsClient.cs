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

    /// <summary>Records a manual (dispatcher) acknowledgement attempt for a notification.</summary>
    public async Task<NotificationResponse> RecordManualAcknowledgementAsync(
        long ctrlNbr,
        string method,
        bool confirmed,
        string? phoneNumber = null,
        string? notes = null)
    {
        try
        {
            return await _client.RecordManualAcknowledgementAsync(new RecordManualAcknowledgementRequest
            {
                CtrlNbr = ctrlNbr,
                Method = method,
                Confirmed = confirmed,
                PhoneNumber = phoneNumber ?? string.Empty,
                Notes = notes ?? string.Empty
            });
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

    /// <summary>Read-only railroad-wide notification feed (newest first) for the reference menu.</summary>
    public async Task<GetNotificationsResponse> GetRailroadNotificationsAsync(long railroadCtrlNbr)
    {
        try
        {
            return await _client.GetRailroadNotificationsAsync(new RailroadNotificationsRequest { RailroadCtrlNbr = railroadCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    /// <summary>Count of open notices across the railroad (reference-menu badge).</summary>
    public async Task<int> GetRailroadUnacknowledgedCountAsync(long railroadCtrlNbr)
    {
        try
        {
            var response = await _client.GetRailroadUnacknowledgedCountAsync(new RailroadNotificationsRequest { RailroadCtrlNbr = railroadCtrlNbr });
            return response.Count;
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    /// <summary>Read-only feed of a single employee's notifications (newest first) for managerial review.</summary>
    public async Task<GetNotificationsResponse> GetEmployeeNotificationsAsync(long employeeCtrlNbr)
    {
        try
        {
            return await _client.GetEmployeeNotificationsAsync(new EmployeeNotificationsRequest { EmployeeCtrlNbr = employeeCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetNotificationTypeConfigsResponse> GetNotificationTypeConfigsAsync(long railroadCtrlNbr)
    {
        try
        {
            return await _client.GetNotificationTypeConfigsAsync(new NotificationTypeConfigsRequest { RailroadCtrlNbr = railroadCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<NotificationTypeConfigResponse> UpsertNotificationTypeConfigAsync(
        long railroadCtrlNbr,
        string key,
        string displayName,
        bool isEnabled,
        bool requiresAcknowledgementDefault,
        string audience,
        bool sendInApp,
        bool sendEmail,
        bool sendText,
        bool sendExternalApi,
        string? messageTemplate)
    {
        try
        {
            return await _client.UpsertNotificationTypeConfigAsync(new UpsertNotificationTypeConfigRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr,
                Key = key,
                DisplayName = displayName,
                IsEnabled = isEnabled,
                RequiresAcknowledgementDefault = requiresAcknowledgementDefault,
                Audience = audience,
                SendInApp = sendInApp,
                SendEmail = sendEmail,
                SendText = sendText,
                SendExternalApi = sendExternalApi,
                MessageTemplate = messageTemplate ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
