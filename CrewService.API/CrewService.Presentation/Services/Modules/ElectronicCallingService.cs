using CrewService.Application.ElectronicCalling;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class ElectronicCallingService(CrewCallingService callingService)
    : ElectronicCallingSrvc.ElectronicCallingSrvcBase
{
    public override async Task<NotificationRequestResponse> SendCrewCall(
        SendCrewCallRequest request, ServerCallContext context)
    {
        var result = await callingService.SendCallAsync(
            ControlNumber.Create(request.PositionSlotCtrlNbr),
            ControlNumber.Create(request.EmployeeCtrlNbr),
            request.TemplateType,
            new Dictionary<string, string>(),
            request.PollingTimeoutMinutes > 0 ? request.PollingTimeoutMinutes : 6,
            context.CancellationToken);

        return MapResponse(result);
    }

    public override async Task<NotificationRequestResponse> PollCallStatus(
        PollCallStatusRequest request, ServerCallContext context)
    {
        var result = await callingService.PollAndUpdateAsync(
            ControlNumber.Create(request.RequestCtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Notification request not found"));

        return MapResponse(result);
    }

    private static NotificationRequestResponse MapResponse(Domain.Modules.Notifications.NotificationRequest req)
    {
        var resp = new NotificationRequestResponse
        {
            CtrlNbr = req.CtrlNbr.Value,
            Status = req.Status,
            TemplateType = req.TemplateType,
            SentAt = Timestamp.FromDateTime(DateTime.SpecifyKind(req.SentAtUtc, DateTimeKind.Utc)),
            ExpiresAt = Timestamp.FromDateTime(DateTime.SpecifyKind(req.ExpiresAtUtc, DateTimeKind.Utc)),
        };
        if (req.ExternalId is not null) resp.ExternalId = req.ExternalId;
        return resp;
    }
}
