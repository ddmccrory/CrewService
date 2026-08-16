using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;
using CrewService.Application.Workflows;
using CrewService.Persistance.Data;
using CrewService.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace CrewService.UnitTests.RosterBoardOps;

/// <summary>
/// Regression tests for the Static required-positions strategy on ExtraBoard vacancy handling.
/// The Static formula is a manual, no-op strategy: an admin sets the board's RequiredPositions
/// explicitly and it must never be recalculated. A prior defect had
/// <c>RecalculateRequiredPositionsAsync</c> unconditionally invoke <c>IRequiredPositionsFormula.Calculate</c>,
/// which for Static always returns 0 — wiping the manual value on the very vacate that should have
/// bulletined, so no bulletin was ever created. These run against a real orchestration UoW on a
/// shared SQLite connection so the recalculation + auto-repost flow is exercised as in production.
/// </summary>
public sealed class StaticRequiredPositionsVacateTests : IDisposable
{
    private readonly SeniorityVacancyTestHost _host = new();
    public void Dispose() => _host.Dispose();

    private sealed record Fixture(
        ControlNumber ParentCtrlNbr,
        ControlNumber RailroadCtrlNbr,
        ControlNumber WorkAreaCtrlNbr,
        ControlNumber DepartmentCtrlNbr,
        ControlNumber CraftCtrlNbr,
        ControlNumber RosterCtrlNbr,
        ControlNumber StaticStrategyCtrlNbr,
        ControlNumber Employee1CtrlNbr,
        ControlNumber Employee2CtrlNbr);

    /// <summary>
    /// Seeds the tenant graph plus the system-wide Static strategy assigned to the fixture craft,
    /// and a BulletinRule so the vacate's auto-repost path posts a bulletin exactly as in production.
    /// </summary>
    private async Task<Fixture> SeedAsync(CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();

        var parent = Parent.Create("Test Parent");
        ctx.Parents.Add(parent);
        await ctx.SaveChangesAsync(ct);

        var railroadType = GroupType.Create("Railroad", null, true);
        ctx.Set<GroupType>().Add(railroadType);
        await ctx.SaveChangesAsync(ct);

        var railroad = DynamicGroup.Create(
            railroadType.CtrlNbr, "Test Railroad", null, null, false, "RR",
            parentCtrlNbr: parent.CtrlNbr);
        ctx.DynamicGroups.Add(railroad);
        await ctx.SaveChangesAsync(ct);

        var workArea = DynamicGroup.Create(
            railroadType.CtrlNbr, "Test Work Area", null, null, true, "WA",
            parentCtrlNbr: parent.CtrlNbr,
            railroadCtrlNbr: railroad.CtrlNbr);
        ctx.DynamicGroups.Add(workArea);

        var department = Department.Create(parent.CtrlNbr, railroad.CtrlNbr, "Transportation");
        ctx.Set<Department>().Add(department);

        var craft = Craft.Create(
            null,
            railroad.CtrlNbr,
            "Engineer",
            "Engineers",
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
            0,
            department.CtrlNbr);
        ctx.Crafts.Add(craft);

        // Satisfy department reassignment prerequisites for board removal flows while keeping
        // behavior static-strategy focused by making the rule optional and unresolved.
        ctx.Set<DepartmentReassignmentRule>().Add(
            DepartmentReassignmentRule.Create(department.CtrlNbr, BoardType.NewHire, isRequired: false));
        await ctx.SaveChangesAsync(ct);

        var roster = Roster.Create(craft.CtrlNbr, workArea.CtrlNbr, null, "Engineer Roster", "Engineer Rosters", 1);
        ctx.Rosters.Add(roster);

        var empStatus = EmploymentStatus.Create(workArea.CtrlNbr, "ACT", "Active", 1, "A");
        ctx.EmploymentStatuses.Add(empStatus);
        await ctx.SaveChangesAsync(ct);

        // System-wide Static strategy (Calculate is a documented no-op → always 0), assigned to the craft.
        var staticStrategy = RequiredPositionsStrategy.Create(
            "STATIC", "Static", "Fixed required-position count set manually per board.", "Static", "{\"count\":1}");
        ctx.Set<RequiredPositionsStrategy>().Add(staticStrategy);
        await ctx.SaveChangesAsync(ct);

        var craftAssignment = CraftRequiredPositionsStrategy.Create(craft.CtrlNbr, staticStrategy.CtrlNbr);
        ctx.Set<CraftRequiredPositionsStrategy>().Add(craftAssignment);

        var rule = BulletinRule.Create(
            craft.CtrlNbr, 48, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), 5, new TimeSpan(8, 0, 0), 0);
        ctx.Set<BulletinRule>().Add(rule);

