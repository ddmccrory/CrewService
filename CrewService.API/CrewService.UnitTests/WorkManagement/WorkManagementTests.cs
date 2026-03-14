using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.WorkManagement;

public class ShiftDefinitionTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var def = ShiftDefinition.Create(
            ControlNumber.Create(1), "DAY", "Day Shift",
            new TimeOnly(7, 0), new TimeOnly(15, 0), 1, true);

        Assert.Equal("DAY", def.ShiftCode);
        Assert.Equal("Day Shift", def.DisplayName);
        Assert.True(def.IsActive);
    }

    [Fact]
    public void Update_ChangesOnlySpecifiedFields()
    {
        var def = ShiftDefinition.Create(
            ControlNumber.Create(1), "DAY", "Day Shift",
            new TimeOnly(7, 0), new TimeOnly(15, 0), 1, true);

        def.Update(displayName: "Morning Shift", isActive: false);

        Assert.Equal("Morning Shift", def.DisplayName);
        Assert.False(def.IsActive);
        Assert.Equal("DAY", def.ShiftCode);
    }
}

public class PositionSlotInstanceTests
{
    [Fact]
    public void Fill_SetsFilled()
    {
        var slot = CreateSlot();

        slot.Fill(ControlNumber.Create(100));

        Assert.Equal("Filled", slot.Status);
        Assert.Equal(100, slot.IncumbentEmployeeCtrlNbr!.Value);
    }

    [Fact]
    public void MarkOnDuty_SetsOnDuty()
    {
        var slot = CreateSlot();
        slot.Fill(ControlNumber.Create(100));

        slot.MarkOnDuty();

        Assert.Equal("OnDuty", slot.Status);
    }

    [Fact]
    public void MarkTiedUp_SetsTiedUp()
    {
        var slot = CreateSlot();

        slot.MarkTiedUp();

        Assert.Equal("TiedUp", slot.Status);
    }

    [Fact]
    public void Annul_SetsAnnulledWithReason()
    {
        var slot = CreateSlot();

        slot.Annul("Position abolished");

        Assert.Equal("Annulled", slot.Status);
        Assert.True(slot.IsAnnulled);
        Assert.Equal("Position abolished", slot.AnnulmentReason);
    }

    [Fact]
    public void MarkDoNotFill_SetsDoNotFill()
    {
        var slot = CreateSlot();

        slot.MarkDoNotFill();

        Assert.Equal("DoNotFill", slot.Status);
        Assert.True(slot.IsDoNotFill);
    }

    [Fact]
    public void Skip_SetsSkipped()
    {
        var slot = CreateSlot();

        slot.Skip();

        Assert.Equal("Skipped", slot.Status);
        Assert.True(slot.IsSkipped);
    }

    private static PositionSlotInstance CreateSlot()
    {
        var shiftInstance = ShiftInstance.Create(
            ControlNumber.Create(1), "DAY", DateTime.UtcNow, DateTime.UtcNow.AddHours(8));

        var slot = shiftInstance.AddPositionSlot(ControlNumber.Create(10), null, 1);
        return slot;
    }
}
