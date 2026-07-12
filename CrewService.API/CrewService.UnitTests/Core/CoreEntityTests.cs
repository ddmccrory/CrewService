using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using System.Reflection;
using CrewService.Application.SeniorityOps;
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

public class CraftProvisioningOptionsTests
{
    [Fact]
    public void Defaults_EnableAllProvisioningFlags()
    {
        var options = InvokeProvisioningCreate();

        Assert.True(GetBool(options, "CreateStandardRoster"));
        Assert.True(GetBool(options, "CreateExtraBoard"));
        Assert.True(GetBool(options, "CreateHangoutBoard"));
        Assert.True(GetBool(options, "CreateExtendedAbsenceBoard"));
        Assert.True(GetBool(options, "CreateTrainingRoster"));
        Assert.True(GetBool(options, "CreateNewHiresBoard"));
    }

    [Fact]
    public void StandardBoardWithoutStandardRoster_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            InvokeProvisioningCreate(createStandardRoster: false, createExtraBoard: true));

        Assert.Contains("Standard roster", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NewHiresBoardWithoutTrainingRoster_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            InvokeProvisioningCreate(createTrainingRoster: false, createNewHiresBoard: true));

        Assert.Contains("Training roster", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BlankEnabledBoardName_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            InvokeProvisioningCreate(createStandardRoster: true, createExtraBoard: true, extraBoardName: "   "));

        Assert.Contains("extraBoardName", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static object InvokeProvisioningCreate(
        bool? createStandardRoster = null,
        bool? createExtraBoard = null,
        bool? createHangoutBoard = null,
        bool? createExtendedAbsenceBoard = null,
        bool? createTrainingRoster = null,
        bool? createNewHiresBoard = null,
        string? standardRosterName = null,
        string? standardRosterPluralName = null,
        string? trainingRosterName = null,
        string? trainingRosterPluralName = null,
        string? extraBoardName = null,
        string? hangoutBoardName = null,
        string? extendedAbsenceBoardName = null,
        string? newHiresBoardName = null)
    {
        var craft = Craft.Create(
            parentCtrlNbr: ControlNumber.Create(1),
            dynamicGroupCtrlNbr: ControlNumber.Create(2),
            craftName: "Engineer",
            craftPluralName: "Engineers",
            craftNumber: 1,
            autoMarkUp: false,
            approveAllMarkOffs: false,
            markOffHours: 0,
            markUpHours: 0,
            requiredRestHours: 0,
            maximumVacationDayTime: 0,
            unpaidMealPeriodMinutes: 0,
            hoursofService: false,
            processPayroll: false,
            showNotifications: false,
            vacationAssignmentType: 0);

        var nestedType = typeof(CraftAppService).GetNestedType("CraftProvisioningOptions", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CraftProvisioningOptions nested type not found.");

        var createMethod = nestedType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("CraftProvisioningOptions.Create method not found.");

        try
        {
            return createMethod.Invoke(null,
            [
                craft,
                createStandardRoster,
                createExtraBoard,
                createHangoutBoard,
                createExtendedAbsenceBoard,
                createTrainingRoster,
                createNewHiresBoard,
                standardRosterName,
                standardRosterPluralName,
                trainingRosterName,
                trainingRosterPluralName,
                extraBoardName,
                hangoutBoardName,
                extendedAbsenceBoardName,
                newHiresBoardName
            ])!;
        }
        catch (TargetInvocationException tie) when (tie.InnerException is not null)
        {
            throw tie.InnerException;
        }
    }

    private static bool GetBool(object instance, string propertyName)
    {
        var value = instance.GetType().GetProperty(propertyName)?.GetValue(instance);
        return value is bool b && b;
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
        var craft = Craft.Create(10, 1, "Engineer", "Engineers", 1,
            true, false, 8, 8, 10, 24, 30, true, true, true, 0);

        Assert.Equal(10, craft.ParentCtrlNbr);
        Assert.Equal(1, craft.DynamicGroupCtrlNbr!.Value);
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
        var roster = Roster.Create(1, 10, null, "Main Roster", "Main Rosters", 1);

        Assert.Equal("Main Roster", roster.RosterName);
        Assert.Equal(1, roster.RosterNumber);
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
        var state = SeniorityState.Create("Active Duty", StateType.Active, 1234567890);

        Assert.Equal("Active Duty", state.StateDescription);
        Assert.Equal(StateType.Active, state.StateType);
        Assert.True(state.DomainEvents.Count > 0);
    }

    [Fact]
    public void Update_ChangesFields_RaisesEvent()
    {
        var state = SeniorityState.Create("Active Duty", StateType.Active, 1234567890);
        var eventsBefore = state.DomainEvents.Count;

        state.Update("Cut Back", StateType.CutBack);

        Assert.Equal("Cut Back", state.StateDescription);
        Assert.Equal(StateType.CutBack, state.StateType);
        Assert.True(state.DomainEvents.Count > eventsBefore);
    }

    [Fact]
    public void Update_NoChanges_DoesNotRaiseEvent()
    {
        var state = SeniorityState.Create("Active Duty", StateType.Active, 1234567890);
        var eventsBefore = state.DomainEvents.Count;

        state.Update("Active Duty", StateType.Active);

        Assert.Equal(eventsBefore, state.DomainEvents.Count);
    }
}
