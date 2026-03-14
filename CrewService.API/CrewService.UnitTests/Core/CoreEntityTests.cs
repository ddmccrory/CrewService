using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Core;

public class ParentTests
{
    [Fact]
    public void Create_SetsNameAndRaisesEvent()
    {
        var parent = Parent.Create("BNSF Railway");

        Assert.Equal("BNSF Railway", parent.Name.Value);
        Assert.True(parent.DomainEvents.Count > 0);
    }

    [Fact]
    public void Update_ChangesName()
    {
        var parent = Parent.Create("Old Name");

        parent.Update("New Name");

        Assert.Equal("New Name", parent.Name.Value);
    }

    [Fact]
    public void Update_EmptyName_DoesNotChange()
    {
        var parent = Parent.Create("Original");

        parent.Update(string.Empty);

        Assert.Equal("Original", parent.Name.Value);
    }
}

public class RailroadTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var rr = Railroad.Create(1, "BNSF", "BNSF Railway");

        Assert.Equal("BNSF", rr.RailroadMark);
        Assert.Equal("BNSF Railway", rr.Name.Value);
        Assert.True(rr.DomainEvents.Count > 0);
    }

    [Fact]
    public void Update_ChangesSpecifiedFields()
    {
        var rr = Railroad.Create(1, "BNSF", "BNSF Railway");

        rr.Update(0, "UP", "Union Pacific");

        Assert.Equal("UP", rr.RailroadMark);
        Assert.Equal("Union Pacific", rr.Name.Value);
    }
}

public class EmploymentStatusTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var status = EmploymentStatus.Create(1, "ACT", "Active", 1, "E");

        Assert.Equal("ACT", status.StatusCode);
        Assert.Equal("Active", status.StatusName);
        Assert.Equal(1, status.StatusNumber);
        Assert.Equal("E", status.EmploymentCode);
    }
}

public class SeniorityTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var rosterDate = new DateTime(2024, 1, 1);
        var seniority = Seniority.Create(1, 100, true, rosterDate, 5, 1, false);

        Assert.Equal(1, seniority.RosterCtrlNbr.Value);
        Assert.Equal(100, seniority.EmployeeCtrlNbr.Value);
        Assert.True(seniority.LastActiveRoster);
        Assert.Equal(5, seniority.Rank);
        Assert.False(seniority.CanTrain);
    }

    [Fact]
    public void Update_ChangesOnlySpecifiedFields()
    {
        var seniority = Seniority.Create(1, 100, true, DateTime.UtcNow, 5, 1, false);

        seniority.Update(rank: 3, canTrain: true);

        Assert.Equal(3, seniority.Rank);
        Assert.True(seniority.CanTrain);
        Assert.True(seniority.LastActiveRoster);
    }
}
