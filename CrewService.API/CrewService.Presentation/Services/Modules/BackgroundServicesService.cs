using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class BackgroundServicesService()
    : BackgroundServicesSrvc.BackgroundServicesSrvcBase
{
    public override Task<GetWorkerSchedulesResponse> GetWorkerSchedules(
        GetWorkerSchedulesRequest request, ServerCallContext context)
    {
        return Task.FromResult(new GetWorkerSchedulesResponse());
    }

    public override Task<WorkerScheduleResponse> UpdateSchedule(
        UpdateScheduleRequest request, ServerCallContext context)
    {
        return Task.FromResult(new WorkerScheduleResponse
        {
            CtrlNbr = request.CtrlNbr,
            IsEnabled = request.IsEnabled,
        });
    }

    public override Task<GetExecutionLogsResponse> GetExecutionLogs(
        GetExecutionLogsRequest request, ServerCallContext context)
    {
        return Task.FromResult(new GetExecutionLogsResponse());
    }
}
