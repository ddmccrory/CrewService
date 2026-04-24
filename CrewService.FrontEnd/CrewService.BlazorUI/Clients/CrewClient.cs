using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class CrewClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<CrewClient> logger)
    : BaseGrpcClient<CrewsSrvc.CrewsSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new CrewsSrvc.CrewsSrvcClient(callInvoker), logger)
{
    // ── Crews ──

    public async Task<GetAllCrewsResponse> GetAllCrewsAsync(long railroadCtrlNbr, long workAreaCtrlNbr = 0, string? crewType = null)
    {
        try
        {
            return await _client.GetAllCrewsAsync(new GetAllCrewsRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr,
                WorkAreaCtrlNbr = workAreaCtrlNbr,
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

    public async Task<CrewResponse> CreateCrewAsync(string crewType, long workAreaCtrlNbr, string name, bool isActive, long departmentCtrlNbr = 0, string? effectiveDate = null, string? abolishedDate = null)
    {
        try
        {
            return await _client.CreateCrewAsync(new CreateCrewRequest
            {
                CrewType = crewType,
                WorkAreaCtrlNbr = workAreaCtrlNbr,
                Name = name,
                IsActive = isActive,
                DepartmentCtrlNbr = departmentCtrlNbr,
                EffectiveDate = effectiveDate ?? string.Empty,
                AbolishedDate = abolishedDate ?? string.Empty
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CrewResponse> UpdateCrewAsync(long ctrlNbr, string name, bool isActive, long departmentCtrlNbr = 0, string? effectiveDate = null, string? abolishedDate = null, string? crewType = null)
    {
        try
        {
            return await _client.UpdateCrewAsync(new UpdateCrewRequest
            {
                CtrlNbr = ctrlNbr,
                Name = name,
                IsActive = isActive,
                DepartmentCtrlNbr = departmentCtrlNbr,
                EffectiveDate = effectiveDate ?? string.Empty,
                AbolishedDate = abolishedDate ?? string.Empty,
                CrewType = crewType ?? string.Empty
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

    public async Task<DeleteResponse> DeleteCrewPositionAsync(long ctrlNbr)
    {
        try
        {
            return await _client.DeleteCrewPositionAsync(new DeleteCrewPositionRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    // ── Crew Incumbencies ──

    public async Task<GetCrewIncumbenciesResponse> GetCrewIncumbenciesAsync(long crewPositionCtrlNbr)
    {
        try
        {
            return await _client.GetCrewIncumbenciesAsync(new GetCrewIncumbenciesRequest { CrewPositionCtrlNbr = crewPositionCtrlNbr });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CrewIncumbencyResponse> CreateCrewIncumbencyAsync(long crewPositionCtrlNbr, long employeeCtrlNbr, string startUtc)
    {
        try
        {
            return await _client.CreateCrewIncumbencyAsync(new CreateCrewIncumbencyRequest
            {
                CrewPositionCtrlNbr = crewPositionCtrlNbr,
                EmployeeCtrlNbr = employeeCtrlNbr,
                StartUtc = startUtc
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<DeleteResponse> EndCrewIncumbencyAsync(long ctrlNbr)
    {
        try
        {
            return await _client.EndCrewIncumbencyAsync(new EndCrewIncumbencyRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

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

    // ── Crew Setup Wizard ──

    public async Task<CrewSetupWizardResponse> CrewSetupWizardAsync(CrewSetupWizardRequest request)
    {
        try
        {
            return await _client.CrewSetupWizardAsync(request);
        }
        catch (Exception ex) { LogException(ex); throw; }
    }
}
