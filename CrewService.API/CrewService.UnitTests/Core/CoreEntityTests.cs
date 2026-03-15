using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Employees;
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

public class CraftTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var craft = Craft.Create(1, "Engineer", "Engineers", 1,
            true, false, 8, 8, 10, 24, 30, true, true, true, 0);

        Assert.Equal("Engineer", craft.CraftName);
        Assert.Equal("Engineers", craft.CraftPluralName);
        Assert.Equal(1, craft.CraftNumber);
        Assert.True(craft.AutoMarkUp);
        Assert.True(craft.HoursofService);
        Assert.True(craft.DomainEvents.Count > 0);
    }
}

public class RosterTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var roster = Roster.Create(1, 10, "Main Roster", "Main Rosters", 1,
            false, true, false);

        Assert.Equal("Main Roster", roster.RosterName);
        Assert.Equal(1, roster.RosterNumber);
        Assert.True(roster.ExtraBoard);
        Assert.False(roster.Training);
        Assert.True(roster.DomainEvents.Count > 0);
    }
}

public class EmployeePriorServiceCreditTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var credit = EmployeePriorServiceCredit.Create(100, 5, 3, 15);

        Assert.Equal(100, credit.EmployeeCtrlNbr.Value);
        Assert.Equal(5, credit.ServiceYears);
        Assert.Equal(3, credit.ServiceMonths);
        Assert.Equal(15, credit.ServiceDays);
    }
}

public class EmploymentStatusHistoryTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var changeDate = DateTime.UtcNow;
        var history = EmploymentStatusHistory.Create(100, 200, changeDate);

        Assert.Equal(100, history.EmployeeCtrlNbr.Value);
        Assert.Equal(200, history.EmploymentStatusCtrlNbr.Value);
        Assert.Equal(changeDate, history.StatusChangeDate);
        Assert.True(history.DomainEvents.Count > 0);
    }
}

public class SeniorityStateTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var state = SeniorityState.Create("Active Duty", true, false, false);

        Assert.Equal("Active Duty", state.StateDescription);
        Assert.True(state.Active);
        Assert.False(state.CutBack);
        Assert.False(state.Inactive);
        Assert.True(state.DomainEvents.Count > 0);
    }

    [Fact]
    public void Update_ChangesFields_RaisesEvent()
    {
        var state = SeniorityState.Create("Active Duty", true, false, false);
        var eventsBefore = state.DomainEvents.Count;

        state.Update("Cut Back", false, true, false);

        Assert.Equal("Cut Back", state.StateDescription);
        Assert.False(state.Active);
        Assert.True(state.CutBack);
        Assert.True(state.DomainEvents.Count > eventsBefore);
    }

    [Fact]
    public void Update_NoChanges_DoesNotRaiseEvent()
    {
        var state = SeniorityState.Create("Active Duty", true, false, false);
        var eventsBefore = state.DomainEvents.Count;

        state.Update("Active Duty", true, false, false);

        Assert.Equal(eventsBefore, state.DomainEvents.Count);
    }
}
