using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class CrewClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<CrewClient> logger)
    : BaseGrpcClient<CrewsSrvc.CrewsSrvcClient>(channelProvider, tokenProvider, callInvoker => new CrewsSrvc.CrewsSrvcClient(callInvoker), logger)
{
    // ── Crews ──

    public async Task<GetAllCrewsResponse> GetAllCrewsAsync(long homeGroupCtrlNbr, string? crewType = null)
    {
        try
        {
            return await _client.GetAllCrewsAsync(new GetAllCrewsRequest
            {
                HomeGroupCtrlNbr = homeGroupCtrlNbr,
                CrewType = crewType ?? string.Empty
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CrewResponse> GetCrewAsync(long ctrlNbr)
    {
        try
        {
            return await _client.GetCrewAsync(new GetCrewRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CrewResponse> CreateCrewAsync(string crewType, long homeGroupCtrlNbr, string name, bool isActive)
    {
        try
        {
            return await _client.CreateCrewAsync(new CreateCrewRequest
            {
                CrewType = crewType,
                HomeGroupCtrlNbr = homeGroupCtrlNbr,
                Name = name,
                IsActive = isActive
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CrewResponse> UpdateCrewAsync(long ctrlNbr, string name, bool isActive)
    {
        try
        {
            return await _client.UpdateCrewAsync(new UpdateCrewRequest
            {
                CtrlNbr = ctrlNbr,
                Name = name,
                IsActive = isActive
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<DeleteResponse> DeleteCrewAsync(long ctrlNbr)
    {
        try
        {
            return await _client.DeleteCrewAsync(new DeleteCrewRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    // ── Crew Positions ──

    public async Task<GetCrewPositionsResponse> GetCrewPositionsAsync(long crewCtrlNbr)
    {
        try
        {
            return await _client.GetCrewPositionsAsync(new GetCrewPositionsRequest { CrewCtrlNbr = crewCtrlNbr });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CrewPositionResponse> CreateCrewPositionAsync(long crewCtrlNbr, long positionRoleCtrlNbr, int displayOrder)
    {
        try
        {
            return await _client.CreateCrewPositionAsync(new CreateCrewPositionRequest
            {
                CrewCtrlNbr = crewCtrlNbr,
                PositionRoleCtrlNbr = positionRoleCtrlNbr,
                DisplayOrder = displayOrder
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    // ── Crew Assignments ──

    public async Task<GetCrewAssignmentsResponse> GetCrewAssignmentsAsync(long crewCtrlNbr)
    {
        try
        {
            return await _client.GetCrewAssignmentsAsync(new GetCrewAssignmentsRequest { CrewCtrlNbr = crewCtrlNbr });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CrewAssignmentResponse> CreateCrewAssignmentAsync(long crewCtrlNbr, long assignmentGroupCtrlNbr, int daysOfWeekMask, string startUtc, string? endUtc = null)
    {
        try
        {
            return await _client.CreateCrewAssignmentAsync(new CreateCrewAssignmentRequest
            {
                CrewCtrlNbr = crewCtrlNbr,
                AssignmentGroupCtrlNbr = assignmentGroupCtrlNbr,
                DaysOfWeekMask = daysOfWeekMask,
                StartUtc = startUtc,
                EndUtc = endUtc ?? string.Empty
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CrewAssignmentResponse> UpdateCrewAssignmentAsync(long ctrlNbr, int daysOfWeekMask, string startUtc, string? endUtc = null)
    {
        try
        {
            return await _client.UpdateCrewAssignmentAsync(new UpdateCrewAssignmentRequest
            {
                CtrlNbr = ctrlNbr,
                DaysOfWeekMask = daysOfWeekMask,
                StartUtc = startUtc,
                EndUtc = endUtc ?? string.Empty
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<DeleteResponse> DeleteCrewAssignmentAsync(long ctrlNbr)
    {
        try
        {
            return await _client.DeleteCrewAssignmentAsync(new DeleteCrewAssignmentRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }
}
