using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class DispatchingService(
    IDispatchProjectionRepository projectionRepository,
    IDispatchDecisionLogRepository decisionLogRepository,
    IDispatchOverrideRepository overrideRepository) : DispatchingSrvc.DispatchingSrvcBase
{
    public override async Task<GetProjectionsResponse> GetProjections(GetProjectionsRequest request, ServerCallContext context)
    {
        var response = new GetProjectionsResponse();
        foreach (var slotCtrlNbr in request.PositionSlotCtrlNbrs)
        {
            var projections = await projectionRepository.GetByPositionSlotAsync(ControlNumber.Create(slotCtrlNbr));
            foreach (var p in projections)
            {
                response.Projections.Add(new ProjectionResponse
                {
                    CtrlNbr = p.CtrlNbr.Value,
                    PositionSlotCtrlNbr = p.PositionSlotCtrlNbr.Value,
                    ProjectedEmployeeCtrlNbr = p.ProjectedEmployeeCtrlNbr?.Value ?? 0,
                    TraceJson = p.TraceJson ?? string.Empty,
                    ComputedUtc = p.ComputedUtc.ToString("O")
                });
            }
        }
        return response;
    }

    public override async Task<GetDecisionLogsResponse> GetDecisionLogs(GetDecisionLogsRequest request, ServerCallContext context)
    {
        var logs = await decisionLogRepository.GetByPositionSlotAsync(ControlNumber.Create(request.PositionSlotCtrlNbr));
        var response = new GetDecisionLogsResponse();
        foreach (var l in logs)
        {
            response.Logs.Add(new DecisionLogResponse
            {
                CtrlNbr = l.CtrlNbr.Value,
                PositionSlotCtrlNbr = l.PositionSlotCtrlNbr.Value,
                AsOfUtc = l.AsOfUtc.ToString("O"),
                Phase = l.Phase,
                SelectedEmployeeCtrlNbr = l.SelectedEmployeeCtrlNbr?.Value ?? 0,
                SelectionSource = l.SelectionSource ?? string.Empty,
                DecisionJson = l.DecisionJson ?? string.Empty
            });
        }
        return response;
    }

    public override Task<ExecuteCallResponse> ExecuteCall(ExecuteCallRequest request, ServerCallContext context)
    {
        // Placeholder: full calling-time binding orchestration will be implemented in a later phase
        return Task.FromResult(new ExecuteCallResponse
        {
            Filled = false,
            UnfilledReason = "Calling-time binding not yet implemented"
        });
    }

    public override async Task<OverrideResponse> RequestOverride(RequestOverrideRequest request, ServerCallContext context)
    {
        var dispatch = DispatchOverride.Create(request.PositionSlotCtrlNbr, request.EmployeeCtrlNbr,
            request.OverrideType, request.ReasonCode, request.ReasonText);
        await overrideRepository.AddAsync(dispatch);
        return MapOverride(dispatch);
    }

    public override async Task<OverrideResponse> ApproveOverride(ApproveOverrideRequest request, ServerCallContext context)
    {
        var dispatch = await overrideRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Override {request.CtrlNbr} not found."));
        dispatch.Approve(request.ApprovedByCtrlNbr);
        await overrideRepository.UpdateAsync(dispatch);
        return MapOverride(dispatch);
    }

    private static OverrideResponse MapOverride(DispatchOverride o) => new()
    {
        CtrlNbr = o.CtrlNbr.Value,
        PositionSlotCtrlNbr = o.PositionSlotCtrlNbr.Value,
        EmployeeCtrlNbr = o.EmployeeCtrlNbr.Value,
        OverrideType = o.OverrideType,
        ReasonCode = o.ReasonCode,
        Status = o.Status
    };
}
