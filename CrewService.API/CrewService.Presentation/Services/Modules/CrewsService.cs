using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class CrewsService(
    ICrewRepository crewRepository,
    ICrewPositionRepository crewPositionRepository,
    ICrewIncumbencyRepository incumbencyRepository,
    ICrewAssignmentRepository assignmentRepository,
    IOrchestrationUnitOfWorkFactory uowFactory) : CrewsSrvc.CrewsSrvcBase
{
    public override async Task<GetAllCrewsResponse> GetAllCrews(GetAllCrewsRequest request, ServerCallContext context)
    {
        List<Domain.Modules.Crews.Crew> crews;
        if (!string.IsNullOrEmpty(request.CrewType))
            crews = await crewRepository.GetByTypeAsync(request.CrewType);
        else if (request.HomeGroupCtrlNbr > 0)
            crews = await crewRepository.GetByHomeGroupAsync(ControlNumber.Create(request.HomeGroupCtrlNbr));
        else if (request.RailroadCtrlNbr > 0)
            crews = await crewRepository.GetByRailroadAsync(ControlNumber.Create(request.RailroadCtrlNbr));
        else
            crews = await crewRepository.GetAllAsync();
        var crewIds = crews.Select(c => c.CtrlNbr).ToList();
        var allPositions = await crewPositionRepository.GetByCrewsAsync(crewIds);
        var allAssignments = await assignmentRepository.GetByCrewsAsync(crewIds);
        var positionCounts = allPositions.GroupBy(p => p.CrewCtrlNbr).ToDictionary(g => g.Key, g => g.Count());
        var daysMasks = allAssignments.GroupBy(a => a.CrewCtrlNbr).ToDictionary(g => g.Key, g => g.Aggregate(0, (mask, a) => mask | a.DaysOfWeekMask));

        var response = new GetAllCrewsResponse { TotalCount = crews.Count };
        foreach (var c in crews)
        {
            var mapped = MapCrew(c);
            positionCounts.TryGetValue(c.CtrlNbr, out var posCount);
            daysMasks.TryGetValue(c.CtrlNbr, out var daysMask);
            mapped.PositionCount = posCount;
            mapped.WorkDaysMask = daysMask;
            response.Crews.Add(mapped);
        }
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
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        var crew = Crew.Create(request.CrewType, request.HomeGroupCtrlNbr, request.Name, request.IsActive, departmentCtrlNbr);

        await using var uow = await uowFactory.CreateAsync();
        uow.Crews.Add(crew);
        await uow.CommitAsync();

        return MapCrew(crew);
    }

    public override async Task<CrewResponse> UpdateCrew(UpdateCrewRequest request, ServerCallContext context)
    {
        var crew = await crewRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Crew {request.CtrlNbr} not found."));
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        crew.Update(request.Name, request.IsActive, departmentCtrlNbr);

        await using var uow = await uowFactory.CreateAsync();
        uow.Crews.Update(crew);
        await uow.CommitAsync();

        return MapCrew(crew);
    }

    public override async Task<DeleteResponse> DeleteCrew(DeleteCrewRequest request, ServerCallContext context)
    {
        var crew = await crewRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Crew {request.CtrlNbr} not found."));

        await using var uow = await uowFactory.CreateAsync();
        uow.Crews.Remove(crew);
        await uow.CommitAsync();

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
                CraftRoleCtrlNbr = p.CraftRoleCtrlNbr.Value,
                DisplayOrder = p.DisplayOrder
            });
        return response;
    }

    public override async Task<CrewPositionResponse> CreateCrewPosition(CreateCrewPositionRequest request, ServerCallContext context)
    {
        var staffablePosition = StaffablePosition.Create("Crew");
        var position = CrewPosition.Create(request.CrewCtrlNbr, request.CraftRoleCtrlNbr, request.DisplayOrder, staffablePosition.CtrlNbr);

        await using var uow = await uowFactory.CreateAsync();
        uow.StaffablePositions.Add(staffablePosition);
        uow.CrewPositions.Add(position);
        await uow.CommitAsync();

        return new CrewPositionResponse
        {
            CtrlNbr = position.CtrlNbr.Value,
            CrewCtrlNbr = position.CrewCtrlNbr.Value,
            CraftRoleCtrlNbr = position.CraftRoleCtrlNbr.Value,
            DisplayOrder = position.DisplayOrder
        };
    }

    public override async Task<DeleteResponse> DeleteCrewPosition(DeleteCrewPositionRequest request, ServerCallContext context)
    {
        var position = await crewPositionRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"CrewPosition {request.CtrlNbr} not found."));

        await using var uow = await uowFactory.CreateAsync();
        uow.CrewPositions.Remove(position);
        await uow.CommitAsync();

        return new DeleteResponse { Success = true };
    }

    private static CrewResponse MapCrew(Crew c) => new()
    {
        CtrlNbr = c.CtrlNbr.Value,
        CrewType = c.CrewType,
        HomeGroupCtrlNbr = c.HomeGroupCtrlNbr.Value,
        DepartmentCtrlNbr = c.DepartmentCtrlNbr?.Value ?? 0,
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

        await using var uow = await uowFactory.CreateAsync();
        uow.CrewIncumbencies.Add(incumbency);
        await uow.CommitAsync();

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
        return await BuildCrewAssignmentsResponse(items);
    }

    public override async Task<GetCrewAssignmentsResponse> GetCrewAssignmentsByAssignment(GetCrewAssignmentsByAssignmentRequest request, ServerCallContext context)
    {
        var items = await assignmentRepository.GetByAssignmentAsync(ControlNumber.Create(request.AssignmentCtrlNbr));
        return await BuildCrewAssignmentsResponse(items);
    }

    private async Task<GetCrewAssignmentsResponse> BuildCrewAssignmentsResponse(List<CrewAssignment> items)
    {
        var crewCtrlNbrs = items.Select(a => a.CrewCtrlNbr).Distinct().ToList();
        var crewNames = new Dictionary<long, string>();
        foreach (var ctrlNbr in crewCtrlNbrs)
        {
            var crew = await crewRepository.GetByCtrlNbrAsync(ctrlNbr);
            if (crew is not null) crewNames[ctrlNbr.Value] = crew.Name;
        }
        var response = new GetCrewAssignmentsResponse { TotalCount = items.Count };
        foreach (var a in items) response.Assignments.Add(MapAssignment(a, crewNames));
        return response;
    }

    public override async Task<CrewAssignmentResponse> CreateCrewAssignment(CreateCrewAssignmentRequest request, ServerCallContext context)
    {
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        var assignment = CrewAssignment.Create(request.CrewCtrlNbr, request.AssignmentCtrlNbr, request.DaysOfWeekMask, startUtc, endUtc);

        await using var uow = await uowFactory.CreateAsync();
        uow.CrewAssignments.Add(assignment);
        await uow.CommitAsync();

        return MapAssignment(assignment);
    }

    public override async Task<CrewAssignmentResponse> UpdateCrewAssignment(UpdateCrewAssignmentRequest request, ServerCallContext context)
    {
        var assignment = await assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"CrewAssignment {request.CtrlNbr} not found."));
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        assignment.Update(request.DaysOfWeekMask, startUtc, endUtc);

        await using var uow = await uowFactory.CreateAsync();
        uow.CrewAssignments.Update(assignment);
        await uow.CommitAsync();

        return MapAssignment(assignment);
    }

    public override async Task<DeleteResponse> DeleteCrewAssignment(DeleteCrewAssignmentRequest request, ServerCallContext context)
    {
        var assignment = await assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"CrewAssignment {request.CtrlNbr} not found."));

        await using var uow = await uowFactory.CreateAsync();
        uow.CrewAssignments.Remove(assignment);
        await uow.CommitAsync();

        return new DeleteResponse { Success = true };
    }

    private static CrewAssignmentResponse MapAssignment(CrewAssignment a, Dictionary<long, string>? crewNames = null) => new()
    {
        CtrlNbr = a.CtrlNbr.Value,
        CrewCtrlNbr = a.CrewCtrlNbr.Value,
        AssignmentCtrlNbr = a.AssignmentCtrlNbr.Value,
        DaysOfWeekMask = a.DaysOfWeekMask,
        StartUtc = a.StartUtc.ToString("O"),
        EndUtc = a.EndUtc?.ToString("O") ?? string.Empty,
        CrewName = crewNames is not null && crewNames.TryGetValue(a.CrewCtrlNbr.Value, out var name) ? name : string.Empty
    };
}