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

        slot.Annul("Position abolished", DateTime.UtcNow);

        Assert.Equal(PositionSlotStatus.Annulled, slot.Status);
        Assert.True(slot.IsAnnulled);
        Assert.Equal("Position abolished", slot.AnnulmentReason);
        Assert.NotNull(slot.AnnulmentDateTimeUtc);
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

    [Fact]
    public void Restore_FromAnnulled_ResetsToOpen()
    {
        var slot = CreateSlot();
        slot.Annul("No work", DateTime.UtcNow);

        slot.RestoreSlot();

        Assert.Equal(PositionSlotStatus.Open, slot.Status);
        Assert.False(slot.IsAnnulled);
        Assert.Null(slot.AnnulmentReason);
        Assert.Null(slot.AnnulmentDateTimeUtc);
        Assert.False(slot.IsDoNotFill);
    }

    [Fact]
    public void Restore_FromDoNotFill_ResetsToOpen()
    {
        var slot = CreateSlot();
        slot.MarkDoNotFill();

        slot.RestoreSlot();

        Assert.Equal(PositionSlotStatus.Open, slot.Status);
        Assert.False(slot.IsDoNotFill);
    }

    [Fact]
    public void Restore_WithIncumbent_ResetsToFilled()
    {
        var shiftInstance = ShiftInstance.Create(ControlNumber.Create(1), ControlNumber.Create(1000), "DAY", "Day Shift");
        var slot = shiftInstance.AddPositionSlot(ControlNumber.Create(10), ControlNumber.Create(200), 1,
            ControlNumber.Create(50), "TY-101", "Pool Turn 101", "Engineer", "", "",
            new TimeOnly(7, 0), new TimeOnly(15, 0));
        slot.Annul("Temp annul", DateTime.UtcNow);

        slot.RestoreSlot();

        Assert.Equal(PositionSlotStatus.Filled, slot.Status);
        Assert.False(slot.IsAnnulled);
        Assert.Equal(200, slot.IncumbentEmployeeCtrlNbr!.Value);
    }

    [Fact]
    public void RestoreSlot_AllAnnulledSlots_RestoresEachToCorrectStatus()
    {
        var shift = ShiftInstance.Create(ControlNumber.Create(1), ControlNumber.Create(1000), "DAY", "Day Shift");
        var assignmentCtrlNbr = ControlNumber.Create(50);

        var vacant = shift.AddPositionSlot(ControlNumber.Create(10), null, 1,
            assignmentCtrlNbr, "TY-101", "Pool Turn 101", "Engineer", "", "",
            new TimeOnly(7, 0), new TimeOnly(15, 0));
        var filled = shift.AddPositionSlot(ControlNumber.Create(11), ControlNumber.Create(200), 2,
            assignmentCtrlNbr, "TY-101", "Pool Turn 101", "Foreman", "", "",
            new TimeOnly(7, 0), new TimeOnly(15, 0));

        vacant.Annul("No work", DateTime.UtcNow);
        filled.Annul("No work", DateTime.UtcNow);

        foreach (var slot in shift.PositionSlots.Where(s => s.AssignmentCtrlNbr == assignmentCtrlNbr && s.IsAnnulled))
            slot.RestoreSlot();

        Assert.Equal(PositionSlotStatus.Open, vacant.Status);
        Assert.False(vacant.IsAnnulled);
        Assert.Equal(PositionSlotStatus.Filled, filled.Status);
        Assert.False(filled.IsAnnulled);
        Assert.Equal(200, filled.IncumbentEmployeeCtrlNbr!.Value);
    }

    private static PositionSlotInstance CreateSlot()
    {
        var shiftInstance = ShiftInstance.Create(ControlNumber.Create(1), ControlNumber.Create(1000), "DAY", "Day Shift");

        var slot = shiftInstance.AddPositionSlot(ControlNumber.Create(10), null, 1,
            ControlNumber.Create(50), "TY-101", "Pool Turn 101", "Engineer", "", "",
            new TimeOnly(7, 0), new TimeOnly(15, 0));
        return slot;
    }
}


public class ShiftInstancePositionManagementTests
{
    [Fact]
    public void AddAdHocPositionSlot_CreatesAdHocSlotWithCopiedMetadata()
    {
        var shift = CreateShiftWithPositions();
        var assignmentCtrlNbr = ControlNumber.Create(50);

        var adHoc = shift.AddAdHocPositionSlot(assignmentCtrlNbr, "Conductor");

        Assert.True(adHoc.IsAdHoc);
        Assert.Null(adHoc.CrewPositionCtrlNbr);
        Assert.Equal(PositionSlotStatus.Open, adHoc.Status);
        Assert.Equal("Conductor", adHoc.CraftRoleName);
        Assert.Equal("TY-101", adHoc.AssignmentCode);
        Assert.Equal("Pool Turn 101", adHoc.AssignmentName);
    }

    [Fact]
    public void AddAdHocPositionSlot_CalculatesDisplayOrderPerCraft()
    {
        var shift = CreateShiftWithPositions();
        var assignmentCtrlNbr = ControlNumber.Create(50);

        // Existing Engineer slot has DisplayOrder = 1
        var adHoc = shift.AddAdHocPositionSlot(assignmentCtrlNbr, "Engineer");

        Assert.Equal(2, adHoc.DisplayOrder);
    }

