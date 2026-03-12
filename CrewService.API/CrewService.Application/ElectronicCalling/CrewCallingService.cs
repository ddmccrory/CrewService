using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.ElectronicCalling;

public interface INotificationRequestRepository
{
    Task AddAsync(NotificationRequest request, CancellationToken ct = default);
    Task<NotificationRequest?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationRequest>> GetPendingAsync(CancellationToken ct = default);
}

public sealed class CrewCallingService(
    ICrewNotificationProvider provider,
    INotificationRequestRepository requestRepo)
{
    public async Task<NotificationRequest> SendCallAsync(
        ControlNumber positionSlotCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string templateType,
        IDictionary<string, string> templateData,
        int pollingTimeoutMinutes = 6,
        CancellationToken ct = default)
    {
        var sendResult = await provider.SendAsync(employeeCtrlNbr, templateType, templateData, ct);

        var request = NotificationRequest.Create(
            positionSlotCtrlNbr, employeeCtrlNbr, templateType,
            pollingTimeoutMinutes,
            sendResult.Success ? sendResult.ExternalId : null);

        if (!sendResult.Success)
            request.MarkFailed();

        await requestRepo.AddAsync(request, ct);
        return request;
    }

    public async Task<NotificationRequest?> PollAndUpdateAsync(
        ControlNumber requestCtrlNbr, CancellationToken ct = default)
    {
        var request = await requestRepo.GetByCtrlNbrAsync(requestCtrlNbr, ct);
        if (request is null || request.Status != "Sent") return request;

        if (request.IsExpired())
        {
            request.MarkExpired();
            return request;
        }

        if (request.ExternalId is null) return request;

        var pollResult = await provider.PollResponseAsync(request.ExternalId, ct);
        if (pollResult.HasResponse && pollResult.ResponseType is not null)
        {
            request.RecordResponse(pollResult.ResponseType, pollResult.DeviceType);
        }

        return request;
    }
}
