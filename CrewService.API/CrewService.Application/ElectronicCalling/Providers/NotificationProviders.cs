using CrewService.Domain.ValueObjects;

namespace CrewService.Application.ElectronicCalling.Providers;

public sealed class AtHocNotificationProvider : ICrewNotificationProvider
{
    public string ProviderType => "AtHoc";

    public Task<SendResult> SendAsync(
        ControlNumber employeeCtrlNbr,
        string templateType,
        IDictionary<string, string> templateData,
        CancellationToken ct = default)
    {
        // Placeholder — real implementation calls AtHoc REST API
        var externalId = $"ATHOC-{Guid.NewGuid():N}";
        return Task.FromResult(new SendResult(true, externalId));
    }

    public Task<PollResult> PollResponseAsync(string externalId, CancellationToken ct = default)
    {
        // Placeholder — real implementation polls AtHoc alert status
        return Task.FromResult(new PollResult(false, null));
    }
}

public sealed class MockNotificationProvider : ICrewNotificationProvider
{
    public string ProviderType => "Mock";

    public Task<SendResult> SendAsync(
        ControlNumber employeeCtrlNbr,
        string templateType,
        IDictionary<string, string> templateData,
        CancellationToken ct = default)
    {
        return Task.FromResult(new SendResult(true, $"MOCK-{Guid.NewGuid():N}"));
    }

    public Task<PollResult> PollResponseAsync(string externalId, CancellationToken ct = default)
    {
        return Task.FromResult(new PollResult(true, "Accept", "Mock"));
    }
}
