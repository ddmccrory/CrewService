using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class RequiredPositionsStrategyClient(
    GrpcChannelProvider channelProvider,
    CircuitTokenProvider tokenProvider,
    AppContextService appContext,
    ILogger<RequiredPositionsStrategyClient> logger)
    : BaseGrpcClient<BoardsSrvc.BoardsSrvcClient>(channelProvider, tokenProvider, appContext,
        callInvoker => new BoardsSrvc.BoardsSrvcClient(callInvoker), logger)
{
    public async Task<GetAllRequiredPositionsStrategiesResponse> GetAllAsync()
    {
        try { return await _client.GetAllRequiredPositionsStrategiesAsync(new GetAllRequiredPositionsStrategiesRequest()); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<RequiredPositionsStrategyResponse> CreateAsync(CreateRequiredPositionsStrategyRequest request)
    {
        try { return await _client.CreateRequiredPositionsStrategyAsync(request); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<RequiredPositionsStrategyResponse> UpdateAsync(UpdateRequiredPositionsStrategyRequest request)
    {
        try { return await _client.UpdateRequiredPositionsStrategyAsync(request); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task DeleteAsync(long ctrlNbr)
    {
        try { await _client.DeleteRequiredPositionsStrategyAsync(new DeleteRequiredPositionsStrategyRequest { CtrlNbr = ctrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CraftStrategyResponse> AssignToCraftAsync(long craftCtrlNbr, long strategyCtrlNbr, string? parametersJson)
    {
        try
        {
            return await _client.AssignStrategyToCraftAsync(new AssignStrategyToCraftRequest
            {
                CraftCtrlNbr    = craftCtrlNbr,
                StrategyCtrlNbr = strategyCtrlNbr,
                ParametersJson  = parametersJson ?? string.Empty
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetFormulaTypesResponse> GetFormulaTypesAsync()
    {
        try { return await _client.GetFormulaTypesAsync(new GetFormulaTypesRequest()); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetCraftAssignmentsResponse> GetCraftAssignmentsAsync(long railroadCtrlNbr)
    {
        try { return await _client.GetCraftAssignmentsAsync(new GetCraftAssignmentsRequest { RailroadCtrlNbr = railroadCtrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetCraftsForAssignmentResponse> GetCraftsForAssignmentAsync(long railroadCtrlNbr, long strategyCtrlNbr)
    {
        try
        {
            return await _client.GetCraftsForAssignmentAsync(new GetCraftsForAssignmentRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr,
                StrategyCtrlNbr = strategyCtrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }
}
