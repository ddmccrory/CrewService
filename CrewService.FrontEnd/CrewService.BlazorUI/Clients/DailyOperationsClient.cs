using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class DailyOperationsClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<DailyOperationsClient> logger)
    : BaseGrpcClient<DailyOperationsSrvc.DailyOperationsSrvcClient>(channelProvider, tokenProvider, callInvoker => new DailyOperationsSrvc.DailyOperationsSrvcClient(callInvoker), logger)
{
    public async Task<GetCallSheetResponse> GetCallSheetAsync(long workAreaGroupCtrlNbr, string targetDate)
    {
        try
        {
            return await _client.GetCallSheetAsync(new GetCallSheetRequest
            {
                WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
                TargetDate = targetDate
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> GenerateCallSheetAsync(long workAreaGroupCtrlNbr, long shiftDefinitionCtrlNbr, string targetDate)
    {
        try
        {
            return await _client.GenerateCallSheetAsync(new GenerateCallSheetRequest
            {
                WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
                ShiftDefinitionCtrlNbr = shiftDefinitionCtrlNbr,
                TargetDate = targetDate
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }
}
