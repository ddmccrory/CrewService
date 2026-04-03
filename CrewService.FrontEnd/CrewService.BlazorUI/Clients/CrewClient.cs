using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class CrewClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<CrewClient> logger)
    : BaseGrpcClient<CrewsSrvc.CrewsSrvcClient>(channelProvider, tokenProvider, callInvoker => new CrewsSrvc.CrewsSrvcClient(callInvoker), logger)
{
    // ── Crews ──

    public async Task<GetAllCrewsResponse> GetAllCrewsAsync(long railroadCtrlNbr, long homeGroupCtrlNbr = 0, string? crewType = null)
    {
        try
        {
            return await _client.GetAllCrewsAsync(new GetAllCrewsRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr,
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

    public async Task<CrewResponse> CreateCrewAsync(string crewType, long homeGroupCtrlNbr, string name, bool isActive, long departmentCtrlNbr = 0)
    {
        try
        {
            return await _client.CreateCrewAsync(new CreateCrewRequest
            {
                CrewType = crewType,
                HomeGroupCtrlNbr = homeGroupCtrlNbr,
                Name = name,
                IsActive = isActive,
                DepartmentCtrlNbr = departmentCtrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CrewResponse> UpdateCrewAsync(long ctrlNbr, string name, bool isActive, long departmentCtrlNbr = 0)
    {
        try
        {
            return await _client.UpdateCrewAsync(new UpdateCrewRequest
            {
                CtrlNbr = ctrlNbr,
                Name = name,
                IsActive = isActive,
                DepartmentCtrlNbr = departmentCtrlNbr
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

    public async Task<CrewPositionResponse> CreateCrewPositionAsync(long crewCtrlNbr, long craftRoleCtrlNbr, int displayOrder)
    {
        try
        {
            return await _client.CreateCrewPositionAsync(new CreateCrewPositionRequest
            {
                CrewCtrlNbr = crewCtrlNbr,
                CraftRoleCtrlNbr = craftRoleCtrlNbr,
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

    public async Task<GetCrewAssignmentsResponse> GetCrewAssignmentsByAssignmentAsync(long assignmentCtrlNbr)
    {
        try
        {
            return await _client.GetCrewAssignmentsByAssignmentAsync(new GetCrewAssignmentsByAssignmentRequest { AssignmentCtrlNbr = assignmentCtrlNbr });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CrewAssignmentResponse> CreateCrewAssignmentAsync(long crewCtrlNbr, long assignmentCtrlNbr, int daysOfWeekMask, string startUtc, string? endUtc = null)
    {
        try
        {
            return await _client.CreateCrewAssignmentAsync(new CreateCrewAssignmentRequest
            {
                CrewCtrlNbr = crewCtrlNbr,
                AssignmentCtrlNbr = assignmentCtrlNbr,
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
