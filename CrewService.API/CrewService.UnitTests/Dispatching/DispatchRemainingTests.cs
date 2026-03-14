using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Dispatching;

public class DispatchOverrideTests
{
    [Fact]
    public void Create_DefaultsToPending()
    {
        var ov = DispatchOverride.Create(1, 100, "SKIP", "REST", "Employee resting");

        Assert.Equal("PENDING", ov.Status);
        Assert.Null(ov.ApprovedByCtrlNbr);
    }

    [Fact]
    public void Approve_SetsApprovedStatus()
    {
        var ov = DispatchOverride.Create(1, 100, "SKIP", "REST", null);

        ov.Approve(200);

        Assert.Equal("APPROVED", ov.Status);
        Assert.Equal(200, ov.ApprovedByCtrlNbr!.Value);
        Assert.NotNull(ov.ApprovedAtUtc);
    }

    [Fact]
    public void Reject_SetsRejectedStatus()
    {
        var ov = DispatchOverride.Create(1, 100, "SKIP", "REST", null);

        ov.Reject();

        Assert.Equal("REJECTED", ov.Status);
    }
}

public class DispatchDecisionLogTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var log = DispatchDecisionLog.Create(1, DateTime.UtcNow, "ElectronicCall", 100, "ExtraBoard", null);

        Assert.Equal("ElectronicCall", log.Phase);
        Assert.Equal(100, log.SelectedEmployeeCtrlNbr!.Value);
        Assert.True(log.DomainEvents.Count > 0);
    }
}

public class DispatchProjectionTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var proj = DispatchProjection.Create(1, DateTime.UtcNow, 100, "{\"trace\":true}");

        Assert.Equal(100, proj.ProjectedEmployeeCtrlNbr!.Value);
        Assert.Equal("{\"trace\":true}", proj.TraceJson);
    }
}

public class EmployeeBookingTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(8);
        var booking = EmployeeBooking.Create(100, start, end, 50);

        Assert.Equal(100, booking.EmployeeCtrlNbr.Value);
        Assert.Equal(50, booking.PositionSlotCtrlNbr!.Value);
    }
}