        var employee1 = Employee.Create(
            workArea.CtrlNbr, "jdoe", "E001", "000-00-0001", Gender.Male, Race.White,
            new DateTime(1990, 1, 1), DateTime.UtcNow, empStatus.CtrlNbr, "jdoe@example.com", "admin", "Admin User");
        ctx.Employees.Add(employee1);

        var employee2 = Employee.Create(
            workArea.CtrlNbr, "asmith", "E002", "000-00-0002", Gender.Female, Race.White,
            new DateTime(1991, 2, 2), DateTime.UtcNow, empStatus.CtrlNbr, "asmith@example.com", "admin", "Admin User");
        ctx.Employees.Add(employee2);
        await ctx.SaveChangesAsync(ct);

        await SeedPositionVacatedWorkflowAsync(ctx, railroad.CtrlNbr, ct);

        return new Fixture(
            parent.CtrlNbr, railroad.CtrlNbr, workArea.CtrlNbr, department.CtrlNbr, craft.CtrlNbr, roster.CtrlNbr,
            staticStrategy.CtrlNbr, employee1.CtrlNbr, employee2.CtrlNbr);
    }

    private static async Task SeedPositionVacatedWorkflowAsync(
        CrewServiceDbContext ctx,
        ControlNumber railroadCtrlNbr,
        CancellationToken ct)
    {
        var triggerTypeCtrlNbr = await ctx.Set<WorkflowTriggerType>()
            .Where(t => t.Code == WorkflowTriggerTypeCodes.PositionVacated)
            .Select(t => t.CtrlNbr)
            .FirstAsync(ct);

        var createBulletinEffectTypeCtrlNbr = await ctx.Set<WorkflowEffectType>()
            .Where(t => t.Code == WorkflowEffectTypeCodes.CreateBulletin)
            .Select(t => t.CtrlNbr)
            .FirstAsync(ct);

        var existingTemplate = await ctx.Set<WorkflowTemplate>()
            .FirstOrDefaultAsync(
                t => t.RailroadCtrlNbr == railroadCtrlNbr
                     && t.TriggerTypeCtrlNbr == triggerTypeCtrlNbr
                     && t.Name == "Position Vacated",
                ct);
        if (existingTemplate is not null)
            return;

        var template = WorkflowTemplate.Create(
            railroadCtrlNbr,
            name: "Position Vacated",
            triggerTypeCtrlNbr,
            isEnabled: true);
        ctx.Set<WorkflowTemplate>().Add(template);
        await ctx.SaveChangesAsync(ct);

        var definition = new WorkflowDefinition(
            TriggerTypeCtrlNbr: triggerTypeCtrlNbr,
            TriggerConditionGroupOperator: "ALL",
            TriggerConditions: [],
            Steps:
            [
                new WorkflowStepDefinition(
                    CtrlNbr: ControlNumber.Create(),
                    Order: 1,
                    Name: "Create Bulletin",
                    IsEnabled: true,
                    FailurePolicy: WorkflowFailurePolicies.StopWorkflow,
                    ConditionGroupOperator: "ALL",
                    Conditions: [],
                    Effects:
                    [
                        new WorkflowEffectDefinition(
                            CtrlNbr: ControlNumber.Create(),
                            Order: 1,
                            IsEnabled: true,
                            EffectTypeCtrlNbr: createBulletinEffectTypeCtrlNbr,
                            Options: new Dictionary<string, string>())
                    ])
            ]);

        var version = WorkflowVersion.Create(
            workflowTemplateCtrlNbr: template.CtrlNbr,
            versionNumber: 1,
            definitionJson: JsonSerializer.Serialize(definition),
            notes: "seed",
            status: WorkflowVersionStatus.Published);

        ctx.Set<WorkflowVersion>().Add(version);
        await ctx.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Creates an ExtraBoard with a manual Static RequiredPositions of <paramref name="requiredPositions"/>
    /// and places the two fixture employees on it via the canonical add-position service path (which also
    /// creates the backing PositionAssignments). Returns the board CtrlNbr.
    /// </summary>
    private async Task<ControlNumber> SeedBoardWithTwoMembersAsync(Fixture f, int requiredPositions, CancellationToken ct)
    {
        ControlNumber boardCtrlNbr;
        await using (var ctx = _host.CreateReadContext())
        {
            var board = RosterBoard.Create(
                f.CraftCtrlNbr, f.RosterCtrlNbr, "Engineer Extra Board",
                BoardType.ExtraBoard, RotationType.StandardRotation, isActive: true,
                requiredPositions: requiredPositions);
            ctx.Set<RosterBoard>().Add(board);
            await ctx.SaveChangesAsync(ct);
            boardCtrlNbr = board.CtrlNbr;
        }

        await _host.RosterBoards.AddRosterBoardPositionAsync(boardCtrlNbr, f.Employee1CtrlNbr, 1, null, ct);
        await _host.RosterBoards.AddRosterBoardPositionAsync(boardCtrlNbr, f.Employee2CtrlNbr, 2, null, ct);
        return boardCtrlNbr;
    }

    private async Task<RosterBoard> GetBoardAsync(ControlNumber boardCtrlNbr, CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();
        return await ctx.Set<RosterBoard>()
            .Include(b => b.Positions)
            .SingleAsync(b => b.CtrlNbr == boardCtrlNbr, ct);
    }

    private async Task<List<PositionVacancy>> GetVacanciesAsync(ControlNumber workAreaCtrlNbr, CancellationToken ct)
    {
        await using var uow = await _host.UowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PositionVacancies.GetByWorkAreaAsync(workAreaCtrlNbr);
    }

    private async Task SeedShiftBoardSlotReferenceAsync(
        ControlNumber workAreaCtrlNbr,
        ControlNumber departmentCtrlNbr,
        ControlNumber boardCtrlNbr,
        ControlNumber boardPositionCtrlNbr,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();

        var workInstance = WorkInstance.Create(
            assignmentGroupCtrlNbr: null,
            workAreaGroupCtrlNbr: workAreaCtrlNbr,
            startUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            endUtc: new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            callTimeUtc: null);
        var shiftDefinition = ShiftDefinition.Create(
            workAreaGroupCtrlNbr: workAreaCtrlNbr,
            shiftCode: "D1",
            displayName: "Day 1",
            displayOrder: 1,
            isActive: true);
        var shiftInstance = ShiftInstance.Create(
            workInstanceCtrlNbr: workInstance.CtrlNbr,
            shiftDefinitionCtrlNbr: shiftDefinition.CtrlNbr,
            shiftCode: "D1",
            shiftDisplayName: "Day 1",
            departmentCtrlNbr: departmentCtrlNbr,
            departmentName: "Transportation");

        shiftInstance.AddBoardSlot(
            rosterBoardCtrlNbr: boardCtrlNbr,
            rosterBoardPositionCtrlNbr: boardPositionCtrlNbr,
            employeeCtrlNbr: employeeCtrlNbr,
            boardOrder: 1,
            callSequence: 1,
            boardName: "Engineer Extra Board",
            employeeName: "Employee One");

        ctx.Set<WorkInstance>().Add(workInstance);
        ctx.Set<ShiftDefinition>().Add(shiftDefinition);
        await ctx.SaveChangesAsync(ct);

        ctx.Set<ShiftInstance>().Add(shiftInstance);
        await ctx.SaveChangesAsync(ct);
    }

    // ── Tests ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveBoardPosition_StaticStrategy_DoesNotResetRequiredPositions()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedAsync(ct);
        var boardCtrlNbr = await SeedBoardWithTwoMembersAsync(f, requiredPositions: 20, ct);

        var board = await GetBoardAsync(boardCtrlNbr, ct);
        var positionToRemove = board.Positions.Single(p => p.EmployeeCtrlNbr == f.Employee1CtrlNbr);

        await _host.RosterBoards.RemoveRosterBoardPositionAsync(positionToRemove.CtrlNbr, ct);

        // The manual Static value must survive the vacate — a prior defect recalculated it to 0.
        var afterBoard = await GetBoardAsync(boardCtrlNbr, ct);
        Assert.Equal(20, afterBoard.RequiredPositions);
    }

    [Fact]
    public async Task RemoveBoardPosition_StaticStrategyUnderstaffed_OpensBulletinVacancy()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedAsync(ct);
        var boardCtrlNbr = await SeedBoardWithTwoMembersAsync(f, requiredPositions: 20, ct);

        var board = await GetBoardAsync(boardCtrlNbr, ct);
        var positionToRemove = board.Positions.Single(p => p.EmployeeCtrlNbr == f.Employee1CtrlNbr);

        await _host.RosterBoards.RemoveRosterBoardPositionAsync(positionToRemove.CtrlNbr, ct);

        // Occupancy (1) is below RequiredPositions (20), so the vacated slot must be auto-bulletined.
        var vacancies = await GetVacanciesAsync(f.WorkAreaCtrlNbr, ct);
        var vacancy = Assert.Single(vacancies);
        Assert.Equal("BOARD_UNDERSTAFFED", vacancy.VacancyReasonCode);
        Assert.Equal(StaffablePositionType.Board, vacancy.TargetType);
        Assert.Equal(positionToRemove.StaffablePositionCtrlNbr, vacancy.TargetCtrlNbr);
    }

    [Fact]
    public async Task RemoveBoardPosition_WithBoardSlotReference_SoftDeletesPositionWithoutFkViolation()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedAsync(ct);
        var boardCtrlNbr = await SeedBoardWithTwoMembersAsync(f, requiredPositions: 20, ct);

        var board = await GetBoardAsync(boardCtrlNbr, ct);
        var positionToRemove = board.Positions.Single(p => p.EmployeeCtrlNbr == f.Employee1CtrlNbr);

        await SeedShiftBoardSlotReferenceAsync(
            f.WorkAreaCtrlNbr,
            f.DepartmentCtrlNbr,
            boardCtrlNbr,
            positionToRemove.CtrlNbr,
            f.Employee1CtrlNbr,
            ct);

        await _host.RosterBoards.RemoveRosterBoardPositionAsync(positionToRemove.CtrlNbr, ct);

        await using var ctx = _host.CreateReadContext();

        var removedPosition = await ctx.Set<RosterBoardPosition>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(p => p.CtrlNbr == positionToRemove.CtrlNbr, ct);

        Assert.NotNull(removedPosition);
        Assert.True(removedPosition!.IsDeleted);

        var activePosition = await ctx.Set<RosterBoardPosition>()
            .SingleOrDefaultAsync(p => p.CtrlNbr == positionToRemove.CtrlNbr, ct);
        Assert.Null(activePosition);

        var boardSlot = await ctx.Set<BoardSlotInstance>()
            .SingleOrDefaultAsync(s => s.RosterBoardPositionCtrlNbr == positionToRemove.CtrlNbr, ct);
        Assert.NotNull(boardSlot);
    }
}
