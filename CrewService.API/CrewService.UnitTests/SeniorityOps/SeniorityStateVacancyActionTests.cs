using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CrewService.UnitTests.SeniorityOps;

/// <summary>
/// End-to-end tests for <see cref="Application.SeniorityOps.SeniorityStateVacancyConfigService"/>.
/// Proves that a seniority-state change vacates the employee's current position — for both a crew
/// incumbency and a board membership — under both configured vacancy actions, and that MoveToBoard
/// re-places the employee on the resolved target board. These run against a real orchestration UoW
/// on a shared SQLite connection so the sequential (non-nested) transaction flow is exercised.
/// </summary>
public sealed class SeniorityStateVacancyActionTests : IDisposable
{
    private readonly SeniorityVacancyTestHost _host = new();
    public void Dispose() => _host.Dispose();

    /// <summary>Common tenant graph shared by every scenario.</summary>
    private sealed record Fixture(
        ControlNumber ParentCtrlNbr,
        ControlNumber RailroadCtrlNbr,
        ControlNumber WorkAreaCtrlNbr,
        ControlNumber DepartmentCtrlNbr,
        ControlNumber CraftCtrlNbr,
        ControlNumber CraftRoleCtrlNbr,
        ControlNumber RosterCtrlNbr,
        ControlNumber EmployeeCtrlNbr,
        ControlNumber NewSeniorityStateCtrlNbr);

    /// <summary>
    /// Seeds the tenant graph the vacancy-action flow reads: a railroad-scoped work area, a craft
    /// with one role, an active roster tied to that craft/work area, an employee, and the target
    /// seniority state. Positions (crew or board) are added per-scenario by the caller.
    /// </summary>
    private async Task<Fixture> SeedBaseAsync(CancellationToken ct)
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

