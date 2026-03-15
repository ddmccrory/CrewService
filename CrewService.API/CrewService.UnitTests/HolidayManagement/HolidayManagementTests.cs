using CrewService.Domain.Modules.HolidayManagement;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.HolidayManagement;

public class RailroadHolidaySelectionTests
{
    [Fact]
    public void Create_DefaultsToActive()
    {
        var selection = RailroadHolidaySelection.Create(
            ControlNumber.Create(1), "XMAS");

        Assert.Equal("XMAS", selection.HolidayCode);
        Assert.True(selection.IsActive);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var selection = RailroadHolidaySelection.Create(
            ControlNumber.Create(1), "XMAS");

        selection.Deactivate();

        Assert.False(selection.IsActive);
    }

    [Fact]
    public void Activate_ReactivatesAfterDeactivation()
    {
        var selection = RailroadHolidaySelection.Create(
            ControlNumber.Create(1), "XMAS");
        selection.Deactivate();

        selection.Activate();

        Assert.True(selection.IsActive);
    }
}
