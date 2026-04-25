using CrewService.Application.Crews;
using CrewService.Application.Qualifications;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class CrewsService(
    EmployeeNameService employeeNameService,
    IServiceProvider serviceProvider) : CrewsSrvc.CrewsSrvcBase
{
    public override async Task<GetAllCrewsResponse> GetAllCrews(GetAllCrewsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        var crewType = !string.IsNullOrEmpty(request.CrewType) ? request.CrewType : null;
        var workAreaCtrlNbr = request.WorkAreaCtrlNbr > 0 ? ControlNumber.Create(request.WorkAreaCtrlNbr) : null;
        var railroadCtrlNbr = request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null;
        var (crews, positionCounts, daysMasks) = await svc.GetAllCrewsAsync(crewType, workAreaCtrlNbr, railroadCtrlNbr, context.CancellationToken);

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
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        try { return MapCrew(await svc.GetCrewAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken)); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<CrewResponse> CreateCrew(CreateCrewRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        var effectiveDate = !string.IsNullOrWhiteSpace(request.EffectiveDate) ? DateTime.Parse(request.EffectiveDate).ToUniversalTime() : (DateTime?)null;
        var abolishedDate = !string.IsNullOrWhiteSpace(request.AbolishedDate) ? DateTime.Parse(request.AbolishedDate).ToUniversalTime() : (DateTime?)null;
        try
        {
            var crew = await svc.CreateCrewAsync(request.CrewType, request.WorkAreaCtrlNbr, request.Name,
                request.IsActive, departmentCtrlNbr, effectiveDate, abolishedDate, context.CancellationToken);
            return MapCrew(crew);
        }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message)); }
    }

    public override async Task<CrewResponse> UpdateCrew(UpdateCrewRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        var effectiveDate = !string.IsNullOrWhiteSpace(request.EffectiveDate) ? DateTime.Parse(request.EffectiveDate).ToUniversalTime() : (DateTime?)null;
        var abolishedDate = !string.IsNullOrWhiteSpace(request.AbolishedDate) ? DateTime.Parse(request.AbolishedDate).ToUniversalTime() : (DateTime?)null;
        var crewType = !string.IsNullOrWhiteSpace(request.CrewType) ? request.CrewType : null;
        try
        {
            var crew = await svc.UpdateCrewAsync(ControlNumber.Create(request.CtrlNbr), request.Name,
                request.IsActive, departmentCtrlNbr, effectiveDate, abolishedDate, crewType, context.CancellationToken);
            return MapCrew(crew);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message)); }
    }

    public override async Task<DeleteResponse> DeleteCrew(DeleteCrewRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        try { await svc.DeleteCrewAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken); return new DeleteResponse { Success = true }; }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GetCrewPositionsResponse> GetCrewPositions(GetCrewPositionsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        var positions = await svc.GetCrewPositionsAsync(ControlNumber.Create(request.CrewCtrlNbr), context.CancellationToken);
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
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        var position = await svc.CreateCrewPositionAsync(request.CrewCtrlNbr, request.CraftRoleCtrlNbr, request.DisplayOrder, context.CancellationToken);
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
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        try { await svc.DeleteCrewPositionAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken); return new DeleteResponse { Success = true }; }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    private static CrewResponse MapCrew(Crew c) => new()
    {
        CtrlNbr = c.CtrlNbr.Value,
        CrewType = c.CrewType,
        WorkAreaCtrlNbr = c.WorkAreaCtrlNbr.Value,
        DepartmentCtrlNbr = c.DepartmentCtrlNbr?.Value ?? 0,
        Name = c.Name,
        IsActive = c.IsActive,
        EffectiveDate = c.EffectiveDate.ToString("O"),
        AbolishedDate = c.AbolishedDate?.ToString("O") ?? string.Empty
    };

    // Incumbencies
    public override async Task<GetCrewIncumbenciesResponse> GetCrewIncumbencies(GetCrewIncumbenciesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        var items = await svc.GetCrewIncumbenciesAsync(ControlNumber.Create(request.CrewPositionCtrlNbr), context.CancellationToken);
        var response = new GetCrewIncumbenciesResponse { TotalCount = items.Count };
        foreach (var i in items) response.Incumbencies.Add(await MapIncumbencyAsync(i));
        return response;
    }

    public override async Task<CrewIncumbencyResponse> CreateCrewIncumbency(CreateCrewIncumbencyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        var eligibilitySvc = serviceProvider.GetRequiredService<EmployeeEligibilityService>();
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();

        // Eligibility check stays in Presentation — it needs CraftRoleCtrlNbr from the position
        // which the Application service already fetches internally; for the check we need it here too.
        // We delegate the eligibility check to the Application-layer service.
        var eligibility = await eligibilitySvc.CheckEligibilityByCraftRoleForPositionAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            ControlNumber.Create(request.CrewPositionCtrlNbr),
            context.CancellationToken);
        if (!eligibility.IsEligible)
        {
            var reasons = string.Join("; ", eligibility.BlockingReasons.Select(r => r.Description));
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Employee is not qualified for this position: {reasons}"));
        }

        try
        {
            var incumbency = await svc.CreateCrewIncumbencyAsync(
                request.CrewPositionCtrlNbr, request.EmployeeCtrlNbr, startUtc, endUtc, context.CancellationToken);
            return await MapIncumbencyAsync(incumbency);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message)); }
    }

    public override async Task<DeleteResponse> EndCrewIncumbency(EndCrewIncumbencyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        var endUtc = string.IsNullOrEmpty(request.EndUtc) ? DateTime.UtcNow : DateTime.Parse(request.EndUtc).ToUniversalTime();
        try
        {
            await svc.EndCrewIncumbencyAsync(ControlNumber.Create(request.CtrlNbr), endUtc, context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { "Incumbency ended." } };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    private async Task<CrewIncumbencyResponse> MapIncumbencyAsync(CrewIncumbency i)
    {
        var employee = await employeeNameService.GetEmployeeInfoAsync(i.EmployeeCtrlNbr);
        return new CrewIncumbencyResponse
        {
            CtrlNbr = i.CtrlNbr.Value,
            CrewPositionCtrlNbr = i.CrewPositionCtrlNbr.Value,
            EmployeeCtrlNbr = i.EmployeeCtrlNbr.Value,
            StartUtc = i.StartUtc.ToString("O"),
            EndUtc = i.EndUtc?.ToString("O") ?? string.Empty,
            FullNameLnf = employee.FullNameLnf,
            EmployeeNumber = employee.EmployeeNumber
        };
    }

    // Crew Assignments
    public override async Task<GetCrewAssignmentsResponse> GetCrewAssignments(GetCrewAssignmentsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        var (items, crewNames) = await svc.GetCrewAssignmentsAsync(ControlNumber.Create(request.CrewCtrlNbr), context.CancellationToken);
        var response = new GetCrewAssignmentsResponse { TotalCount = items.Count };
        foreach (var a in items) response.Assignments.Add(MapAssignment(a, crewNames));
        return response;
    }

    public override async Task<GetCrewAssignmentsResponse> GetCrewAssignmentsByAssignment(GetCrewAssignmentsByAssignmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        var (items, crewNames) = await svc.GetCrewAssignmentsByAssignmentAsync(ControlNumber.Create(request.AssignmentCtrlNbr), context.CancellationToken);
        var response = new GetCrewAssignmentsResponse { TotalCount = items.Count };
        foreach (var a in items) response.Assignments.Add(MapAssignment(a, crewNames));
        return response;
    }

    public override async Task<CrewAssignmentResponse> CreateCrewAssignment(CreateCrewAssignmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        var assignment = await svc.CreateCrewAssignmentAsync(request.CrewCtrlNbr, request.AssignmentCtrlNbr, request.DaysOfWeekMask, startUtc, endUtc, context.CancellationToken);
        return MapAssignment(assignment);
    }

    public override async Task<CrewAssignmentResponse> UpdateCrewAssignment(UpdateCrewAssignmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        try
        {
            var assignment = await svc.UpdateCrewAssignmentAsync(ControlNumber.Create(request.CtrlNbr), request.DaysOfWeekMask, startUtc, endUtc, context.CancellationToken);
            return MapAssignment(assignment);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<DeleteResponse> DeleteCrewAssignment(DeleteCrewAssignmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();
        try { await svc.DeleteCrewAssignmentAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken); return new DeleteResponse { Success = true }; }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
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

    // ── Crew Setup Wizard ──

    public override async Task<CrewSetupWizardResponse> CrewSetupWizard(CrewSetupWizardRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CrewsAppService>();

        var positions = request.Positions
            .Select(p => new CrewsAppService.WizardPositionEntry(p.CraftRoleCtrlNbr, p.DisplayOrder))
            .ToList();
        var assignments = request.Assignments
            .Select(e => new CrewsAppService.WizardAssignmentEntry(
                e.ExistingAssignmentCtrlNbr, e.GroupCtrlNbr, e.DepartmentCtrlNbr,
                e.Code, e.Name, e.IsExtra,
                e.ShiftDefinitionCtrlNbr, e.OnDutyTime, e.OffDutyTime,
                e.AssignmentOperatingDaysMask, e.CrewWorkDaysMask,
                e.StartDate, e.EndDate))
            .ToList();

        try
        {
            var result = await svc.CrewSetupWizardAsync(
                request.ExistingCrewCtrlNbr, request.WorkAreaCtrlNbr,
                request.CrewName, request.CrewType, request.CrewDepartmentCtrlNbr,
                request.EffectiveDate, request.AbolishedDate,
                positions, assignments, context.CancellationToken);

            return new CrewSetupWizardResponse
            {
                CrewCtrlNbr = result.CrewCtrlNbr,
                CrewName = result.CrewName,
                AssignmentsCreated = result.AssignmentsCreated,
                AssignmentsUpdated = result.AssignmentsUpdated,
                SchedulesCreated = result.SchedulesCreated,
                SchedulesUpdated = result.SchedulesUpdated,
                SchedulesExisting = result.SchedulesExisting,
                CrewAssignmentsCreated = result.CrewAssignmentsCreated,
                CrewAssignmentsUpdated = result.CrewAssignmentsUpdated,
                CrewAssignmentsDeleted = result.CrewAssignmentsDeleted,
                CrewAssignmentsExisting = result.CrewAssignmentsExisting,
                PositionsCreated = result.PositionsCreated,
                PositionsDeleted = result.PositionsDeleted,
                PositionsExisting = result.PositionsExisting,
                IsExistingCrew = result.IsExistingCrew
            };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            { throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message)); }
        catch (InvalidOperationException ex)
            { throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message)); }
    }
}
