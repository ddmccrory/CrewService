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
            ControlNumber.Create(1), "DAY", "Day Shift", 1, true);

        Assert.Equal("DAY", def.ShiftCode);
        Assert.Equal("Day Shift", def.DisplayName);
        Assert.True(def.IsActive);
    }

    [Fact]
    public void Update_ChangesOnlySpecifiedFields()
    {
        var def = ShiftDefinition.Create(
            ControlNumber.Create(1), "DAY", "Day Shift", 1, true);

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

        Assert.Equal(PositionSlotStatus.Filled, slot.Status);
        Assert.Equal(100, slot.IncumbentEmployeeCtrlNbr!.Value);
    }

    [Fact]
    public void MarkOnDuty_SetsOnDuty()
    {
        var slot = CreateSlot();
        slot.Fill(ControlNumber.Create(100));

        slot.MarkOnDuty();

        Assert.Equal(PositionSlotStatus.OnDuty, slot.Status);
    }

    [Fact]
    public void MarkTiedUp_SetsTiedUp()
    {
        var slot = CreateSlot();

        slot.MarkTiedUp();

        Assert.Equal(PositionSlotStatus.TiedUp, slot.Status);
    }

    [Fact]
    public void Annul_SetsAnnulledWithReason()
    {
        var slot = CreateSlot();

        slot.Annul("Position abolished");

        Assert.Equal(PositionSlotStatus.Annulled, slot.Status);
        Assert.True(slot.IsAnnulled);
        Assert.Equal("Position abolished", slot.AnnulmentReason);
    }

    [Fact]
    public void MarkDoNotFill_SetsDoNotFill()
    {
        var slot = CreateSlot();

        slot.MarkDoNotFill();

        Assert.Equal(PositionSlotStatus.DoNotFill, slot.Status);
        Assert.True(slot.IsDoNotFill);
    }

    [Fact]
    public void Skip_SetsSkipped()
    {
        var slot = CreateSlot();

        slot.Skip();

        Assert.Equal(PositionSlotStatus.Skipped, slot.Status);
        Assert.True(slot.IsSkipped);
    }

    private static PositionSlotInstance CreateSlot()
    {
        var shiftInstance = ShiftInstance.Create(
            ControlNumber.Create(1), "DAY", "Day Shift");

        var slot = shiftInstance.AddPositionSlot(ControlNumber.Create(10), null, 1,
            ControlNumber.Create(50), "TY-101", "Pool Turn 101", "Engineer", "", "",
            new TimeOnly(7, 0), new TimeOnly(15, 0));
        return slot;
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
        Assert.Null(instance.AssignmentGroupCtrlNbr);
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


public class DepartmentTests
{
    [Fact]
    public void Create_WithRailroad_SetsProperties()
    {
        var dept = Department.Create(10, ControlNumber.Create(5), "Transportation");

        Assert.Equal(10, dept.ParentCtrlNbr);
        Assert.Equal(5, dept.DynamicGroupCtrlNbr!.Value);
        Assert.Equal("Transportation", dept.Name);
    }

    [Fact]
    public void Create_ParentLevel_HasNullDynamicGroup()
    {
        var dept = Department.Create(10, null, "Safety");

        Assert.Equal(10, dept.ParentCtrlNbr);
        Assert.Null(dept.DynamicGroupCtrlNbr);
        Assert.Equal("Safety", dept.Name);
    }

    [Fact]
    public void Update_ChangesName()
    {
        var dept = Department.Create(10, ControlNumber.Create(5), "Transportation");

        dept.Update("Mechanical");

        Assert.Equal("Mechanical", dept.Name);
        Assert.Equal(10, dept.ParentCtrlNbr);
        Assert.Equal(5, dept.DynamicGroupCtrlNbr!.Value);
    }
}

public class CraftRoleTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var role = CraftRole.Create(1, "ENG", "Engineer", "Locomotive Engineer");

        Assert.Equal(1, role.CraftCtrlNbr.Value);
        Assert.Equal("ENG", role.Code);
        Assert.Equal("Engineer", role.Name);
        Assert.Equal("Locomotive Engineer", role.AlternateName);
    }

    [Fact]
    public void Create_WithNullCode_AllowsNull()
    {
        var role = CraftRole.Create(1, null, "Brakeman");

        Assert.Null(role.Code);
        Assert.Equal("Brakeman", role.Name);
        Assert.Null(role.AlternateName);
    }

    [Fact]
    public void Update_ChangesAllFields()
    {
        var role = CraftRole.Create(1, "ENG", "Engineer");

        role.Update("ENGR", "Senior Engineer", "Lead Locomotive Engineer");

        Assert.Equal("ENGR", role.Code);
        Assert.Equal("Senior Engineer", role.Name);
        Assert.Equal("Lead Locomotive Engineer", role.AlternateName);
    }

    [Fact]
    public void Update_ClearsOptionalFields()
    {
        var role = CraftRole.Create(1, "ENG", "Engineer", "Locomotive Engineer");

        role.Update(null, "Engineer", null);

        Assert.Null(role.Code);
        Assert.Equal("Engineer", role.Name);
        Assert.Null(role.AlternateName);
    }
}
