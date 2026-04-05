using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.DailyOperations;

public class ShiftInstanceTests
{
    [Fact]
    public void Create_SetsStatusToPlanned()
    {
        var shift = ShiftInstance.Create(
            ControlNumber.Create(1), "1", "First Shift");
        Assert.Equal("Planned", shift.Status);
    }

    [Fact]
    public void AddPositionSlot_WithIncumbent_SetsFilled()
    {
        var shift = ShiftInstance.Create(
            ControlNumber.Create(1), "1", "First Shift");

        var slot = shift.AddPositionSlot(
            ControlNumber.Create(10),
            ControlNumber.Create(100), 1,
            ControlNumber.Create(50), "TY-101", "Pool Turn 101", "Engineer");

        Assert.Equal("Filled", slot.Status);
        Assert.Single(shift.PositionSlots);
    }

    [Fact]
    public void AddPositionSlot_NoIncumbent_SetsOpen()
    {
        var shift = ShiftInstance.Create(
            ControlNumber.Create(1), "1", "First Shift");

        var slot = shift.AddPositionSlot(
            ControlNumber.Create(10), null, 1,
            ControlNumber.Create(50), "TY-101", "Pool Turn 101", "Engineer");

        Assert.Equal("Open", slot.Status);
    }

    [Fact]
    public void Complete_SetsStatusAndTimestamp()
    {
        var shift = ShiftInstance.Create(
            ControlNumber.Create(1), "1", "First Shift");
        shift.Complete();

        Assert.Equal("Completed", shift.Status);
        Assert.True(shift.IsComplete);
        Assert.NotNull(shift.CompletedAtUtc);
    }
}

public class PositionSlotInstanceTests
{
    [Fact]
    public void Annul_SetsStatusAndReason()
    {
        var shift = ShiftInstance.Create(
            ControlNumber.Create(1), "1", "First Shift");
        var slot = shift.AddPositionSlot(ControlNumber.Create(10), null, 1,
            ControlNumber.Create(50), "TY-101", "Pool Turn 101", "Engineer");

        slot.Annul("No work available");

        Assert.Equal("Annulled", slot.Status);
        Assert.True(slot.IsAnnulled);
        Assert.Equal("No work available", slot.AnnulmentReason);
    }

    [Fact]
    public void Fill_SetsIncumbentAndStatus()
    {
        var shift = ShiftInstance.Create(
            ControlNumber.Create(1), "1", "First Shift");
        var slot = shift.AddPositionSlot(ControlNumber.Create(10), null, 1,
            ControlNumber.Create(50), "TY-101", "Pool Turn 101", "Engineer");

        slot.Fill(ControlNumber.Create(200));

        Assert.Equal("Filled", slot.Status);
        Assert.Equal(200, slot.IncumbentEmployeeCtrlNbr!.Value);
    }
}

public class OnDutyRecordTests
{
    [Fact]
    public void Create_NormalCall_NotLate()
    {
        var now = DateTime.UtcNow;
        var record = OnDutyRecord.Create(
            ControlNumber.Create(1), ControlNumber.Create(2),
            now, now, 10m, 1, true, 90);

        Assert.False(record.IsLateCall);
        Assert.Null(record.LateCallAdjustedTimeUtc);
        Assert.Equal("OnDuty", record.Status);
    }

    [Fact]
    public void Create_LateCall_DetectedAndAdjusted()
    {
        var scheduled = DateTime.UtcNow;
        var actual = scheduled.AddMinutes(120);
        var record = OnDutyRecord.Create(
            ControlNumber.Create(1), ControlNumber.Create(2),
            actual, scheduled, 10m, 1, true, 90);

        Assert.True(record.IsLateCall);
        Assert.NotNull(record.LateCallAdjustedTimeUtc);
    }
}

public class OffDutyRecordTests
{
    [Fact]
    public void Create_CalculatesRestedAt()
    {
        var offTime = DateTime.UtcNow;
        var record = OffDutyRecord.Create(
            ControlNumber.Create(1), ControlNumber.Create(2),
            offTime, 600, 10m, 24m, "Normal");

        Assert.Equal(offTime.AddHours(10), record.RestedAtUtc);
        Assert.Equal(offTime.AddHours(24), record.ConsecutiveDayRestedAtUtc);
        Assert.Equal("Normal", record.ReleaseReason);
    }
}
