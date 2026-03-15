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

public class AssignmentTemplateTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var template = AssignmentTemplate.Create(1, "TMPL1", "Morning Template", null);

        Assert.Equal("TMPL1", template.Code);
        Assert.Equal("Morning Template", template.Name);
        Assert.True(template.IsActive);
        Assert.True(template.DomainEvents.Count > 0);
    }

    [Fact]
    public void Update_ChangesAllFields()
    {
        var template = AssignmentTemplate.Create(1, "TMPL1", "Old", null);

        template.Update("TMPL2", "New", "{}", false);

        Assert.Equal("TMPL2", template.Code);
        Assert.Equal("New", template.Name);
        Assert.Equal("{}", template.RecurrenceJson);
        Assert.False(template.IsActive);
    }
}

public class WorkInstanceTests
{
    [Fact]
    public void Create_DefaultsToPlanned()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(8);
        var instance = WorkInstance.Create(null, 1, start, end, null);

        Assert.Equal("Planned", instance.Status);
        Assert.Null(instance.AssignmentTemplateCtrlNbr);
        Assert.True(instance.DomainEvents.Count > 0);
    }

    [Fact]
    public void UpdateStatus_ChangesStatus()
    {
        var instance = WorkInstance.Create(null, 1, DateTime.UtcNow, DateTime.UtcNow.AddHours(8), null);

        instance.UpdateStatus("Active");

        Assert.Equal("Active", instance.Status);
    }
}

public class AbolishmentRecordTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var record = AbolishmentRecord.Create(
            ControlNumber.Create(1), "Position", DateOnly.FromDateTime(DateTime.Today), "Budget cut");

        Assert.Equal("Position", record.AbolishmentType);
        Assert.Equal("Budget cut", record.Reason);
        Assert.Null(record.RestoredDate);
    }

    [Fact]
    public void IsActive_BeforeRestore_ReturnsTrue()
    {
        var record = AbolishmentRecord.Create(
            ControlNumber.Create(1), "Position", DateOnly.FromDateTime(DateTime.Today), "Budget cut");

        Assert.True(record.IsActive(DateOnly.FromDateTime(DateTime.Today)));
    }

    [Fact]
    public void Restore_SetsRestoredDateAndIsActiveFalse()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var record = AbolishmentRecord.Create(
            ControlNumber.Create(1), "Position", today, "Budget cut");

        record.Restore(today.AddDays(30));

        Assert.Equal(today.AddDays(30), record.RestoredDate);
        Assert.False(record.IsActive(today.AddDays(31)));
    }
}

public class CrewOffDayTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var offDay = CrewOffDay.Create(ControlNumber.Create(1), DayOfWeek.Sunday);

        Assert.Equal(1, offDay.CrewPositionCtrlNbr.Value);
        Assert.Equal(DayOfWeek.Sunday, offDay.DayOfWeek);
    }
}

public class PositionRoleTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var role = PositionRole.Create(1, "ENG", "Engineer");

        Assert.Equal(1, role.CraftCtrlNbr.Value);
        Assert.Equal("ENG", role.Code);
        Assert.Equal("Engineer", role.Name);
    }
}
