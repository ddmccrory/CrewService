using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.Policies;
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

        await _host.RosterBoards.AddRosterBoardPositionAsync(boardCtrlNbr, f.EmployeeCtrlNbr, 1, null, ct);
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

    private async Task<(ControlNumber BulletinCtrlNbr, ControlNumber TargetBoardPositionCtrlNbr, ControlNumber TargetBoardStaffablePositionCtrlNbr)>
        SeedTargetBoardBulletinAsync(Fixture f, CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();

        var boardMemberStatus = await ctx.EmploymentStatuses
            .FirstAsync(es => es.ClientCtrlNbr == f.WorkAreaCtrlNbr, ct);
        var boardMember = Employee.Create(
            f.WorkAreaCtrlNbr, "boardmember", "E002", "000-00-0002", Gender.Female, Race.White,
            new DateTime(1991, 1, 1), DateTime.UtcNow, boardMemberStatus.CtrlNbr,
            "boardmember@example.com", "admin", "Admin User");
        ctx.Employees.Add(boardMember);

        var board = RosterBoard.Create(f.CraftCtrlNbr, f.RosterCtrlNbr, "Target Board", BoardType.ExtraBoard);
        var boardSlot = StaffablePosition.Create(StaffablePositionType.Board);
        ctx.StaffablePositions.Add(boardSlot);
        await ctx.SaveChangesAsync(ct);

        var boardPosition = board.AddPosition(boardMember.CtrlNbr, 1, boardSlot.CtrlNbr);
        ctx.Set<RosterBoard>().Add(board);

        var vacancy = PositionVacancy.Create(
            f.WorkAreaCtrlNbr,
            StaffablePositionType.Board,
            boardSlot.CtrlNbr,
            f.CraftCtrlNbr,
            "INCUMBENT_VACATED",
            targetName: "Target Board — Position 1");
        vacancy.MarkBulletined();
        ctx.Set<PositionVacancy>().Add(vacancy);
        await ctx.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;
        var bulletin = Bulletin.Create(
            vacancy.CtrlNbr,
            f.CraftCtrlNbr,
            now.AddDays(-2),
            now.AddDays(-1),
            now);
        ctx.Set<Bulletin>().Add(bulletin);
        await EnsureBulletinRuleAsync(ctx, f.CraftCtrlNbr, ct);
        await ctx.SaveChangesAsync(ct);

        return (bulletin.CtrlNbr, boardPosition.CtrlNbr, boardSlot.CtrlNbr);
    }

    private async Task<(ControlNumber BulletinCtrlNbr, ControlNumber TargetCrewPositionCtrlNbr)>
        SeedOpenTargetCrewBulletinAsync(Fixture f, CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();

        var crew = Crew.Create("REGULAR", f.WorkAreaCtrlNbr, "Open Bid Target Crew");
        ctx.Crews.Add(crew);
        var staffablePosition = StaffablePosition.Create(StaffablePositionType.Crew);
        ctx.StaffablePositions.Add(staffablePosition);
        await ctx.SaveChangesAsync(ct);

        var crewPosition = CrewPosition.Create(crew.CtrlNbr, f.CraftRoleCtrlNbr, 1, staffablePosition.CtrlNbr);
        ctx.CrewPositions.Add(crewPosition);

        var vacancy = PositionVacancy.Create(
            f.WorkAreaCtrlNbr, StaffablePositionType.Crew, staffablePosition.CtrlNbr, f.CraftCtrlNbr,
            "INCUMBENT_VACATED", targetName: "Open Bid Target Crew — Position 1");
        vacancy.MarkBulletined();
        ctx.Set<PositionVacancy>().Add(vacancy);
        await ctx.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;
        var bulletin = Bulletin.Create(
            vacancy.CtrlNbr, f.CraftCtrlNbr, now.AddHours(-1), now.AddHours(2), now.AddDays(1));
        ctx.Set<Bulletin>().Add(bulletin);
        await EnsureBulletinRuleAsync(ctx, f.CraftCtrlNbr, ct);
        await ctx.SaveChangesAsync(ct);

        return (bulletin.CtrlNbr, crewPosition.CtrlNbr);
    }

    private async Task AddRoleQualificationRequirementAsync(Fixture f, CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();
        var craftRole = await ctx.Set<CraftRole>()
            .Include(r => r.RequiredQualifications)
            .SingleAsync(r => r.CtrlNbr == f.CraftRoleCtrlNbr, ct);

        var foremanQualification = QualificationType.Create(
            f.ParentCtrlNbr,
            "FOREMANQ",
            "Foreman Qualification",
            isBlocking: false);

        ctx.Set<QualificationType>().Add(foremanQualification);
        await ctx.SaveChangesAsync(ct);

        craftRole.AddRequiredQualification(foremanQualification.CtrlNbr);
        ctx.Set<CraftRole>().Update(craftRole);
        await ctx.SaveChangesAsync(ct);
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

    /// <summary>
    /// Gives the fixture employee an active-roster <see cref="Seniority"/> entry on the fixture's
    /// roster so the force-assign candidate selection (which excludes anyone without one) can pick
    /// them. The craft role carries no required qualifications, so this membership is the only gate.
    /// </summary>
    private async Task SeedActiveRosterSeniorityAsync(Fixture f, CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();
        var state = SeniorityState.Create("Active", StateType.Active, f.ParentCtrlNbr.Value);
        ctx.Set<SeniorityState>().Add(state);
        await ctx.SaveChangesAsync(ct);

        var seniority = Seniority.Create(
            f.RosterCtrlNbr, f.EmployeeCtrlNbr, lastActiveRoster: true,
            rosterDate: DateTime.UtcNow.AddDays(-30), rank: 1,
            seniorityStateCtrlNbr: state.CtrlNbr, canTrain: true);
        ctx.Set<Seniority>().Add(seniority);
        await ctx.SaveChangesAsync(ct);
    }

    private async Task<List<PositionAssignment>> GetAssignmentsAsync(ControlNumber employeeCtrlNbr, CancellationToken ct)
    {
        await using var uow = await _host.UowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
    }

    private async Task<List<SeniorityMove>> GetEmployeeMovesAsync(ControlNumber employeeCtrlNbr, CancellationToken ct)
    {
        await using var uow = await _host.UowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SeniorityMoves.GetByEmployeeAsync(employeeCtrlNbr, ct);
    }

    private async Task SeedPendingAndApprovedMovesAsync(
        Fixture f,
        ControlNumber targetPositionCtrlNbr,
        string moveType,
        CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();

        var pending = SeniorityMove.Create(
            f.RailroadCtrlNbr,
            f.EmployeeCtrlNbr,
            f.CraftCtrlNbr,
            targetPositionCtrlNbr,
            displacedEmployeeCtrlNbr: null,
            daysOnCurrentPosition: 30,
            moveType: moveType,
            effectiveUtc: DateTime.UtcNow.AddHours(3));

        var approved = SeniorityMove.Create(
            f.RailroadCtrlNbr,
            f.EmployeeCtrlNbr,
            f.CraftCtrlNbr,
            targetPositionCtrlNbr,
            displacedEmployeeCtrlNbr: null,
            daysOnCurrentPosition: 30,
            moveType: moveType,
            effectiveUtc: DateTime.UtcNow.AddHours(4));
        approved.Approve();

        ctx.Set<SeniorityMove>().AddRange(pending, approved);
        await ctx.SaveChangesAsync(ct);
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

    [Fact]
    public async Task Award_CancelsPendingAndApprovedHangoutMovesForWinner()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        await SeedOutgoingBoardSlotAsync(f, ct);
        var (bulletinCtrlNbr, _) = await SeedTargetCrewBulletinAsync(f, ct);

        var targetPosition = StaffablePosition.Create(StaffablePositionType.Crew);
        await using (var ctx = _host.CreateReadContext())
        {
            ctx.StaffablePositions.Add(targetPosition);
            await ctx.SaveChangesAsync(ct);
        }

        await SeedPendingAndApprovedMovesAsync(f, targetPosition.CtrlNbr, SeniorityMoveType.Hangout, ct);

        await _host.Bulletins.AwardBulletinAsync(bulletinCtrlNbr, f.EmployeeCtrlNbr, ct);

        var moves = await GetEmployeeMovesAsync(f.EmployeeCtrlNbr, ct);
        Assert.Equal(2, moves.Count);
        Assert.All(moves, move => Assert.Equal(SeniorityMoveStatus.Cancelled, move.Status));
    }

    [Fact]
    public async Task Award_TargetIsBoard_AssignsWinnerToBoardSlot()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        var (bulletinCtrlNbr, targetBoardPositionCtrlNbr, targetBoardStaffablePositionCtrlNbr) = await SeedTargetBoardBulletinAsync(f, ct);

        await _host.Bulletins.AwardBulletinAsync(bulletinCtrlNbr, f.EmployeeCtrlNbr, ct);

        var assignments = await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct);
        var assignment = Assert.Single(assignments);
        Assert.Equal(PositionAssignmentType.Board, assignment.AssignmentType);
        Assert.Equal(targetBoardPositionCtrlNbr, assignment.AssignmentSourceCtrlNbr);
        Assert.Equal(targetBoardStaffablePositionCtrlNbr, assignment.StaffablePositionCtrlNbr);
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

    [Fact]
    public async Task ForceAssign_TargetIsBoard_AssignsWinnerToBoardSlot()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        var (bulletinCtrlNbr, targetBoardPositionCtrlNbr, targetBoardStaffablePositionCtrlNbr) = await SeedTargetBoardBulletinAsync(f, ct);

        await _host.Bulletins.SetBulletinNoBidAsync(bulletinCtrlNbr, ct);
        await _host.Bulletins.ForceAssignBulletinAsync(bulletinCtrlNbr, f.EmployeeCtrlNbr, ct);

        var assignments = await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct);
        var assignment = Assert.Single(assignments);
        Assert.Equal(PositionAssignmentType.Board, assignment.AssignmentType);
        Assert.Equal(targetBoardPositionCtrlNbr, assignment.AssignmentSourceCtrlNbr);
        Assert.Equal(targetBoardStaffablePositionCtrlNbr, assignment.StaffablePositionCtrlNbr);
    }

    [Fact]
    public async Task SetBulletinNoBid_EligibleCandidateExists_AutoChainsForceAssign()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        await SeedOutgoingBoardSlotAsync(f, ct);
        // Give the board member an active-roster seniority entry so the force-assign selection finds
        // them as an eligible candidate (selection excludes employees without one).
        await SeedActiveRosterSeniorityAsync(f, ct);
        var (bulletinCtrlNbr, targetCrewPositionCtrlNbr) = await SeedTargetCrewBulletinAsync(f, ct);

        // Only the NoBid call is made — it must automatically chain the force-assign process with no
        // further action from the caller.
        var result = await _host.Bulletins.SetBulletinNoBidAsync(bulletinCtrlNbr, ct);

        // The returned bulletin is Forced and the eligible candidate now holds the target seat as a
        // force assignment — proving the chain ran end-to-end.
        Assert.Equal("Forced", result.Status);
        Assert.Equal(f.EmployeeCtrlNbr, result.AwardedEmployeeCtrlNbr);
        var assignments = await GetAssignmentsAsync(f.EmployeeCtrlNbr, ct);
        var assignment = Assert.Single(assignments);
        Assert.Equal(PositionAssignmentType.ForceAssignment, assignment.AssignmentType);
        Assert.Equal(targetCrewPositionCtrlNbr, assignment.AssignmentSourceCtrlNbr);
    }

    [Fact]
    public async Task SubmitBid_UnqualifiedForTargetRole_RejectsBid()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedBaseAsync(ct);
        await AddRoleQualificationRequirementAsync(f, ct);
        var (bulletinCtrlNbr, _) = await SeedOpenTargetCrewBulletinAsync(f, ct);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _host.Bulletins.SubmitBidAsync(bulletinCtrlNbr.Value, f.EmployeeCtrlNbr.Value, priority: 1, ct));

        Assert.Contains("not eligible to bid", ex.Message, StringComparison.OrdinalIgnoreCase);
        var bids = await _host.Bulletins.GetBidsByBulletinAsync(bulletinCtrlNbr, ct);
        Assert.Empty(bids);
    }
}