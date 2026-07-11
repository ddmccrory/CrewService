using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace CrewService.UnitTests.SeniorityOps;

public sealed class RestrictionLabelSyncTests : IDisposable
{
    private readonly SeniorityVacancyTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private sealed record Fixture(
        ControlNumber ParentCtrlNbr,
        ControlNumber WorkAreaCtrlNbr,
        ControlNumber CraftCtrlNbr,
        ControlNumber RosterCtrlNbr,
        ControlNumber EmployeeCtrlNbr);

    private async Task<Fixture> SeedAsync(bool computedQualShouldBeSatisfied, CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();

        var parent = Parent.Create("Test Parent");
        ctx.Parents.Add(parent);
        await ctx.SaveChangesAsync(ct);

        var railroadType = GroupType.Create("Railroad", null, true);
        ctx.Set<GroupType>().Add(railroadType);
        await ctx.SaveChangesAsync(ct);

        var workArea = DynamicGroup.Create(
            railroadType.CtrlNbr,
            "Test Work Area",
            null,
            null,
            true,
            "WA",
            parentCtrlNbr: parent.CtrlNbr);
        ctx.DynamicGroups.Add(workArea);
        await ctx.SaveChangesAsync(ct);

        var craft = Craft.Create(
            null,
            workArea.CtrlNbr,
            "Trainman",
            "Trainmen",
            1,
            false,
            false,
            0,
            0,
            0,
            0,
            0,
            false,
            false,
            false,
            0);
        ctx.Crafts.Add(craft);
        await ctx.SaveChangesAsync(ct);

        var roster = Roster.Create(craft.CtrlNbr, workArea.CtrlNbr, null, "Trainman", "Trainmen", 1);
        ctx.Rosters.Add(roster);

        var empStatus = EmploymentStatus.Create(workArea.CtrlNbr, "ACT", "Active", 1, "A");
        ctx.EmploymentStatuses.Add(empStatus);
        await ctx.SaveChangesAsync(ct);

        var employee = Employee.Create(
            workArea.CtrlNbr,
            "jsmith",
            "T001",
            "000-00-0002",
            Gender.Female,
            Race.White,
            new DateTime(1991, 1, 1),
            DateTime.UtcNow,
            empStatus.CtrlNbr,
            "jsmith@example.com",
            "admin",
            "Admin User");
        ctx.Employees.Add(employee);

        var activeState = SeniorityState.Create("Active", StateType.Active, parent.CtrlNbr.Value);
        ctx.Set<SeniorityState>().Add(activeState);
        await ctx.SaveChangesAsync(ct);

        var seniority = Domain.Models.Seniority.Seniority.Create(
            roster.CtrlNbr,
            employee.CtrlNbr,
            true,
            DateTime.UtcNow.AddYears(-3),
            1,
            activeState.CtrlNbr,
            false);
        ctx.Set<Domain.Models.Seniority.Seniority>().Add(seniority);

        var foremanType = QualificationType.Create(
            parent.CtrlNbr,
            code: "YARD-FOREMAN",
            name: "Yard Foreman",
            evaluationStrategy: EvaluationStrategies.TimeFromEvent,
            craftCtrlNbr: craft.CtrlNbr,
            restrictionLabel: "Helper Only");

        if (!computedQualShouldBeSatisfied)
        {
            foremanType.AddRequirement(
                requirementKind: RequirementKinds.ActivityCount,
                threshold: 1,
                thresholdUnit: ThresholdUnits.Count,
                description: "Requires one qualifying activity.");
        }

        ctx.Set<QualificationType>().Add(foremanType);

        await ctx.SaveChangesAsync(ct);

        return new Fixture(parent.CtrlNbr, workArea.CtrlNbr, craft.CtrlNbr, roster.CtrlNbr, employee.CtrlNbr);
    }

    [Fact]
    public async Task SeniorityList_ComputedRestrictionSatisfied_DoesNotShowLabel()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedAsync(computedQualShouldBeSatisfied: true, ct);

        var items = await _host.Seniority.GetAllAsync(f.RosterCtrlNbr, railroadCtrlNbr: null, ct);

        var employeeItem = Assert.Single(items, i => i.Seniority.EmployeeCtrlNbr == f.EmployeeCtrlNbr);
        Assert.DoesNotContain("Helper Only", employeeItem.RestrictionLabels);
    }

    [Fact]
    public async Task SeniorityList_ComputedRestrictionUnsatisfied_ShowsLabel()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedAsync(computedQualShouldBeSatisfied: false, ct);

        var items = await _host.Seniority.GetAllAsync(f.RosterCtrlNbr, railroadCtrlNbr: null, ct);

        var employeeItem = Assert.Single(items, i => i.Seniority.EmployeeCtrlNbr == f.EmployeeCtrlNbr);
        Assert.Contains("Helper Only", employeeItem.RestrictionLabels);
    }

    [Fact]
    public async Task RosterBoardDetail_ComputedRestrictionSatisfied_DoesNotShowLabel()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedAsync(computedQualShouldBeSatisfied: true, ct);

        var (board, _, _, _, _) = await _host.RosterBoards.CreateRosterBoardAsync(
            f.CraftCtrlNbr.Value,
            f.RosterCtrlNbr.Value,
            "Trainman Extra Board",
            BoardType.ExtraBoard,
            RotationType.FirstInFirstOut,
            isActive: true,
            requiredPositions: 0,
            ct: ct);

        await _host.RosterBoards.AddRosterBoardPositionAsync(
            board.CtrlNbr,
            f.EmployeeCtrlNbr,
            positionOrder: 1,
            ct: ct);

        var detail = await _host.RosterBoards.GetRosterBoardDetailAsync(board.CtrlNbr, ct);

        if (detail.RestrictionLabels.TryGetValue(f.EmployeeCtrlNbr, out var labels))
            Assert.DoesNotContain("Helper Only", labels!);
    }
}
