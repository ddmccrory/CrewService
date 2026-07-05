using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CrewService.UnitTests.Bulletins;

/// <summary>
/// End-to-end tests for the bulletin award/force-assign OUTGOING-vacate wiring in
/// <see cref="Application.Bulletins.BulletinsService"/>. When a winner already holds a position
/// (a crew seat or an extra-board slot), <c>FillBulletinAsync</c> must vacate and remove that prior
/// <see cref="PositionAssignment"/> (ending any backing crew incumbency) and place the winner on the
/// bulletin's target position. These run against a real orchestration UoW on a shared SQLite
/// connection so the sequential transaction flow is exercised exactly as in production.
/// </summary>
public sealed class BulletinAwardOutgoingVacateTests : IDisposable
{
    private readonly SeniorityVacancyTestHost _host = new();
    public void Dispose() => _host.Dispose();

    /// <summary>Common tenant graph shared by every scenario.</summary>
    private sealed record Fixture(
        ControlNumber ParentCtrlNbr,
        ControlNumber RailroadCtrlNbr,
        ControlNumber WorkAreaCtrlNbr,
        ControlNumber CraftCtrlNbr,
        ControlNumber CraftRoleCtrlNbr,
        ControlNumber RosterCtrlNbr,
        ControlNumber EmployeeCtrlNbr);

    /// <summary>
    /// Seeds the tenant graph the bulletin flow reads: a railroad-scoped work area, a craft with one
    /// role, an active roster tied to that craft/work area, and an employee (the bulletin winner).
    /// Positions are added per-scenario by the caller.
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

        var craft = Craft.Create(null, workArea.CtrlNbr, "Engineer", "Engineers", 1, false, false, 0, 0, 0, 0, 0, false, false, false, 0);
        ctx.Crafts.Add(craft);
        await ctx.SaveChangesAsync(ct);

        var role = CraftRole.Create(craft.CtrlNbr, "ENGR", "Engineer");
        ctx.Set<CraftRole>().Add(role);

        var roster = Roster.Create(craft.CtrlNbr, workArea.CtrlNbr, null, "Engineer Roster", "Engineer Rosters", 1);
        ctx.Rosters.Add(roster);

        var empStatus = EmploymentStatus.Create(workArea.CtrlNbr, "ACT", "Active", 1, "A");
        ctx.EmploymentStatuses.Add(empStatus);
        await ctx.SaveChangesAsync(ct);

        var employee = Employee.Create(
            workArea.CtrlNbr, "jdoe", "E001", "000-00-0001", Gender.Male, Race.White,
            new DateTime(1990, 1, 1), DateTime.UtcNow, empStatus.CtrlNbr, "jdoe@example.com", "admin", "Admin User");
        ctx.Employees.Add(employee);
        await ctx.SaveChangesAsync(ct);

