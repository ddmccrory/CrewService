using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Dispatching;

public class ChangeNotificationTests
{
    [Fact]
    public void Create_DefaultsToPending()
    {
        var cn = ChangeNotification.Create(
            ControlNumber.Create(1), "ShiftChange", DateOnly.FromDateTime(DateTime.Today), "Test change");

        Assert.Equal("Pending", cn.Status);
        Assert.Equal("ShiftChange", cn.ChangeType);
    }

    [Fact]
    public void Apply_SetsAppliedStatus()
    {
        var cn = ChangeNotification.Create(
            ControlNumber.Create(1), "ShiftChange", DateOnly.FromDateTime(DateTime.Today), "Test");

        cn.Apply("admin@example.com");

        Assert.Equal("Applied", cn.Status);
    }

    [Fact]
    public void Cancel_SetsCancelledStatus()
    {
        var cn = ChangeNotification.Create(
            ControlNumber.Create(1), "ShiftChange", DateOnly.FromDateTime(DateTime.Today), "Test");

        cn.Cancel("admin@example.com");

        Assert.Equal("Cancelled", cn.Status);
    }
}

public class OnDutyRecordTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var now = DateTime.UtcNow;
        var scheduled = now.AddMinutes(-5);
        var record = OnDutyRecord.Create(
            ControlNumber.Create(1), ControlNumber.Create(100),
            now, scheduled, 10m, 1, true);

        Assert.Equal(1, record.PositionSlotCtrlNbr.Value);
        Assert.Equal(100, record.EmployeeCtrlNbr.Value);
        Assert.Equal("OnDuty", record.Status);
        Assert.False(record.IsLateCall);
    }

    [Fact]
    public void Create_LateCall_SetsIsLateCall()
    {
        var scheduled = DateTime.UtcNow;
        var actual = scheduled.AddMinutes(120);
        var record = OnDutyRecord.Create(
            ControlNumber.Create(1), ControlNumber.Create(100),
            actual, scheduled, 10m, 1, true, lateCallThresholdMinutes: 90);

        Assert.True(record.IsLateCall);
        Assert.NotNull(record.LateCallAdjustedTimeUtc);
    }
}

public class OffDutyRecordTests
{
    [Fact]
    public void Create_CalculatesRestedAtUtc()
    {
        var offDutyTime = DateTime.UtcNow;
        var record = OffDutyRecord.Create(
            ControlNumber.Create(1), ControlNumber.Create(100),
            offDutyTime, 480, 10m, 24m, "Completed");

        Assert.Equal(offDutyTime.AddHours(10), record.RestedAtUtc);
        Assert.Equal(offDutyTime.AddHours(24), record.ConsecutiveDayRestedAtUtc);
        Assert.Equal("Completed", record.ReleaseReason);
    }
}
