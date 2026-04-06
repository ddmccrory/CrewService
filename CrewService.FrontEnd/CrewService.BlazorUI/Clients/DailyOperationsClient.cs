using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class DailyOperationsClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<DailyOperationsClient> logger)
    : BaseGrpcClient<DailyOperationsSrvc.DailyOperationsSrvcClient>(channelProvider, tokenProvider, callInvoker => new DailyOperationsSrvc.DailyOperationsSrvcClient(callInvoker), logger)
{
    public async Task<GetCallSheetResponse> GetCallSheetAsync(long workAreaGroupCtrlNbr, string targetDate, bool includeClosed = false)
    {
        try
        {
            return await _client.GetCallSheetAsync(new GetCallSheetRequest
            {
                WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
                TargetDate = targetDate,
                IncludeClosed = includeClosed
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> GenerateCallSheetAsync(long workAreaGroupCtrlNbr, long shiftDefinitionCtrlNbr, string targetDate, long departmentCtrlNbr = 0)
    {
        try
        {
            var req = new GenerateCallSheetRequest
            {
                WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
                ShiftDefinitionCtrlNbr = shiftDefinitionCtrlNbr,
                TargetDate = targetDate
            };
            if (departmentCtrlNbr > 0) req.DepartmentCtrlNbr = departmentCtrlNbr;
            return await _client.GenerateCallSheetAsync(req);
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> RefreshShiftInstanceAsync(long ctrlNbr)
    {
        try
        {
            return await _client.RefreshShiftInstanceAsync(new RefreshShiftInstanceRequest
            {
                CtrlNbr = ctrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<DeleteResponse> CloseShiftInstanceAsync(long ctrlNbr)
    {
        try
        {
            return await _client.CloseShiftInstanceAsync(new CloseShiftInstanceRequest
            {
                CtrlNbr = ctrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> ReopenShiftInstanceAsync(long ctrlNbr)
    {
        try
        {
            return await _client.ReopenShiftInstanceAsync(new ReopenShiftInstanceRequest
            {
                CtrlNbr = ctrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }
}
