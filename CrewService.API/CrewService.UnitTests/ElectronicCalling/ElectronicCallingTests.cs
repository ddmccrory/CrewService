using CrewService.Application.ElectronicCalling;
using CrewService.Application.ElectronicCalling.Providers;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.ElectronicCalling;

public class NotificationRequestTests
{
    [Fact]
    public void Create_SetsSentStatus()
    {
        var req = NotificationRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(2), "AssignmentCall", 6, "EXT-1");
        Assert.Equal("Sent", req.Status);
        Assert.Equal("EXT-1", req.ExternalId);
    }

    [Fact]
    public void RecordResponse_Accept_SetsAccepted()
    {
        var req = NotificationRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(2), "AssignmentCall");
        req.RecordResponse("Accept", "Phone");

        Assert.Equal("Accepted", req.Status);
        Assert.Single(req.Responses);
        Assert.Equal("Accept", req.Responses[0].ResponseType);
    }

    [Fact]
    public void RecordResponse_Reject_SetsRejected()
    {
        var req = NotificationRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(2), "AssignmentCall");
        req.RecordResponse("Reject");

        Assert.Equal("Rejected", req.Status);
    }

    [Fact]
    public void MarkExpired_SetsExpiredStatus()
    {
        var req = NotificationRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(2), "AssignmentCall");
        req.MarkExpired();
        Assert.Equal("Expired", req.Status);
    }

    [Fact]
    public void MarkFailed_SetsFailedStatus()
    {
        var req = NotificationRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(2), "AssignmentCall");
        req.MarkFailed();
        Assert.Equal("Failed", req.Status);
    }
}

public class NotificationProviderTests
{
    [Fact]
    public async Task AtHoc_Send_ReturnsExternalId()
    {
        var provider = new AtHocNotificationProvider();
        var result = await provider.SendAsync(
            ControlNumber.Create(1), "AssignmentCall", new Dictionary<string, string>());
        Assert.True(result.Success);
        Assert.StartsWith("ATHOC-", result.ExternalId);
    }

    [Fact]
    public async Task Mock_Send_ReturnsExternalId()
    {
        var provider = new MockNotificationProvider();
        var result = await provider.SendAsync(
            ControlNumber.Create(1), "AssignmentCall", new Dictionary<string, string>());
        Assert.True(result.Success);
        Assert.StartsWith("MOCK-", result.ExternalId);
    }

    [Fact]
    public async Task Mock_Poll_ReturnsAccept()
    {
        var provider = new MockNotificationProvider();
        var result = await provider.PollResponseAsync("MOCK-123");
        Assert.True(result.HasResponse);
        Assert.Equal("Accept", result.ResponseType);
    }
}

public class NotificationProviderConfigTests
{
    [Fact]
    public void Create_SetsDefaults()
    {
        var config = NotificationProviderConfig.Create(
            ControlNumber.Create(1), "AtHoc", "{}");
        Assert.Equal(5, config.PollingIntervalSeconds);
        Assert.Equal(6, config.PollingTimeoutMinutes);
        Assert.Equal(15, config.BatchSize);
        Assert.Equal(60, config.BatchPauseSeconds);
    }
}
