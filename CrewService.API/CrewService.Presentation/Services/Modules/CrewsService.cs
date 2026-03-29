using CrewService.Domain.Modules.Crews;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class CrewsService(
    ICrewRepository crewRepository,
    ICrewPositionRepository crewPositionRepository,
    ICrewIncumbencyRepository incumbencyRepository,
    ICrewAssignmentRepository assignmentRepository) : CrewsSrvc.CrewsSrvcBase
{
    public override async Task<GetAllCrewsResponse> GetAllCrews(GetAllCrewsRequest request, ServerCallContext context)
    {
        List<Domain.Modules.Crews.Crew> crews;
        if (!string.IsNullOrEmpty(request.CrewType))
            crews = await crewRepository.GetByTypeAsync(request.CrewType);
        else if (request.HomeGroupCtrlNbr > 0)
            crews = await crewRepository.GetByHomeGroupAsync(ControlNumber.Create(request.HomeGroupCtrlNbr));
        else
            crews = await crewRepository.GetAllAsync();
        var response = new GetAllCrewsResponse { TotalCount = crews.Count };
        foreach (var c in crews)
            response.Crews.Add(MapCrew(c));
        return response;
    }

    public override async Task<CrewResponse> GetCrew(GetCrewRequest request, ServerCallContext context)
    {
        var crew = await crewRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Crew {request.CtrlNbr} not found."));
        return MapCrew(crew);
    }

    public override async Task<CrewResponse> CreateCrew(CreateCrewRequest request, ServerCallContext context)
    {
        var crew = Crew.Create(request.CrewType, request.HomeGroupCtrlNbr, request.Name, request.IsActive);
        await crewRepository.AddAsync(crew);
        return MapCrew(crew);
    }

    public override async Task<CrewResponse> UpdateCrew(UpdateCrewRequest request, ServerCallContext context)
    {
        var crew = await crewRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Crew {request.CtrlNbr} not found."));
        crew.Update(request.Name, request.IsActive);
        await crewRepository.UpdateAsync(crew);
        return MapCrew(crew);
    }

    public override async Task<DeleteResponse> DeleteCrew(DeleteCrewRequest request, ServerCallContext context)
    {
        await crewRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    public override async Task<GetCrewPositionsResponse> GetCrewPositions(GetCrewPositionsRequest request, ServerCallContext context)
    {
        var positions = await crewPositionRepository.GetByCrewAsync(ControlNumber.Create(request.CrewCtrlNbr));
        var response = new GetCrewPositionsResponse { TotalCount = positions.Count };
        foreach (var p in positions)
            response.Positions.Add(new CrewPositionResponse
            {
                CtrlNbr = p.CtrlNbr.Value,
                CrewCtrlNbr = p.CrewCtrlNbr.Value,
                PositionRoleCtrlNbr = p.PositionRoleCtrlNbr.Value,
                DisplayOrder = p.DisplayOrder
            });
        return response;
    }

    public override async Task<CrewPositionResponse> CreateCrewPosition(CreateCrewPositionRequest request, ServerCallContext context)
    {
        var position = CrewPosition.Create(request.CrewCtrlNbr, request.PositionRoleCtrlNbr, request.DisplayOrder);
        await crewPositionRepository.AddAsync(position);
        return new CrewPositionResponse
        {
            CtrlNbr = position.CtrlNbr.Value,
            CrewCtrlNbr = position.CrewCtrlNbr.Value,
            PositionRoleCtrlNbr = position.PositionRoleCtrlNbr.Value,
            DisplayOrder = position.DisplayOrder
        };
    }

    private static CrewResponse MapCrew(Crew c) => new()
    {
        CtrlNbr = c.CtrlNbr.Value,
        CrewType = c.CrewType,
        HomeGroupCtrlNbr = c.HomeGroupCtrlNbr.Value,
        Name = c.Name,
        IsActive = c.IsActive
    };

    // Incumbencies
    public override async Task<GetCrewIncumbenciesResponse> GetCrewIncumbencies(GetCrewIncumbenciesRequest request, ServerCallContext context)
    {
        var items = await incumbencyRepository.GetByCrewPositionAsync(ControlNumber.Create(request.CrewPositionCtrlNbr));
        var response = new GetCrewIncumbenciesResponse { TotalCount = items.Count };
        foreach (var i in items) response.Incumbencies.Add(MapIncumbency(i));
        return response;
    }

    public override async Task<CrewIncumbencyResponse> CreateCrewIncumbency(CreateCrewIncumbencyRequest request, ServerCallContext context)
    {
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        var incumbency = CrewIncumbency.Create(request.CrewPositionCtrlNbr, request.EmployeeCtrlNbr, startUtc, endUtc);
        await incumbencyRepository.AddAsync(incumbency);
        return MapIncumbency(incumbency);
    }

    private static CrewIncumbencyResponse MapIncumbency(CrewIncumbency i) => new()
    {
        CtrlNbr = i.CtrlNbr.Value,
        CrewPositionCtrlNbr = i.CrewPositionCtrlNbr.Value,
        EmployeeCtrlNbr = i.EmployeeCtrlNbr.Value,
        StartUtc = i.StartUtc.ToString("O"),
        EndUtc = i.EndUtc?.ToString("O") ?? string.Empty
    };

    // Crew Assignments
    public override async Task<GetCrewAssignmentsResponse> GetCrewAssignments(GetCrewAssignmentsRequest request, ServerCallContext context)
    {
        var items = await assignmentRepository.GetByCrewAsync(ControlNumber.Create(request.CrewCtrlNbr));
        var response = new GetCrewAssignmentsResponse { TotalCount = items.Count };
        foreach (var a in items) response.Assignments.Add(MapAssignment(a));
        return response;
    }

    public override async Task<CrewAssignmentResponse> CreateCrewAssignment(CreateCrewAssignmentRequest request, ServerCallContext context)
    {
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        var assignment = CrewAssignment.Create(request.CrewCtrlNbr, request.AssignmentGroupCtrlNbr, request.DaysOfWeekMask, startUtc, endUtc);
        await assignmentRepository.AddAsync(assignment);
        return MapAssignment(assignment);
    }

    public override async Task<CrewAssignmentResponse> UpdateCrewAssignment(UpdateCrewAssignmentRequest request, ServerCallContext context)
    {
        var assignment = await assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"CrewAssignment {request.CtrlNbr} not found."));
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        assignment.Update(request.DaysOfWeekMask, startUtc, endUtc);
        await assignmentRepository.UpdateAsync(assignment);
        return MapAssignment(assignment);
    }

    public override async Task<DeleteResponse> DeleteCrewAssignment(DeleteCrewAssignmentRequest request, ServerCallContext context)
    {
        await assignmentRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    private static CrewAssignmentResponse MapAssignment(CrewAssignment a) => new()
    {
        CtrlNbr = a.CtrlNbr.Value,
        CrewCtrlNbr = a.CrewCtrlNbr.Value,
        AssignmentGroupCtrlNbr = a.AssignmentGroupCtrlNbr.Value,
        DaysOfWeekMask = a.DaysOfWeekMask,
        StartUtc = a.StartUtc.ToString("O"),
        EndUtc = a.EndUtc?.ToString("O") ?? string.Empty
    };
}