using CrewService.Application.Dispatching;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class DispatchingService(IServiceProvider serviceProvider) : DispatchingSrvc.DispatchingSrvcBase
{
    public override async Task<GetProjectionsResponse> GetProjections(GetProjectionsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Dispatching.DispatchingService>();
        var projections = await svc.GetProjectionsAsync(
            request.PositionSlotCtrlNbrs.Select(ControlNumber.Create), context.CancellationToken);

        var response = new GetProjectionsResponse();
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
        return response;
    }

    public override async Task<GetDecisionLogsResponse> GetDecisionLogs(GetDecisionLogsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Dispatching.DispatchingService>();
        var logs = await svc.GetDecisionLogsAsync(
            ControlNumber.Create(request.PositionSlotCtrlNbr), context.CancellationToken);

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
        var svc = serviceProvider.GetRequiredService<Application.Dispatching.DispatchingService>();
        var dispatch = await svc.RequestOverrideAsync(
            request.PositionSlotCtrlNbr, request.EmployeeCtrlNbr,
            request.OverrideType, request.ReasonCode, request.ReasonText,
            context.CancellationToken);
        return MapOverride(dispatch);
    }

    public override async Task<OverrideResponse> ApproveOverride(ApproveOverrideRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Dispatching.DispatchingService>();
        try
        {
            var dispatch = await svc.ApproveOverrideAsync(
                ControlNumber.Create(request.CtrlNbr), request.ApprovedByCtrlNbr, context.CancellationToken);
            return MapOverride(dispatch);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
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

    public override async Task<GetEmployeeBookingsResponse> GetEmployeeBookings(GetEmployeeBookingsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Dispatching.DispatchingService>();
        var bookings = await svc.GetEmployeeBookingsAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);

        var response = new GetEmployeeBookingsResponse { TotalCount = bookings.Count };
        foreach (var b in bookings) response.Bookings.Add(MapBooking(b));
        return response;
    }

    public override async Task<EmployeeBookingResponse> CreateEmployeeBooking(CreateEmployeeBookingRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Dispatching.DispatchingService>();
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        var endUtc = DateTime.Parse(request.EndUtc).ToUniversalTime();
        ControlNumber? slotCtrl = request.PositionSlotCtrlNbr > 0 ? ControlNumber.Create(request.PositionSlotCtrlNbr) : null;

        var booking = await svc.CreateEmployeeBookingAsync(
            request.EmployeeCtrlNbr, startUtc, endUtc, slotCtrl, context.CancellationToken);
        return MapBooking(booking);
    }

    private static EmployeeBookingResponse MapBooking(EmployeeBooking b) => new()
    {
        CtrlNbr = b.CtrlNbr.Value,
        EmployeeCtrlNbr = b.EmployeeCtrlNbr.Value,
        StartUtc = b.StartUtc.ToString("O"),
        EndUtc = b.EndUtc.ToString("O"),
        PositionSlotCtrlNbr = b.PositionSlotCtrlNbr?.Value ?? 0
    };
}
