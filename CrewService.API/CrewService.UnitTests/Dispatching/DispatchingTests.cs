using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Dispatching;

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

public class VacancyResolutionRunTests
{
    [Fact]
    public void Start_DefaultsToRunning()
    {
        var run = VacancyResolutionRun.Start(ControlNumber.Create(1), ControlNumber.Create(10));

        Assert.Equal("Running", run.Status);
        Assert.Null(run.CompletedAtUtc);
    }

    [Fact]
    public void Complete_SetsSlotsAndCompleted()
    {
        var run = VacancyResolutionRun.Start(ControlNumber.Create(1), ControlNumber.Create(10));

        run.Complete(5, 3);

        Assert.Equal("Completed", run.Status);
        Assert.Equal(5, run.SlotsEvaluated);
        Assert.Equal(3, run.SlotsFilled);
        Assert.NotNull(run.CompletedAtUtc);
    }

    [Fact]
    public void Fail_SetsFailedStatus()
    {
        var run = VacancyResolutionRun.Start(ControlNumber.Create(1), ControlNumber.Create(10));

        run.Fail();

        Assert.Equal("Failed", run.Status);
    }
}

public class DailyEmployeeStatusRecordTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var record = DailyEmployeeStatusRecord.Create(
            ControlNumber.Create(100), ControlNumber.Create(1),
            DateOnly.FromDateTime(DateTime.Today), "ON_DUTY", "{\"shift\":\"DAY\"}");

        Assert.Equal("ON_DUTY", record.StatusCode);
        Assert.Equal("{\"shift\":\"DAY\"}", record.SnapshotJson);
    }
}