        return await SeedCraftAndEmployeeAsync(ctx, parent.CtrlNbr, railroad.CtrlNbr, workArea.CtrlNbr, ct);
    }

    /// <summary>
    /// Seeds the tenant graph for the "railroad group is itself the work area" topology (mirrors the
    /// PTRA seed data): a single DynamicGroup flagged <c>IsWorkArea = true</c> with a null
    /// <c>RailroadCtrlNbr</c>. The vacancy config is keyed by that group's own CtrlNbr, so the apply
    /// path must fall back to the work area's CtrlNbr when RailroadCtrlNbr is null.
    /// </summary>
    private async Task<Fixture> SeedRailroadIsWorkAreaAsync(CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();

        var parent = Parent.Create("Test Parent");
        ctx.Parents.Add(parent);
        await ctx.SaveChangesAsync(ct);

        var railroadType = GroupType.Create("Railroad", null, true);
        ctx.Set<GroupType>().Add(railroadType);
        await ctx.SaveChangesAsync(ct);

        // The railroad group IS the work area: IsWorkArea = true, RailroadCtrlNbr left null.
        var railroad = DynamicGroup.Create(
            railroadType.CtrlNbr, "PTRA", null, null, true, "PTRA",
            parentCtrlNbr: parent.CtrlNbr);
        ctx.DynamicGroups.Add(railroad);
        await ctx.SaveChangesAsync(ct);

        // Craft/roster/employee all hang off the railroad-as-work-area group.
        return await SeedCraftAndEmployeeAsync(ctx, parent.CtrlNbr, railroad.CtrlNbr, railroad.CtrlNbr, ct);
    }

    private static async Task<Fixture> SeedCraftAndEmployeeAsync(
        CrewServiceDbContext ctx, ControlNumber parentCtrlNbr, ControlNumber railroadCtrlNbr,
        ControlNumber workAreaCtrlNbr, CancellationToken ct)
    {
        var department = Department.Create(parentCtrlNbr, railroadCtrlNbr, "Transportation");
        ctx.Set<Department>().Add(department);

        var craft = Craft.Create(
            null,
            workAreaCtrlNbr,
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

        // Configure a department reassignment rule so canonical vacate paths can execute.
        // The rule is optional and points to a board type not seeded by these scenarios,
        // so test behavior remains vacancy-action driven.
        ctx.Set<DepartmentReassignmentRule>().Add(
            DepartmentReassignmentRule.Create(department.CtrlNbr, BoardType.NewHire, isRequired: false));

        await ctx.SaveChangesAsync(ct);

        var role = CraftRole.Create(craft.CtrlNbr, "ENGR", "Engineer");
        ctx.Set<CraftRole>().Add(role);

        var roster = Roster.Create(craft.CtrlNbr, workAreaCtrlNbr, null, "Engineer Roster", "Engineer Rosters", 1);
        ctx.Rosters.Add(roster);

        var empStatus = EmploymentStatus.Create(workAreaCtrlNbr, "ACT", "Active", 1, "A");
        ctx.EmploymentStatuses.Add(empStatus);
        await ctx.SaveChangesAsync(ct);

        var employee = Employee.Create(
            workAreaCtrlNbr, "jdoe", "E001", "000-00-0001", Gender.Male, Race.White,
            new DateTime(1990, 1, 1), DateTime.UtcNow, empStatus.CtrlNbr, "jdoe@example.com", "admin", "Admin User");
        ctx.Employees.Add(employee);

        var newState = SeniorityState.Create("Cut Back", StateType.CutBack, parentCtrlNbr.Value);
        ctx.Set<SeniorityState>().Add(newState);
        await ctx.SaveChangesAsync(ct);

        return new Fixture(
            parentCtrlNbr, railroadCtrlNbr, workAreaCtrlNbr, department.CtrlNbr, craft.CtrlNbr, role.CtrlNbr,
            roster.CtrlNbr, employee.CtrlNbr, newState.CtrlNbr);
    }

    /// <summary>
    /// Seeds a crew with one position for the fixture's craft role and places the employee on it
    /// through the canonical <see cref="Application.Crews.CrewsAppService.CreateCrewIncumbencyAsync"/>,
    /// which also creates the backing <see cref="PositionAssignment"/>. A BulletinRule is seeded so
    /// the crew vacate's auto-bulletin path runs exactly as it does in production.
    /// </summary>
    private async Task SeedCrewIncumbencyAsync(Fixture f, CancellationToken ct)
    {
        ControlNumber crewPositionCtrlNbr;
        await using (var ctx = _host.CreateReadContext())
        {
            var crew = Crew.Create("REGULAR", f.WorkAreaCtrlNbr, "Test Crew", departmentCtrlNbr: f.DepartmentCtrlNbr);
            ctx.Crews.Add(crew);
            var staffablePosition = StaffablePosition.Create(StaffablePositionType.Crew);
            ctx.StaffablePositions.Add(staffablePosition);
            await ctx.SaveChangesAsync(ct);

            var crewPosition = CrewPosition.Create(crew.CtrlNbr, f.CraftRoleCtrlNbr, 1, staffablePosition.CtrlNbr);
            ctx.CrewPositions.Add(crewPosition);

            var rule = BulletinRule.Create(
                f.CraftCtrlNbr, 48, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), 5, new TimeSpan(8, 0, 0), 0);
            ctx.Set<BulletinRule>().Add(rule);
            await ctx.SaveChangesAsync(ct);

            crewPositionCtrlNbr = crewPosition.CtrlNbr;
        }

        await _host.Crews.CreateCrewIncumbencyAsync(
            crewPositionCtrlNbr.Value, f.EmployeeCtrlNbr.Value, DateTime.UtcNow.AddDays(-1), null, ct);
    }

    private async Task<List<PositionAssignment>> GetAssignmentsAsync(ControlNumber employeeCtrlNbr, CancellationToken ct)
    {
        await using var uow = await _host.UowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
    }

    /// <summary>
    /// Creates a board of the given type for the fixture's craft/roster and places the employee on
    /// it through the canonical <see cref="Application.RosterBoardOps.RosterBoardAppService.AddRosterBoardPositionAsync"/>,
    /// which also creates the backing <see cref="PositionAssignment"/>. Returns the board's CtrlNbr.
    /// </summary>
    private async Task<ControlNumber> SeedBoardWithEmployeeAsync(Fixture f, BoardType boardType, CancellationToken ct)
    {
        var boardCtrlNbr = await SeedEmptyBoardAsync(f, boardType, ct);
        await _host.RosterBoards.AddRosterBoardPositionAsync(boardCtrlNbr, f.EmployeeCtrlNbr, 1, null, ct);
        return boardCtrlNbr;
    }

    /// <summary>Creates an empty (unstaffed) board of the given type for the fixture's craft/roster.</summary>
    private async Task<ControlNumber> SeedEmptyBoardAsync(Fixture f, BoardType boardType, CancellationToken ct)
        => await SeedEmptyBoardAsync(f.CraftCtrlNbr, f.RosterCtrlNbr, boardType, ct);

    private async Task<ControlNumber> SeedEmptyBoardAsync(
        ControlNumber craftCtrlNbr,
        ControlNumber rosterCtrlNbr,
        BoardType boardType,
        CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();
        var board = RosterBoard.Create(craftCtrlNbr, rosterCtrlNbr, $"{boardType} Board", boardType);
        ctx.Set<RosterBoard>().Add(board);
        await ctx.SaveChangesAsync(ct);
        return board.CtrlNbr;
    }

    private async Task<ControlNumber> SeedAdditionalRosterAsync(Fixture f, string rosterName, CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();
        var roster = Roster.Create(f.CraftCtrlNbr, f.WorkAreaCtrlNbr, null, rosterName, $"{rosterName}s", 99);
        ctx.Rosters.Add(roster);
        await ctx.SaveChangesAsync(ct);
        return roster.CtrlNbr;
    }

    private async Task<RosterBoard?> GetBoardAsync(ControlNumber boardCtrlNbr, CancellationToken ct)
    {
        await using var uow = await _host.UowFactory.CreateAsync(cancellationToken: ct);
        return await uow.RosterBoards.GetByCtrlNbrAsync(boardCtrlNbr, ct);
    }

    /// <summary>
    /// Seeds an <see cref="StateType.OffProperty"/> seniority state and returns its CtrlNbr.
    /// Deliberately seeds NO vacancy config so the test proves off-property still vacates through
    /// the canonical path even when the state is unconfigured.
    /// </summary>
    private async Task<ControlNumber> SeedOffPropertyStateAsync(Fixture f, CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();
        var offProperty = SeniorityState.Create("Terminated", StateType.OffProperty, f.ParentCtrlNbr.Value);
        ctx.Set<SeniorityState>().Add(offProperty);
        await ctx.SaveChangesAsync(ct);
        return offProperty.CtrlNbr;
    }

    /// <summary>
    /// Seeds a <see cref="Seniority"/> record for the fixture employee/roster in the supplied