        return new Fixture(
            parent.CtrlNbr, railroad.CtrlNbr, workArea.CtrlNbr, craft.CtrlNbr, role.CtrlNbr,
            roster.CtrlNbr, employee.CtrlNbr);
    }

    /// <summary>
    /// Places the winner on an OUTGOING crew seat via the canonical
    /// <see cref="Application.Crews.CrewsAppService.CreateCrewIncumbencyAsync"/> (which also creates
    /// the backing <see cref="PositionAssignment"/>). A BulletinRule is seeded so the vacated seat's
    /// auto-repost path runs exactly as in production. Returns the incumbency's CtrlNbr.
    /// </summary>
    private async Task<ControlNumber> SeedOutgoingCrewSeatAsync(Fixture f, CancellationToken ct)
    {
        ControlNumber crewPositionCtrlNbr;
        await using (var ctx = _host.CreateReadContext())
        {
            var crew = Crew.Create("REGULAR", f.WorkAreaCtrlNbr, "Outgoing Crew");
            ctx.Crews.Add(crew);
            var staffablePosition = StaffablePosition.Create(StaffablePositionType.Crew);
            ctx.StaffablePositions.Add(staffablePosition);
            await ctx.SaveChangesAsync(ct);

            var crewPosition = CrewPosition.Create(crew.CtrlNbr, f.CraftRoleCtrlNbr, 1, staffablePosition.CtrlNbr);
            ctx.CrewPositions.Add(crewPosition);
            await EnsureBulletinRuleAsync(ctx, f.CraftCtrlNbr, ct);
            await ctx.SaveChangesAsync(ct);
            crewPositionCtrlNbr = crewPosition.CtrlNbr;
        }

        var incumbency = await _host.Crews.CreateCrewIncumbencyAsync(
            crewPositionCtrlNbr.Value, f.EmployeeCtrlNbr.Value, DateTime.UtcNow.AddDays(-1), null, ct);
        return incumbency.CtrlNbr;
    }

    /// <summary>
    /// Places the winner on an OUTGOING extra-board slot via the canonical
    /// <see cref="Application.RosterBoardOps.RosterBoardAppService.AddRosterBoardPositionAsync"/>
    /// (which also creates the backing <see cref="PositionAssignment"/>). Returns the board CtrlNbr.
    /// </summary>
    private async Task<ControlNumber> SeedOutgoingBoardSlotAsync(Fixture f, CancellationToken ct)
    {
        ControlNumber boardCtrlNbr;
        await using (var ctx = _host.CreateReadContext())
        {
            var board = RosterBoard.Create(f.CraftCtrlNbr, f.RosterCtrlNbr, "Extra Board", BoardType.ExtraBoard);
            ctx.Set<RosterBoard>().Add(board);
            await EnsureBulletinRuleAsync(ctx, f.CraftCtrlNbr, ct);
            await ctx.SaveChangesAsync(ct);
            boardCtrlNbr = board.CtrlNbr;
        }

        await _host.RosterBoards.AddRosterBoardPositionAsync(boardCtrlNbr, f.EmployeeCtrlNbr, 1, ct);
        return boardCtrlNbr;
    }

    /// <summary>
    /// Seeds a TARGET crew position (vacant) with an Open <see cref="PositionVacancy"/> and a Posted
    /// <see cref="Bulletin"/> whose bid window is open. The winner is later awarded/force-assigned to
    /// this bulletin. Returns (bulletin CtrlNbr, target crew-position CtrlNbr).
    /// </summary>
    private async Task<(ControlNumber BulletinCtrlNbr, ControlNumber TargetCrewPositionCtrlNbr)>
        SeedTargetCrewBulletinAsync(Fixture f, CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();

        var crew = Crew.Create("REGULAR", f.WorkAreaCtrlNbr, "Target Crew");
        ctx.Crews.Add(crew);
        var staffablePosition = StaffablePosition.Create(StaffablePositionType.Crew);
        ctx.StaffablePositions.Add(staffablePosition);
        await ctx.SaveChangesAsync(ct);

        var crewPosition = CrewPosition.Create(crew.CtrlNbr, f.CraftRoleCtrlNbr, 1, staffablePosition.CtrlNbr);
        ctx.CrewPositions.Add(crewPosition);

        var vacancy = PositionVacancy.Create(
            f.WorkAreaCtrlNbr, StaffablePositionType.Crew, staffablePosition.CtrlNbr, f.CraftCtrlNbr,
            "INCUMBENT_VACATED", targetName: "Target Crew — Position 1");
        vacancy.MarkBulletined();
        ctx.Set<PositionVacancy>().Add(vacancy);
        await ctx.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;
        var bulletin = Bulletin.Create(
            vacancy.CtrlNbr, f.CraftCtrlNbr, now.AddDays(-2), now.AddDays(-1), now);
        ctx.Set<Bulletin>().Add(bulletin);
        await EnsureBulletinRuleAsync(ctx, f.CraftCtrlNbr, ct);
        await ctx.SaveChangesAsync(ct);

        return (bulletin.CtrlNbr, crewPosition.CtrlNbr);
    }

    private static async Task EnsureBulletinRuleAsync(CrewServiceDbContext ctx, ControlNumber craftCtrlNbr, CancellationToken ct)
    {
        // BulletinRules.CraftCtrlNbr is unique; multiple seed helpers may request a rule for the
        // same craft, so insert only when one is not already present.
        var exists = await ctx.Set<BulletinRule>().AnyAsync(r => r.CraftCtrlNbr == craftCtrlNbr, ct);
        if (exists) return;

        var rule = BulletinRule.Create(
            craftCtrlNbr, 48, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), 5, new TimeSpan(8, 0, 0), 0);
        ctx.Set<BulletinRule>().Add(rule);
        await ctx.SaveChangesAsync(ct);
    }

    private async Task<List<PositionAssignment>> GetAssignmentsAsync(ControlNumber employeeCtrlNbr, CancellationToken ct)
    {
        await using var uow = await _host.UowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
    }

    private async Task<CrewIncumbency?> GetIncumbencyAsync(ControlNumber incumbencyCtrlNbr, CancellationToken ct)
    {
        await using var uow = await _host.UowFactory.CreateAsync(cancellationToken: ct);
        return await uow.CrewIncumbencies.GetByCtrlNbrAsync(incumbencyCtrlNbr, ct);
    }

    // ── Award ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Award_WinnerHoldsCrewSeat_VacatesOutgoingSeatAndPlacesOnTarget()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        var outgoingIncumbencyCtrlNbr = await SeedOutgoingCrewSeatAsync(f, ct);
        var (bulletinCtrlNbr, targetCrewPositionCtrlNbr) = await SeedTargetCrewBulletinAsync(f, ct);

        // Sanity: the winner holds exactly their outgoing crew seat before the award.
        Assert.Single(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));

        await _host.Bulletins.AwardBulletinAsync(bulletinCtrlNbr, f.EmployeeCtrlNbr, ct);

        // The winner still holds exactly one assignment, now on the bulletin's TARGET crew position —
        // the outgoing seat's assignment was vacated and removed, not duplicated.
        var assignments = await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct);
        var assignment = Assert.Single(assignments);
        Assert.Equal(PositionAssignmentType.BulletinAssignment, assignment.AssignmentType);
        Assert.Equal(targetCrewPositionCtrlNbr, assignment.AssignmentSourceCtrlNbr);

        // The outgoing crew incumbency was ended (vacated), not left active.
        var outgoing = await GetIncumbencyAsync(outgoingIncumbencyCtrlNbr, ct);
        Assert.NotNull(outgoing);
        Assert.NotNull(outgoing!.EndUtc);
    }

    [Fact]
    public async Task Award_WinnerHoldsBoardSlot_VacatesOutgoingSlotAndPlacesOnTarget()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        await SeedOutgoingBoardSlotAsync(f, ct);
        var (bulletinCtrlNbr, targetCrewPositionCtrlNbr) = await SeedTargetCrewBulletinAsync(f, ct);

        // Sanity: the winner holds exactly their outgoing board slot before the award.
        var before = Assert.Single(await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct));
        Assert.Equal(PositionAssignmentType.Board, before.AssignmentType);

        await _host.Bulletins.AwardBulletinAsync(bulletinCtrlNbr, f.EmployeeCtrlNbr, ct);

        // The board slot's assignment was vacated/removed and the winner now holds the target seat.
        var assignments = await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct);
        var assignment = Assert.Single(assignments);
        Assert.Equal(PositionAssignmentType.BulletinAssignment, assignment.AssignmentType);
        Assert.Equal(targetCrewPositionCtrlNbr, assignment.AssignmentSourceCtrlNbr);
    }

    // ── Force assign ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ForceAssign_WinnerHoldsBoardSlot_VacatesOutgoingSlotAndPlacesOnTarget()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        await SeedOutgoingBoardSlotAsync(f, ct);
        var (bulletinCtrlNbr, targetCrewPositionCtrlNbr) = await SeedTargetCrewBulletinAsync(f, ct);

        // Transition the bulletin to NoBid so it is eligible for an explicit force assignment.
        await _host.Bulletins.SetBulletinNoBidAsync(bulletinCtrlNbr, ct);

        await _host.Bulletins.ForceAssignBulletinAsync(bulletinCtrlNbr, f.EmployeeCtrlNbr, ct);

        // The outgoing board slot was vacated/removed and the winner now holds the target seat as a
        // force assignment.
        var assignments = await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct);
        var assignment = Assert.Single(assignments);
        Assert.Equal(PositionAssignmentType.ForceAssignment, assignment.AssignmentType);
        Assert.Equal(targetCrewPositionCtrlNbr, assignment.AssignmentSourceCtrlNbr);
    }
}