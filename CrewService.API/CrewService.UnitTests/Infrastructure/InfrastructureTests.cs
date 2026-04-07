using CrewService.Domain.Modules.Infrastructure;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Infrastructure;

public class WorkerScheduleTests
{
    [Fact]
    public void Create_DefaultsEnabled()
    {
        var schedule = WorkerSchedule.Create(ControlNumber.Create(1), "CallSheet");

        Assert.Equal("CallSheet", schedule.WorkerType);
        Assert.True(schedule.IsEnabled);
        Assert.Null(schedule.LastRunUtc);
    }

    [Fact]
    public void IsDue_WhenPastNextFire_ReturnsTrue()
    {
        var schedule = WorkerSchedule.Create(
            ControlNumber.Create(1), "CallSheet",
            nextFireUtc: DateTime.UtcNow.AddMinutes(-1));

        Assert.True(schedule.IsDue());
    }

    [Fact]
    public void IsDue_WhenDisabled_ReturnsFalse()
    {
        var schedule = WorkerSchedule.Create(
            ControlNumber.Create(1), "CallSheet",
            nextFireUtc: DateTime.UtcNow.AddMinutes(-1), isEnabled: false);

        Assert.False(schedule.IsDue());
    }

    [Fact]
    public void RecordSuccess_SetsLastRunStatus()
    {
        var schedule = WorkerSchedule.Create(ControlNumber.Create(1), "CallSheet");
        var next = DateTime.UtcNow.AddMinutes(5);

        schedule.RecordSuccess(next);

        Assert.Equal("Success", schedule.LastRunStatus);
        Assert.NotNull(schedule.LastRunUtc);
        Assert.Equal(next, schedule.NextFireUtc);
    }

    [Fact]
    public void RecordFailure_SetsFailedStatus()
    {
        var schedule = WorkerSchedule.Create(ControlNumber.Create(1), "CallSheet");

        schedule.RecordFailure();

        Assert.Equal("Failed", schedule.LastRunStatus);
    }
}

public class WorkerExecutionLogTests
{
    [Fact]
    public void Start_DefaultsToRunning()
    {
        var log = WorkerExecutionLog.Start(ControlNumber.Create(1));

        Assert.Equal("Running", log.Status);
        Assert.Null(log.CompletedAtUtc);
    }

    [Fact]
    public void Complete_SetsSuccess()
    {
        var log = WorkerExecutionLog.Start(ControlNumber.Create(1));

        log.Complete();

        Assert.Equal("Success", log.Status);
        Assert.NotNull(log.CompletedAtUtc);
    }

    [Fact]
    public void Fail_SetsFailedWithMessage()
    {
        var log = WorkerExecutionLog.Start(ControlNumber.Create(1));

        log.Fail("Connection timeout");

        Assert.Equal("Failed", log.Status);
        Assert.Equal("Connection timeout", log.ErrorMessage);
        Assert.NotNull(log.CompletedAtUtc);
    }
}

public class ShiftInstanceTests
{
    [Fact]
    public void Create_DefaultsToPlanned()
    {
        var instance = ShiftInstance.Create(ControlNumber.Create(1), ControlNumber.Create(1000), "DAY", "Day Shift");

        Assert.Equal("Planned", instance.Status);
        Assert.False(instance.IsComplete);
        Assert.Empty(instance.PositionSlots);
    }

    [Fact]
    public void AddPositionSlot_AddsOpenSlot()
    {
        var instance = ShiftInstance.Create(ControlNumber.Create(1), ControlNumber.Create(1000), "DAY", "Day Shift");

        var slot = instance.AddPositionSlot(ControlNumber.Create(10), null, 1,
            ControlNumber.Create(50), "TY-101", "Pool Turn 101", "Engineer", "", "",
            new TimeOnly(7, 0), new TimeOnly(15, 0));

        Assert.Single(instance.PositionSlots);
        Assert.Equal(PositionSlotStatus.Open, slot.Status);
    }

    [Fact]
    public void AddPositionSlot_WithIncumbent_AddsFilled()
    {
        var instance = ShiftInstance.Create(ControlNumber.Create(1), ControlNumber.Create(1000), "DAY", "Day Shift");

        var slot = instance.AddPositionSlot(ControlNumber.Create(10), ControlNumber.Create(100), 1,
            ControlNumber.Create(50), "TY-101", "Pool Turn 101", "Engineer", "", "",
            new TimeOnly(7, 0), new TimeOnly(15, 0));

        Assert.Equal(PositionSlotStatus.Filled, slot.Status);
        Assert.Equal(100, slot.IncumbentEmployeeCtrlNbr!.Value);
    }

    [Fact]
    public void Activate_SetsActive()
    {
        var instance = ShiftInstance.Create(ControlNumber.Create(1), ControlNumber.Create(1000), "DAY", "Day Shift");

        instance.Activate();

        Assert.Equal("Active", instance.Status);
    }

    [Fact]
    public void Complete_SetsCompletedAndTimestamp()
    {
        var instance = ShiftInstance.Create(ControlNumber.Create(1), ControlNumber.Create(1000), "DAY", "Day Shift");

        instance.Complete();

        Assert.Equal("Completed", instance.Status);
        Assert.True(instance.IsComplete);
        Assert.NotNull(instance.CompletedAtUtc);
    }

    [Fact]
    public void Cancel_SetsCancelled()
    {
        var instance = ShiftInstance.Create(ControlNumber.Create(1), ControlNumber.Create(1000), "DAY", "Day Shift");

        instance.Cancel();

        Assert.Equal("Cancelled", instance.Status);
    }
}
