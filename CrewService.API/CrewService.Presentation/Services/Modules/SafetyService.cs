using CrewService.Domain.Modules.Safety;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class SafetyService(
    ISafetyObservationRepository obsRepo,
    ISafetyObservationResolutionRepository resRepo,
    ISafetyCategoryRepository catRepo,
    ISafetyAreaRepository areaRepo,
    ISafetySubdivisionRepository subdivRepo) : SafetySrvc.SafetySrvcBase
{
    public override async Task<SafetyObservationResponse> CreateObservation(CreateObservationRequest request, ServerCallContext context)
    {
        var obs = SafetyObservation.Create(
            request.WorkAreaGroupCtrlNbr, request.ObserverEmployeeCtrlNbr,
            request.CategoryCode, request.AreaCode, request.Description,
            string.IsNullOrEmpty(request.SubdivisionCode) ? null : request.SubdivisionCode);
        await obsRepo.AddAsync(obs);
        return MapObservation(obs);
    }

    public override async Task<SafetyObservationResponse> GetObservation(GetObservationRequest request, ServerCallContext context)
    {
        var obs = await obsRepo.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Safety observation {request.CtrlNbr} not found."));
        return MapObservation(obs);
    }

    public override async Task<GetObservationsResponse> GetByWorkArea(GetSafetyByWorkAreaRequest request, ServerCallContext context)
    {
        var workArea = ControlNumber.Create(request.WorkAreaGroupCtrlNbr);
        var items = request.OpenOnly
            ? await obsRepo.GetOpenByWorkAreaAsync(workArea, context.CancellationToken)
            : await obsRepo.GetByWorkAreaAsync(workArea, context.CancellationToken);

        var response = new GetObservationsResponse { TotalCount = items.Count };
        foreach (var item in items) response.Items.Add(MapObservation(item));
        return response;
    }

    public override async Task<SafetyActionResponse> AddAction(AddActionRequest request, ServerCallContext context)
    {
        var obs = await obsRepo.GetByCtrlNbrAsync(ControlNumber.Create(request.ObservationCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Safety observation {request.ObservationCtrlNbr} not found."));

        var action = obs.AddAction(ControlNumber.Create(request.TakenByCtrlNbr), request.ActionDescription);
        await obsRepo.UpdateAsync(obs);
        return MapAction(action);
    }

    public override async Task<SafetyResolutionResponse> ResolveObservation(ResolveObservationRequest request, ServerCallContext context)
    {
        var obs = await obsRepo.GetByCtrlNbrAsync(ControlNumber.Create(request.ObservationCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Safety observation {request.ObservationCtrlNbr} not found."));

        var resolution = obs.Resolve(ControlNumber.Create(request.ResolvedByCtrlNbr), request.ResolutionDescription);
        await resRepo.AddAsync(resolution, context.CancellationToken);
        await obsRepo.UpdateAsync(obs);

        return MapResolution(resolution);
    }

    private static SafetyObservationResponse MapObservation(SafetyObservation obs)
    {
        var response = new SafetyObservationResponse
        {
            CtrlNbr = obs.CtrlNbr.Value,
            WorkAreaGroupCtrlNbr = obs.WorkAreaGroupCtrlNbr.Value,
            ObserverEmployeeCtrlNbr = obs.ObserverEmployeeCtrlNbr.Value,
            CategoryCode = obs.CategoryCode,
            AreaCode = obs.AreaCode,
            SubdivisionCode = obs.SubdivisionCode ?? string.Empty,
            Description = obs.Description,
            ObservedAtUtc = obs.ObservedAtUtc.ToString("O"),
            Status = obs.Status
        };
        foreach (var a in obs.Actions) response.Actions.Add(MapAction(a));
        return response;
    }

    private static SafetyActionResponse MapAction(SafetyObservationAction a) => new()
    {
        CtrlNbr = a.CtrlNbr.Value,
        ObservationCtrlNbr = a.ObservationCtrlNbr.Value,
        TakenByCtrlNbr = a.TakenByCtrlNbr.Value,
        ActionDescription = a.ActionDescription,
        TakenAtUtc = a.TakenAtUtc.ToString("O")
    };

    private static SafetyResolutionResponse MapResolution(SafetyObservationResolution r) => new()
    {
        CtrlNbr = r.CtrlNbr.Value,
        ObservationCtrlNbr = r.ObservationCtrlNbr.Value,
        ResolvedByCtrlNbr = r.ResolvedByCtrlNbr.Value,
        ResolutionDescription = r.ResolutionDescription,
        ResolvedAtUtc = r.ResolvedAtUtc.ToString("O")
    };

    // Reference Data endpoints
    public override async Task<GetSafetyCategoriesResponse> GetCategories(GetSafetyRefDataRequest request, ServerCallContext context)
    {
        var items = await catRepo.GetByWorkAreaAsync(ControlNumber.Create(request.WorkAreaGroupCtrlNbr), context.CancellationToken);
        var response = new GetSafetyCategoriesResponse { TotalCount = items.Count };
        foreach (var c in items) response.Items.Add(new SafetyCategoryResponse
        {
            CtrlNbr = c.CtrlNbr.Value, WorkAreaGroupCtrlNbr = c.WorkAreaGroupCtrlNbr.Value,
            Code = c.Code, DisplayName = c.DisplayName, IsActive = c.IsActive
        });
        return response;
    }

    public override async Task<SafetyCategoryResponse> CreateCategory(CreateSafetyCategoryRequest request, ServerCallContext context)
    {
        var cat = SafetyCategory.Create(request.WorkAreaGroupCtrlNbr, request.Code, request.DisplayName);
        await catRepo.AddAsync(cat, context.CancellationToken);
        return new SafetyCategoryResponse
        {
            CtrlNbr = cat.CtrlNbr.Value, WorkAreaGroupCtrlNbr = cat.WorkAreaGroupCtrlNbr.Value,
            Code = cat.Code, DisplayName = cat.DisplayName, IsActive = cat.IsActive
        };
    }

    public override async Task<GetSafetyAreasResponse> GetAreas(GetSafetyRefDataRequest request, ServerCallContext context)
    {
        var items = await areaRepo.GetByWorkAreaAsync(ControlNumber.Create(request.WorkAreaGroupCtrlNbr), context.CancellationToken);
        var response = new GetSafetyAreasResponse { TotalCount = items.Count };
        foreach (var a in items) response.Items.Add(new SafetyAreaResponse
        {
            CtrlNbr = a.CtrlNbr.Value, WorkAreaGroupCtrlNbr = a.WorkAreaGroupCtrlNbr.Value,
            Code = a.Code, DisplayName = a.DisplayName, IsActive = a.IsActive
        });
        return response;
    }

    public override async Task<SafetyAreaResponse> CreateArea(CreateSafetyAreaRequest request, ServerCallContext context)
    {
        var area = SafetyArea.Create(request.WorkAreaGroupCtrlNbr, request.Code, request.DisplayName);
        await areaRepo.AddAsync(area, context.CancellationToken);
        return new SafetyAreaResponse
        {
            CtrlNbr = area.CtrlNbr.Value, WorkAreaGroupCtrlNbr = area.WorkAreaGroupCtrlNbr.Value,
            Code = area.Code, DisplayName = area.DisplayName, IsActive = area.IsActive
        };
    }

    public override async Task<GetSafetySubdivisionsResponse> GetSubdivisions(GetSafetyRefDataRequest request, ServerCallContext context)
    {
        var items = await subdivRepo.GetByWorkAreaAsync(ControlNumber.Create(request.WorkAreaGroupCtrlNbr), context.CancellationToken);
        var response = new GetSafetySubdivisionsResponse { TotalCount = items.Count };
        foreach (var s in items) response.Items.Add(new SafetySubdivisionResponse
        {
            CtrlNbr = s.CtrlNbr.Value, WorkAreaGroupCtrlNbr = s.WorkAreaGroupCtrlNbr.Value,
            Code = s.Code, DisplayName = s.DisplayName, IsActive = s.IsActive
        });
        return response;
    }

    public override async Task<SafetySubdivisionResponse> CreateSubdivision(CreateSafetySubdivisionRequest request, ServerCallContext context)
    {
        var subdiv = SafetySubdivision.Create(request.WorkAreaGroupCtrlNbr, request.Code, request.DisplayName);
        await subdivRepo.AddAsync(subdiv, context.CancellationToken);
        return new SafetySubdivisionResponse
        {
            CtrlNbr = subdiv.CtrlNbr.Value, WorkAreaGroupCtrlNbr = subdiv.WorkAreaGroupCtrlNbr.Value,
            Code = subdiv.Code, DisplayName = subdiv.DisplayName, IsActive = subdiv.IsActive
        };
    }
}
