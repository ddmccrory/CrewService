using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class VacancyAssignmentClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<VacancyAssignmentClient> logger)
    : BaseGrpcClient<VacancyAssignmentSrvc.VacancyAssignmentSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new VacancyAssignmentSrvc.VacancyAssignmentSrvcClient(callInvoker), logger)
{
    public async Task<GetBoardSnapshotTimelineResponse> GetBoardSnapshotTimelineAsync(long shiftInstanceCtrlNbr)
    {
        try
        {
            return await _client.GetBoardSnapshotTimelineAsync(new GetBoardSnapshotTimelineRequest
            {
                ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetBoardSnapshotDetailResponse> GetBoardSnapshotDetailAsync(long snapshotCtrlNbr)
    {
        try
        {
            return await _client.GetBoardSnapshotDetailAsync(new GetBoardSnapshotDetailRequest
            {
                SnapshotCtrlNbr = snapshotCtrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetBoardSelectionDecisionsResponse> GetBoardSelectionDecisionsAsync(long shiftInstanceCtrlNbr)
    {
        try
        {
            return await _client.GetBoardSelectionDecisionsAsync(new GetBoardSelectionDecisionsRequest
            {
                ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetCurrentCallBoardResponse> GetCurrentCallBoardAsync(
        long workAreaGroupCtrlNbr,
        long craftCtrlNbr,
        string boardType,
        DateOnly targetDate)
    {
        try
        {
            return await _client.GetCurrentCallBoardAsync(new GetCurrentCallBoardRequest
            {
                WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
                CraftCtrlNbr = craftCtrlNbr,
                BoardType = boardType,
                TargetDateYyyyMmDd = targetDate.ToString("yyyy-MM-dd")
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }
}
