using CrewService.BlazorUI.Components.Pages.Staffing;
using Xunit;

namespace CrewService.BlazorUI.Tests.Pages.Staffing;

public class CrewAssignmentEntryTests
{
    [Fact]
    public void Defaults_StartDateIsToday()
    {
        var entry = new CrewSetupWizard.CrewAssignmentEntry();

        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), entry.StartDate);
    }

    [Fact]
    public void Defaults_EndDateIsNull()
    {
        var entry = new CrewSetupWizard.CrewAssignmentEntry();

        Assert.Null(entry.EndDate);
    }

    [Fact]
    public void Defaults_NewAssignment_DutyTimes()
    {
        var entry = new CrewSetupWizard.CrewAssignmentEntry();

        Assert.Equal(new TimeOnly(7, 0), entry.NewAssignment.OnDutyTime);
        Assert.Equal(new TimeOnly(15, 0), entry.NewAssignment.OffDutyTime);
    }

    [Fact]
    public void IsValid_NewEntry_ReturnsFalse_WhenMissingRequired()
    {
        var entry = new CrewSetupWizard.CrewAssignmentEntry
        {
            UseExisting = false,
            NewAssignment = new()
            {
                Code = "",
                Name = "",
                GroupCtrlNbr = 0,
                ShiftDefinitionCtrlNbr = 0
            }
        };

        Assert.False(entry.IsValid);
    }

    [Fact]
    public void IsValid_NewEntry_ReturnsFalse_WhenMissingShift()
    {
        var entry = new CrewSetupWizard.CrewAssignmentEntry
        {
            UseExisting = false,
            NewAssignment = new()
            {
                Code = "A001",
                Name = "Test Assignment",
                GroupCtrlNbr = 1,
                ShiftDefinitionCtrlNbr = 0
            }
        };

        Assert.False(entry.IsValid);
    }

    [Fact]
    public void IsValid_NewEntry_ReturnsTrue_WhenAllFieldsSet()
    {
        var entry = new CrewSetupWizard.CrewAssignmentEntry
        {
            UseExisting = false,
            NewAssignment = new()
            {
                Code = "A001",
                Name = "Test Assignment",
                GroupCtrlNbr = 1,
                ShiftDefinitionCtrlNbr = 100
            }
        };

        Assert.True(entry.IsValid);
    }

    [Fact]
    public void IsValid_ExistingEntry_ReturnsFalse_WhenNoAssignmentSelected()
    {
        var entry = new CrewSetupWizard.CrewAssignmentEntry
        {
            UseExisting = true,
            ExistingAssignmentCtrlNbr = 0
        };

        Assert.False(entry.IsValid);
    }

    [Fact]
    public void IsValid_ExistingEntry_ReturnsTrue_WhenAssignmentSelected()
    {
        var entry = new CrewSetupWizard.CrewAssignmentEntry
        {
            UseExisting = true,
            ExistingAssignmentCtrlNbr = 42,
            NewAssignment = new()
            {
                Code = "A001",
                Name = "Test Assignment",
                GroupCtrlNbr = 1,
                ShiftDefinitionCtrlNbr = 100
            }
        };

        Assert.True(entry.IsValid);
    }

    [Fact]
    public void IsValid_ExistingEntry_ReturnsFalse_WhenNewAssignmentFieldsEmpty()
    {
        var entry = new CrewSetupWizard.CrewAssignmentEntry
        {
            UseExisting = true,
            ExistingAssignmentCtrlNbr = 42,
            NewAssignment = new()
            {
                Code = "",
                Name = "",
                GroupCtrlNbr = 0,
                ShiftDefinitionCtrlNbr = 0
            }
        };

        Assert.False(entry.IsValid);
    }

    [Fact]
    public void IsAssignmentValid_NewEntry_ReturnsTrue_WhenAssignmentFieldsSet()
    {
        var entry = new CrewSetupWizard.CrewAssignmentEntry
        {
            UseExisting = false,
            NewAssignment = new()
            {
                Code = "A001",
                Name = "Test",
                GroupCtrlNbr = 1,
                ShiftDefinitionCtrlNbr = 100
            }
        };

        Assert.True(entry.IsAssignmentValid);
    }

    [Fact]
    public void IsAssignmentValid_ExistingEntry_ReturnsFalse_WhenNoSelection()
    {
        var entry = new CrewSetupWizard.CrewAssignmentEntry
        {
            UseExisting = true,
            ExistingAssignmentCtrlNbr = 0
        };

        Assert.False(entry.IsAssignmentValid);
    }
}

public class WizardPositionEntryTests
{
    [Fact]
    public void Defaults_DisplayOrderIsOne()
    {
        var entry = new CrewSetupWizard.WizardPositionEntry();

        Assert.Equal(1, entry.DisplayOrder);
    }

    [Fact]
    public void Defaults_CraftRoleCtrlNbrIsZero()
    {
        var entry = new CrewSetupWizard.WizardPositionEntry();

        Assert.Equal(0, entry.CraftRoleCtrlNbr);
    }
}
