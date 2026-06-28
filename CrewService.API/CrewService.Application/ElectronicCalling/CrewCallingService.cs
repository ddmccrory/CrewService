using CrewService.Domain.Modules.VacancyCalls;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.ElectronicCalling;

public interface IVacancyCallRequestRepository
{
    Task AddAsync(VacancyCallRequest request, CancellationToken ct = default);
    Task<VacancyCallRequest?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task<IReadOnlyList<VacancyCallRequest>> GetPendingAsync(CancellationToken ct = default);
}

public sealed class CrewCallingService(
    ICrewNotificationProvider provider,
    IVacancyCallRequestRepository requestRepo)
{
    public async Task<VacancyCallRequest> SendCallAsync(
        ControlNumber positionSlotCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string templateType,
        IDictionary<string, string> templateData,
        int pollingTimeoutMinutes = 6,
        CancellationToken ct = default)
    {
        var sendResult = await provider.SendAsync(employeeCtrlNbr, templateType, templateData, ct);

        var request = VacancyCallRequest.Create(
            positionSlotCtrlNbr, employeeCtrlNbr, templateType,
            pollingTimeoutMinutes,
            sendResult.Success ? sendResult.ExternalId : null);

        if (!sendResult.Success)
            request.MarkFailed();

        await requestRepo.AddAsync(request, ct);
        return request;
    }

    public async Task<VacancyCallRequest?> PollAndUpdateAsync(
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
