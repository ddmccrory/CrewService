using CrewService.Application.Safety;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class SafetyService(IServiceProvider serviceProvider) : SafetySrvc.SafetySrvcBase
{
    public override async Task<SafetyObservationResponse> CreateObservation(CreateObservationRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Safety.SafetyService>();
        var obs = await svc.CreateObservationAsync(
            request.WorkAreaGroupCtrlNbr, request.ObserverEmployeeCtrlNbr,
            request.CategoryCode, request.AreaCode, request.Description,
            string.IsNullOrEmpty(request.SubdivisionCode) ? null : request.SubdivisionCode);
        return MapObservation(obs);
    }

    public override async Task<SafetyObservationResponse> GetObservation(GetObservationRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Safety.SafetyService>();
        try
        {
            var obs = await svc.GetObservationAsync(ControlNumber.Create(request.CtrlNbr));
            return MapObservation(obs);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GetObservationsResponse> GetByWorkArea(GetSafetyByWorkAreaRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Safety.SafetyService>();
        var items = await svc.GetByWorkAreaAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), request.OpenOnly, context.CancellationToken);
        var response = new GetObservationsResponse { TotalCount = items.Count };
        foreach (var item in items) response.Items.Add(MapObservation(item));
        return response;
    }

    public override async Task<SafetyActionResponse> AddAction(AddActionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Safety.SafetyService>();
        try
        {
            var action = await svc.AddActionAsync(
                ControlNumber.Create(request.ObservationCtrlNbr),
                ControlNumber.Create(request.TakenByCtrlNbr),
                request.ActionDescription);
            return MapAction(action);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<SafetyResolutionResponse> ResolveObservation(ResolveObservationRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Safety.SafetyService>();
        try
        {
            var resolution = await svc.ResolveObservationAsync(
                ControlNumber.Create(request.ObservationCtrlNbr),
                ControlNumber.Create(request.ResolvedByCtrlNbr),
                request.ResolutionDescription,
                context.CancellationToken);
            return MapResolution(resolution);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GetSafetyCategoriesResponse> GetCategories(GetSafetyRefDataRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Safety.SafetyService>();
        var items = await svc.GetCategoriesAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), context.CancellationToken);
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
        var svc = serviceProvider.GetRequiredService<Application.Safety.SafetyService>();
        var cat = await svc.CreateCategoryAsync(
            request.WorkAreaGroupCtrlNbr, request.Code, request.DisplayName, context.CancellationToken);
        return new SafetyCategoryResponse
        {
            CtrlNbr = cat.CtrlNbr.Value, WorkAreaGroupCtrlNbr = cat.WorkAreaGroupCtrlNbr.Value,
            Code = cat.Code, DisplayName = cat.DisplayName, IsActive = cat.IsActive
        };
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
}
