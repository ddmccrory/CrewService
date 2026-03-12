using CrewService.Domain.ValueObjects;

namespace CrewService.Application.ElectronicCalling;

public interface ICrewNotificationProvider
{
    string ProviderType { get; }

    Task<SendResult> SendAsync(
        ControlNumber employeeCtrlNbr,
        string templateType,
        IDictionary<string, string> templateData,
        CancellationToken ct = default);

    Task<PollResult> PollResponseAsync(
        string externalId, CancellationToken ct = default);
}

public sealed record SendResult(
    bool Success,
    string? ExternalId,
    string? ErrorMessage = null);

public sealed record PollResult(
    bool HasResponse,
    string? ResponseType,
    string? DeviceType = null);
