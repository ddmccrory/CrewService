using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class BackgroundServicesClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<BackgroundServicesClient> logger)
    : BaseGrpcClient<BackgroundServicesSrvc.BackgroundServicesSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new BackgroundServicesSrvc.BackgroundServicesSrvcClient(callInvoker), logger)
{
    public async Task<GetWorkerSchedulesResponse> GetWorkerSchedulesAsync(long railroadCtrlNbr, string? workerType = null)
    {
        try
        {
            var request = new GetWorkerSchedulesRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr
            };
            if (!string.IsNullOrWhiteSpace(workerType))
                request.WorkerType = workerType;
            return await _client.GetWorkerSchedulesAsync(request);
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<WorkerScheduleResponse> UpdateScheduleAsync(long ctrlNbr, bool isEnabled, string? cronExpression = null)
    {
        try
        {
            var request = new UpdateScheduleRequest
            {
                CtrlNbr = ctrlNbr,
                IsEnabled = isEnabled
            };

            if (!string.IsNullOrWhiteSpace(cronExpression))
                request.CronExpression = cronExpression;

            return await _client.UpdateScheduleAsync(request);
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetExecutionLogsResponse> GetExecutionLogsAsync(long workerScheduleCtrlNbr, int limit = 20)
    {
        try
        {
            return await _client.GetExecutionLogsAsync(new GetExecutionLogsRequest
            {
                WorkerScheduleCtrlNbr = workerScheduleCtrlNbr,
                Limit = limit
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }
}