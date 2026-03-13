using CrewService.Domain.Modules.Safety;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class SafetyService(
    ISafetyObservationRepository obsRepo,
    ISafetyObservationResolutionRepository resRepo) : SafetySrvc.SafetySrvcBase
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
}
