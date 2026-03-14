using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Notifications;

public class NotificationRequestTests
{
    [Fact]
    public void Create_DefaultsToSent()
    {
        var request = NotificationRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(100), "CrewCall");

        Assert.Equal("Sent", request.Status);
        Assert.Equal("CrewCall", request.TemplateType);
        Assert.Empty(request.Responses);
    }

    [Fact]
    public void RecordResponse_Accept_SetsAccepted()
    {
        var request = NotificationRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(100), "CrewCall");

        var response = request.RecordResponse("Accept", "Mobile");

        Assert.Equal("Accepted", request.Status);
        Assert.Single(request.Responses);
        Assert.Equal("Accept", response.ResponseType);
    }

    [Fact]
    public void RecordResponse_Reject_SetsRejected()
    {
        var request = NotificationRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(100), "CrewCall");

        request.RecordResponse("Reject");

        Assert.Equal("Rejected", request.Status);
    }

    [Fact]
    public void MarkExpired_SetsExpiredStatus()
    {
        var request = NotificationRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(100), "CrewCall");

        request.MarkExpired();

        Assert.Equal("Expired", request.Status);
    }

    [Fact]
    public void MarkFailed_SetsFailedStatus()
    {
        var request = NotificationRequest.Create(
            ControlNumber.Create(1), ControlNumber.Create(100), "CrewCall");

        request.MarkFailed();

        Assert.Equal("Failed", request.Status);
    }
}