/// starting state so the real <see cref="Application.SeniorityOps.SeniorityAppService.UpdateAsync"/>
    /// entry point has a record to transition. Returns its CtrlNbr.
    /// </summary>
    private async Task<ControlNumber> SeedSeniorityRecordAsync(Fixture f, ControlNumber startingStateCtrlNbr, CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();
        var seniority = Seniority.Create(
            f.RosterCtrlNbr, f.EmployeeCtrlNbr, true, DateTime.UtcNow.AddYears(-1), 1, startingStateCtrlNbr, true);
        ctx.Set<Seniority>().Add(seniority);
        await ctx.SaveChangesAsync(ct);
        return seniority.CtrlNbr;
    }

    /// <summary>Resolves the crew's backing staffable position CtrlNbr for the fixture (single seeded crew).</summary>
    private async Task<ControlNumber> GetCrewStaffablePositionCtrlNbrAsync(CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();
        var crewPosition = await ctx.Set<CrewPosition>().AsNoTracking().FirstAsync(ct);
        return crewPosition.StaffablePositionCtrlNbr;
    }

    /// <summary>Counts bulletins opened for the given crew staffable position.</summary>
    private async Task<int> CountCrewBulletinsAsync(ControlNumber staffablePositionCtrlNbr, CancellationToken ct)
    {
        await using var uow = await _host.UowFactory.CreateAsync(cancellationToken: ct);
        var vacancies = await uow.PositionVacancies.GetByTargetAsync(
            StaffablePositionType.Crew, staffablePositionCtrlNbr);
        var count = 0;
        foreach (var vacancy in vacancies)
        {
            var bulletin = await uow.Bulletins.GetByVacancyAsync(vacancy.CtrlNbr);
            if (bulletin is not null)
                count++;
        }
        return count;
    }

    /// <summary>
    /// End-to-end regression for the production bug where moving an employee off property (seniority
    /// state Terminated) created NO bulletin for the vacated position because no per-state vacancy
    /// config existed, so the crew position was never vacated and never bulletined. Off-property is
    /// terminal and must ALWAYS route through the same canonical vacate/bulletin path as every other
    /// vacate. This drives the real <c>SeniorityAppService.UpdateAsync</c> UI entry point with NO
    /// vacancy config seeded, then runs the same durable repost sweep the BulletinProcessingWorker
    /// runs in production, and asserts an actual Bulletin row now exists for the freed crew position.
    /// </summary>
    [Fact]
    public async Task OffProperty_NoVacancyConfig_VacatesAndBulletinsCrewPosition_EndToEnd()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        await SeedCrewIncumbencyAsync(f, ct);
        var offPropertyStateCtrlNbr = await SeedOffPropertyStateAsync(f, ct);
        var seniorityCtrlNbr = await SeedSeniorityRecordAsync(f, f.NewSeniorityStateCtrlNbr, ct);
        var crewStaffablePositionCtrlNbr = await GetCrewStaffablePositionCtrlNbrAsync(ct);

        // Sanity: employee holds the crew position, NO vacancy config exists, and no bulletin yet.
        Assert.Single(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));
        Assert.Equal(0, await CountCrewBulletinsAsync(crewStaffablePositionCtrlNbr, ct));

        // Drive the real UI entry point: transition the employee's seniority to off-property.
        await _host.Seniority.UpdateAsync(
            seniorityCtrlNbr, lastActiveRoster: true, DateTime.UtcNow, rank: 1,
            offPropertyStateCtrlNbr, canTrain: true, ct);

        // The crew position is freed by the canonical vacate path.
        Assert.Empty(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));

        // Run the same durable repost sweep the BulletinProcessingWorker runs in production. This is
        // the deterministic counterpart of the post-commit reactor and calls the identical
        // RepostVacatedPositionAsync -> OpenVacancyAsync path, so it proves the vacate bulletins.
        await _host.Repost.ReconcileUnbulletinedVacantPositionsAsync(ct);

        // End-to-end proof: a bulletin now exists for the vacated crew position — the same outcome
        // every other vacate produces. Before the fix, no position was vacated so none was possible.
        Assert.Equal(1, await CountCrewBulletinsAsync(crewStaffablePositionCtrlNbr, ct));
    }

    [Fact]
    public async Task VacateAndBulletin_CrewSource_RemovesCrewPositionAssignment()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        await SeedCrewIncumbencyAsync(f, ct);

        await _host.VacancyConfig.UpsertAsync(
            f.ParentCtrlNbr, f.RailroadCtrlNbr, f.NewSeniorityStateCtrlNbr, VacancyAction.VacateAndBulletin, ct: ct);

        // Sanity: the employee holds the crew position before the state change.
        Assert.Single(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));

        await _host.VacancyConfig.ApplyVacancyActionAsync(
            f.EmployeeCtrlNbr, f.NewSeniorityStateCtrlNbr, f.RosterCtrlNbr, ct);

        // The crew incumbency is ended and its PositionAssignment removed.
        Assert.Empty(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));
    }

    /// <summary>
    /// Regression for the production bug where nothing was vacated when the railroad group is
    /// itself the work area (PTRA topology: IsWorkArea = true, RailroadCtrlNbr = null). The apply
    /// path must resolve the railroad as <c>workArea.RailroadCtrlNbr ?? workArea.CtrlNbr</c> so the
    /// config — keyed by the railroad group's own CtrlNbr — is found and the vacate runs.
    /// </summary>
    [Fact]
    public async Task VacateAndBulletin_RailroadIsWorkArea_StillVacatesCrewPosition()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedRailroadIsWorkAreaAsync(ct);
        await SeedCrewIncumbencyAsync(f, ct);

        // Config keyed by the railroad-as-work-area group's own CtrlNbr (RailroadCtrlNbr == CtrlNbr).
        await _host.VacancyConfig.UpsertAsync(
            f.ParentCtrlNbr, f.RailroadCtrlNbr, f.NewSeniorityStateCtrlNbr, VacancyAction.VacateAndBulletin, ct: ct);

        // Sanity: the employee holds the crew position before the state change.
        Assert.Single(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));

        await _host.VacancyConfig.ApplyVacancyActionAsync(
            f.EmployeeCtrlNbr, f.NewSeniorityStateCtrlNbr, f.RosterCtrlNbr, ct);

        // Before the fix this stayed occupied because the railroad resolved to null and the
        // action was skipped. Now the crew position is vacated.
        Assert.Empty(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));
    }

    [Fact]
    public async Task VacateAndBulletin_BoardSource_RemovesBoardPositionAssignment()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        var boardCtrlNbr = await SeedBoardWithEmployeeAsync(f, BoardType.ExtraBoard, ct);

        await _host.VacancyConfig.UpsertAsync(
            f.ParentCtrlNbr, f.RailroadCtrlNbr, f.NewSeniorityStateCtrlNbr, VacancyAction.VacateAndBulletin, ct: ct);

        // Sanity: the employee holds the board position before the state change.
        Assert.Single(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));

        await _host.VacancyConfig.ApplyVacancyActionAsync(
            f.EmployeeCtrlNbr, f.NewSeniorityStateCtrlNbr, f.RosterCtrlNbr, ct);

        // The board position is removed along with its PositionAssignment.
        Assert.Empty(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));
        var board = await GetBoardAsync(boardCtrlNbr, ct);
        Assert.NotNull(board);
        Assert.DoesNotContain(board!.Positions, p => p.EmployeeCtrlNbr == f.EmployeeCtrlNbr);
    }

    [Fact]
    public async Task MoveToBoard_CrewSource_VacatesCrewAndPlacesOnTargetBoard()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        await SeedCrewIncumbencyAsync(f, ct);
        // Target board employees are moved to (a no-rotation Hangout avoids a required-positions strategy).
        var targetBoardCtrlNbr = await SeedEmptyBoardAsync(f, BoardType.Hangout, ct);

        await _host.VacancyConfig.UpsertAsync(
            f.ParentCtrlNbr, f.RailroadCtrlNbr, f.NewSeniorityStateCtrlNbr, VacancyAction.MoveToBoard,
            targetBoardType: BoardType.Hangout, ct: ct);

        // Sanity: the employee holds the crew position before the state change.
        Assert.Single(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));

        await _host.VacancyConfig.ApplyVacancyActionAsync(
            f.EmployeeCtrlNbr, f.NewSeniorityStateCtrlNbr, f.RosterCtrlNbr, ct);

        // The crew source is vacated and the employee now holds exactly one board assignment.
        var assignments = await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct);
        Assert.Single(assignments);
        Assert.Equal(PositionAssignmentType.Board, assignments[0].AssignmentType);

        var targetBoard = await GetBoardAsync(targetBoardCtrlNbr, ct);
        Assert.NotNull(targetBoard);
        Assert.Contains(targetBoard!.Positions, p => p.EmployeeCtrlNbr == f.EmployeeCtrlNbr);
    }

    [Fact]
    public async Task MoveToBoard_BoardSource_VacatesSourceBoardAndPlacesOnTargetBoard()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        var sourceBoardCtrlNbr = await SeedBoardWithEmployeeAsync(f, BoardType.ExtraBoard, ct);
        var targetBoardCtrlNbr = await SeedEmptyBoardAsync(f, BoardType.Hangout, ct);

        await _host.VacancyConfig.UpsertAsync(
            f.ParentCtrlNbr, f.RailroadCtrlNbr, f.NewSeniorityStateCtrlNbr, VacancyAction.MoveToBoard,
            targetBoardType: BoardType.Hangout, ct: ct);

        // Sanity: the employee holds the source board position before the state change.
        Assert.Single(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));

        await _host.VacancyConfig.ApplyVacancyActionAsync(
            f.EmployeeCtrlNbr, f.NewSeniorityStateCtrlNbr, f.RosterCtrlNbr, ct);

        // The source board slot is vacated and the employee holds exactly one (target) board assignment.
        Assert.Single(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));

        var sourceBoard = await GetBoardAsync(sourceBoardCtrlNbr, ct);
        Assert.NotNull(sourceBoard);
        Assert.DoesNotContain(sourceBoard!.Positions, p => p.EmployeeCtrlNbr == f.EmployeeCtrlNbr);

        var targetBoard = await GetBoardAsync(targetBoardCtrlNbr, ct);
        Assert.NotNull(targetBoard);
        Assert.Contains(targetBoard!.Positions, p => p.EmployeeCtrlNbr == f.EmployeeCtrlNbr);
    }

    [Fact]
    public async Task MoveToBoard_AlreadyOnTargetBoardType_LeavesPositionUnchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        var targetBoardCtrlNbr = await SeedBoardWithEmployeeAsync(f, BoardType.ExtendedAbsence, ct);

        await _host.VacancyConfig.UpsertAsync(
            f.ParentCtrlNbr, f.RailroadCtrlNbr, f.NewSeniorityStateCtrlNbr, VacancyAction.MoveToBoard,
            targetBoardType: BoardType.ExtendedAbsence, ct: ct);

        var beforeAssignments = await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct);
        var beforeAssignment = Assert.Single(beforeAssignments);
        Assert.Equal(PositionAssignmentType.Board, beforeAssignment.AssignmentType);

        await _host.VacancyConfig.ApplyVacancyActionAsync(
            f.EmployeeCtrlNbr, f.NewSeniorityStateCtrlNbr, f.RosterCtrlNbr, ct);

        var afterAssignments = await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct);
        var afterAssignment = Assert.Single(afterAssignments);
        Assert.Equal(beforeAssignment.StaffablePositionCtrlNbr, afterAssignment.StaffablePositionCtrlNbr);

        var targetBoard = await GetBoardAsync(targetBoardCtrlNbr, ct);
        Assert.NotNull(targetBoard);
        Assert.Contains(targetBoard!.Positions, p => p.EmployeeCtrlNbr == f.EmployeeCtrlNbr);
    }

    [Fact]
    public async Task NonOffProperty_NoConfig_DefaultsToLeaveOnCurrentPosition()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        await SeedCrewIncumbencyAsync(f, ct);

        // No per-state config and no state-type default are seeded.
        await _host.VacancyConfig.ApplyVacancyActionAsync(
            f.EmployeeCtrlNbr, f.NewSeniorityStateCtrlNbr, f.RosterCtrlNbr, ct);

        // Non-OffProperty defaults to LeaveOnCurrentPosition.
        Assert.Single(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));
    }

    [Fact]
    public async Task StateTypeDefault_OffProperty_RejectsLeaveOnCurrentPosition()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _host.VacancyConfig.UpsertStateTypeDefaultAsync(
                f.ParentCtrlNbr,
                f.RailroadCtrlNbr,
                StateType.OffProperty,
                VacancyAction.LeaveOnCurrentPosition,
                ct));
    }

    [Fact]
    public async Task MoveToBoard_NoRosterMatchingBoard_LeavesCurrentPosition()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        await SeedCrewIncumbencyAsync(f, ct);

        var otherRosterCtrlNbr = await SeedAdditionalRosterAsync(f, "Engineer Trainees", ct);
        _ = await SeedEmptyBoardAsync(f.CraftCtrlNbr, otherRosterCtrlNbr, BoardType.Hangout, ct);

        await _host.VacancyConfig.UpsertAsync(
            f.ParentCtrlNbr,
            f.RailroadCtrlNbr,
            f.NewSeniorityStateCtrlNbr,
            VacancyAction.MoveToBoard,
            targetBoardType: BoardType.Hangout,
            ct: ct);

        await _host.VacancyConfig.ApplyVacancyActionAsync(
            f.EmployeeCtrlNbr, f.NewSeniorityStateCtrlNbr, f.RosterCtrlNbr, ct);

        // No matching board exists on the employee roster, so no cross-roster move occurs.
        var assignments = await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct);
        Assert.Single(assignments);
        Assert.Equal(PositionAssignmentType.Direct, assignments[0].AssignmentType);
    }
}