    [Fact]
    public void AddAdHocPositionSlot_NewCraftRole_StartsAtDisplayOrder1()
    {
        var shift = CreateShiftWithPositions();
        var assignmentCtrlNbr = ControlNumber.Create(50);

        var adHoc = shift.AddAdHocPositionSlot(assignmentCtrlNbr, "Brakeman");

        Assert.Equal(1, adHoc.DisplayOrder);
    }

    [Fact]
    public void AddAdHocPositionSlot_NoExistingAssignment_Throws()
    {
        var shift = CreateShiftWithPositions();

        Assert.Throws<InvalidOperationException>(() =>
            shift.AddAdHocPositionSlot(ControlNumber.Create(999), "Engineer"));
    }

    [Fact]
    public void RemovePositionSlot_AdHocOpen_Removes()
    {
        var shift = CreateShiftWithPositions();
        var adHoc = shift.AddAdHocPositionSlot(ControlNumber.Create(50), "Conductor");
        var initialCount = shift.PositionSlots.Count;

        shift.RemovePositionSlot(adHoc.CtrlNbr);

        Assert.Equal(initialCount - 1, shift.PositionSlots.Count);
        Assert.DoesNotContain(adHoc, shift.PositionSlots);
    }

    [Fact]
    public void RemovePositionSlot_TemplateSlot_Throws()
    {
        var shift = CreateShiftWithPositions();
        var templateSlot = shift.PositionSlots[0]; // Template slot from AddPositionSlot

        Assert.Throws<InvalidOperationException>(() =>
            shift.RemovePositionSlot(templateSlot.CtrlNbr));
    }

    [Fact]
    public void RemovePositionSlot_FilledAdHoc_Throws()
    {
        var shift = CreateShiftWithPositions();
        var adHoc = shift.AddAdHocPositionSlot(ControlNumber.Create(50), "Conductor");
        adHoc.Fill(ControlNumber.Create(200));

        Assert.Throws<InvalidOperationException>(() =>
            shift.RemovePositionSlot(adHoc.CtrlNbr));
    }

    [Fact]
    public void RemovePositionSlot_NotFound_Throws()
    {
        var shift = CreateShiftWithPositions();

        Assert.Throws<InvalidOperationException>(() =>
            shift.RemovePositionSlot(ControlNumber.Create(999)));
    }

    [Fact]
    public void ReorderPositionSlots_UpdatesDisplayOrders()
    {
        var shift = CreateShiftWithPositions();
        var assignmentCtrlNbr = ControlNumber.Create(50);
        var adHoc = shift.AddAdHocPositionSlot(assignmentCtrlNbr, "Engineer");

        var original = shift.PositionSlots[0]; // DisplayOrder = 1
        // adHoc has DisplayOrder = 2

        shift.ReorderPositionSlots(
        [
            (original.CtrlNbr, 2),
            (adHoc.CtrlNbr, 1)
        ]);

        Assert.Equal(2, original.DisplayOrder);
        Assert.Equal(1, adHoc.DisplayOrder);
    }

    [Fact]
    public void ReorderPositionSlots_IgnoresUnknownCtrlNbrs()
    {
        var shift = CreateShiftWithPositions();
        var slot = shift.PositionSlots[0];

        // Should not throw
        shift.ReorderPositionSlots(
        [
            (slot.CtrlNbr, 5),
            (ControlNumber.Create(999), 10)
        ]);

        Assert.Equal(5, slot.DisplayOrder);
    }

    private static ShiftInstance CreateShiftWithPositions()
    {
        var shift = ShiftInstance.Create(ControlNumber.Create(1), ControlNumber.Create(1000), "DAY", "Day Shift");
        shift.AddPositionSlot(ControlNumber.Create(10), null, 1,
            ControlNumber.Create(50), "TY-101", "Pool Turn 101", "Engineer", "Pool", "POOL",
            new TimeOnly(7, 0), new TimeOnly(15, 0));
        return shift;
    }
}


public class ShiftInstanceNoteTests
{
    [Fact]
    public void SetAssignmentNote_CreatesNewNote()
    {
        var shift = ShiftInstance.Create(ControlNumber.Create(1), ControlNumber.Create(1000), "DAY", "Day Shift");
        var assignmentCtrlNbr = ControlNumber.Create(50);

        shift.SetAssignmentNote(assignmentCtrlNbr, "Test note");

        Assert.Single(shift.AssignmentNotes);
        Assert.Equal(50, shift.AssignmentNotes[0].AssignmentCtrlNbr.Value);
        Assert.Equal("Test note", shift.AssignmentNotes[0].NoteText);
    }

    [Fact]
    public void SetAssignmentNote_UpdatesExistingNote()
    {
        var shift = ShiftInstance.Create(ControlNumber.Create(1), ControlNumber.Create(1000), "DAY", "Day Shift");
        var assignmentCtrlNbr = ControlNumber.Create(50);

        shift.SetAssignmentNote(assignmentCtrlNbr, "Original");
        shift.SetAssignmentNote(assignmentCtrlNbr, "Updated");

        Assert.Single(shift.AssignmentNotes);
        Assert.Equal("Updated", shift.AssignmentNotes[0].NoteText);
    }

    [Fact]
    public void SetAssignmentNote_DifferentAssignments_CreatesSeparateNotes()
    {
        var shift = ShiftInstance.Create(ControlNumber.Create(1), ControlNumber.Create(1000), "DAY", "Day Shift");

        shift.SetAssignmentNote(ControlNumber.Create(50), "Note A");
        shift.SetAssignmentNote(ControlNumber.Create(60), "Note B");

        Assert.Equal(2, shift.AssignmentNotes.Count);
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

        dept.Update("Mechanical", "horizontal");

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
