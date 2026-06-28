using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.VacancyCalls;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Notifications;

public class VacancyCallRequestTests
{
    [Fact]
    public void Create_DefaultsToSent()
    {
        var request = VacancyCallRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(100), "CrewCall");

        Assert.Equal("Sent", request.Status);
        Assert.Equal("CrewCall", request.TemplateType);
        Assert.Empty(request.Responses);
    }

    [Fact]
    public void RecordResponse_Accept_SetsAccepted()
    {
        var request = VacancyCallRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(100), "CrewCall");

        var response = request.RecordResponse("Accept", "Mobile");

        Assert.Equal("Accepted", request.Status);
        Assert.Single(request.Responses);
        Assert.Equal("Accept", response.ResponseType);
    }

    [Fact]
    public void RecordResponse_Reject_SetsRejected()
    {
        var request = VacancyCallRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(100), "CrewCall");

        request.RecordResponse("Reject");

        Assert.Equal("Rejected", request.Status);
    }

    [Fact]
    public void MarkExpired_SetsExpiredStatus()
    {
        var request = VacancyCallRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(100), "CrewCall");

        request.MarkExpired();

        Assert.Equal("Expired", request.Status);
    }

    [Fact]
    public void MarkFailed_SetsFailedStatus()
    {
        var request = VacancyCallRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(100), "CrewCall");

        request.MarkFailed();

        Assert.Equal("Failed", request.Status);
    }
}

public class NotificationProviderConfigTests
{
    [Fact]
    public void Create_SetsPropertiesWithDefaults()
    {
        var config = NotificationProviderConfig.Create(
            ControlNumber.Create(1), "SMS", "{\"apiKey\":\"x\"}");

        Assert.Equal("SMS", config.ProviderType);
        Assert.Equal(5, config.PollingIntervalSeconds);
        Assert.Equal(6, config.PollingTimeoutMinutes);
        Assert.Equal(15, config.BatchSize);
        Assert.Equal(60, config.BatchPauseSeconds);
    }

    [Fact]
    public void Create_WithCustomValues_OverridesDefaults()
    {
        var config = NotificationProviderConfig.Create(
            ControlNumber.Create(1), "IVR", "{}",
            pollingIntervalSeconds: 10, pollingTimeoutMinutes: 12,
            batchSize: 30, batchPauseSeconds: 120);

        Assert.Equal(10, config.PollingIntervalSeconds);
        Assert.Equal(12, config.PollingTimeoutMinutes);
        Assert.Equal(30, config.BatchSize);
    }
}
