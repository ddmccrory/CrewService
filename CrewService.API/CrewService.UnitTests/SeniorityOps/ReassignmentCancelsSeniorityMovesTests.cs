using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.UnitTests.Fixtures;
using CrewService.Application.Notifications;
using CrewService.Application.Policies;
using CrewService.Application.TenantConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CrewService.UnitTests.SeniorityOps;

public sealed class ReassignmentCancelsSeniorityMovesTests : IDisposable
{
    private const string ReassignmentCancellationReason = "Cancelled because employee was assigned to a different position.";
    private const string ExecutedMoveCancellationReason = "Superseded by an executed seniority move for the same employee.";

    private readonly SeniorityVacancyTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private sealed record Fixture(
        ControlNumber RailroadCtrlNbr,
        ControlNumber WorkAreaCtrlNbr,
        ControlNumber CraftCtrlNbr,
        ControlNumber CraftRoleCtrlNbr,
        ControlNumber RosterCtrlNbr,
        ControlNumber EmployeeCtrlNbr);

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

        var status = EmploymentStatus.Create(workArea.CtrlNbr, "ACT", "Active", 1, "A");
        ctx.EmploymentStatuses.Add(status);
        await ctx.SaveChangesAsync(ct);

        var employee = Employee.Create(
            workArea.CtrlNbr, "jdoe", "E001", "000-00-0001", Gender.Male, Race.White,
            new DateTime(1990, 1, 1), DateTime.UtcNow, status.CtrlNbr, "jdoe@example.com", "admin", "Admin User");
        ctx.Employees.Add(employee);
        await ctx.SaveChangesAsync(ct);

        return new Fixture(
            railroad.CtrlNbr,
            workArea.CtrlNbr,
            craft.CtrlNbr,
            role.CtrlNbr,
            roster.CtrlNbr,
            employee.CtrlNbr);
    }

    private static async Task SeedPendingAndApprovedMovesAsync(
        CrewServiceDbContext ctx,
        Fixture fixture,
        ControlNumber targetPositionCtrlNbr,
        string moveType,
        CancellationToken ct)
    {
        var pending = SeniorityMove.Create(
            fixture.RailroadCtrlNbr,
            fixture.EmployeeCtrlNbr,
            fixture.CraftCtrlNbr,
            targetPositionCtrlNbr,
            displacedEmployeeCtrlNbr: null,
            daysOnCurrentPosition: 30,
            moveType: moveType,
            effectiveUtc: DateTime.UtcNow.AddHours(3));

        var approved = SeniorityMove.Create(
            fixture.RailroadCtrlNbr,
            fixture.EmployeeCtrlNbr,
            fixture.CraftCtrlNbr,
            targetPositionCtrlNbr,
            displacedEmployeeCtrlNbr: null,
            daysOnCurrentPosition: 30,
            moveType: moveType,
            effectiveUtc: DateTime.UtcNow.AddHours(4));
        approved.Approve();

        ctx.Set<SeniorityMove>().Add(pending);
        ctx.Set<SeniorityMove>().Add(approved);
        await ctx.SaveChangesAsync(ct);
    }

    [Fact]
    public async Task CreateCrewIncumbency_Reassignment_CancelsPendingAndApprovedMoves()
    {
        var ct = TestContext.Current.CancellationToken;
        var fixture = await SeedBaseAsync(ct);

        ControlNumber crewPositionCtrlNbr;
        await using (var ctx = _host.CreateReadContext())
        {
            var crew = Crew.Create("REGULAR", fixture.WorkAreaCtrlNbr, "Test Crew");
            ctx.Crews.Add(crew);

            var crewStaffablePosition = StaffablePosition.Create(StaffablePositionType.Crew);
            var targetPosition = StaffablePosition.Create(StaffablePositionType.Crew);
            ctx.StaffablePositions.Add(crewStaffablePosition);
            ctx.StaffablePositions.Add(targetPosition);
            await ctx.SaveChangesAsync(ct);

            var crewPosition = CrewPosition.Create(crew.CtrlNbr, fixture.CraftRoleCtrlNbr, 1, crewStaffablePosition.CtrlNbr);
            ctx.CrewPositions.Add(crewPosition);
            await ctx.SaveChangesAsync(ct);

            crewPositionCtrlNbr = crewPosition.CtrlNbr;

            await SeedPendingAndApprovedMovesAsync(ctx, fixture, targetPosition.CtrlNbr, SeniorityMoveType.Voluntary, ct);
        }

        await _host.Crews.CreateCrewIncumbencyAsync(
            crewPositionCtrlNbr.Value,
            fixture.EmployeeCtrlNbr.Value,
            DateTime.UtcNow,
            null,
            ct);

        await using var verifyUow = await _host.UowFactory.CreateAsync(cancellationToken: ct);
        var moves = await verifyUow.SeniorityMoves.GetByEmployeeAsync(fixture.EmployeeCtrlNbr, ct);

        Assert.Equal(2, moves.Count);
        Assert.All(moves, move => Assert.Equal(SeniorityMoveStatus.Cancelled, move.Status));
        Assert.All(moves, move => Assert.Equal(ReassignmentCancellationReason, move.CancellationReason));
    }

    [Fact]
    public async Task AddRosterBoardPosition_Reassignment_CancelsPendingAndApprovedMoves()
    {
        var ct = TestContext.Current.CancellationToken;
        var fixture = await SeedBaseAsync(ct);

        ControlNumber boardCtrlNbr;
        await using (var ctx = _host.CreateReadContext())
        {
            var board = RosterBoard.Create(
                fixture.CraftCtrlNbr,
                fixture.RosterCtrlNbr,
                "Engineer Extra Board",
                BoardType.ExtraBoard,
                RotationType.StandardRotation,
                isActive: true,
                requiredPositions: 1);
            ctx.Set<RosterBoard>().Add(board);

            var targetPosition = StaffablePosition.Create(StaffablePositionType.Crew);
            ctx.StaffablePositions.Add(targetPosition);
            await ctx.SaveChangesAsync(ct);

            boardCtrlNbr = board.CtrlNbr;

            await SeedPendingAndApprovedMovesAsync(ctx, fixture, targetPosition.CtrlNbr, SeniorityMoveType.Voluntary, ct);
        }

        await _host.RosterBoards.AddRosterBoardPositionAsync(
            boardCtrlNbr,
            fixture.EmployeeCtrlNbr,
            positionOrder: 1,
            assignedDateUtc: null,
            ct);

        await using var verifyUow = await _host.UowFactory.CreateAsync(cancellationToken: ct);
        var moves = await verifyUow.SeniorityMoves.GetByEmployeeAsync(fixture.EmployeeCtrlNbr, ct);

        Assert.Equal(2, moves.Count);
        Assert.All(moves, move => Assert.Equal(SeniorityMoveStatus.Cancelled, move.Status));
        Assert.All(moves, move => Assert.Equal(ReassignmentCancellationReason, move.CancellationReason));
    }

    [Fact]
    public async Task ExecutedSeniorityMove_CancelsMoversOtherPendingAndApprovedMoves()
    {
        var ct = TestContext.Current.CancellationToken;
        var fixture = await SeedBaseAsync(ct);

        ControlNumber targetStaffablePositionCtrlNbr;
        ControlNumber moveToExecuteCtrlNbr;
        ControlNumber pendingMoveCtrlNbr;
        ControlNumber approvedMoveCtrlNbr;

        await using (var ctx = _host.CreateReadContext())
        {
            var source = StaffablePosition.Create(StaffablePositionType.Crew);
            var target = StaffablePosition.Create(StaffablePositionType.Crew);
            var third = StaffablePosition.Create(StaffablePositionType.Crew);
            ctx.StaffablePositions.AddRange(source, target, third);
            await ctx.SaveChangesAsync(ct);

            targetStaffablePositionCtrlNbr = target.CtrlNbr;

            var currentAssignment = PositionAssignment.Create(
                source.CtrlNbr,
                fixture.EmployeeCtrlNbr,
                PositionAssignmentType.Direct,
                assignmentSourceCtrlNbr: null,
                assignedDateUtc: DateTime.UtcNow.AddDays(-10));
            ctx.PositionAssignments.Add(currentAssignment);

            var moveToExecute = SeniorityMove.Create(
                fixture.RailroadCtrlNbr,
                fixture.EmployeeCtrlNbr,
                fixture.CraftCtrlNbr,
                target.CtrlNbr,
                displacedEmployeeCtrlNbr: null,
                daysOnCurrentPosition: 10,
                moveType: SeniorityMoveType.Voluntary,
                effectiveUtc: DateTime.UtcNow.AddMinutes(-1));
            moveToExecute.Approve();

            var pendingMove = SeniorityMove.Create(
                fixture.RailroadCtrlNbr,
                fixture.EmployeeCtrlNbr,
                fixture.CraftCtrlNbr,
                third.CtrlNbr,
                displacedEmployeeCtrlNbr: null,
                daysOnCurrentPosition: 10,
                moveType: SeniorityMoveType.Voluntary,
                effectiveUtc: DateTime.UtcNow.AddHours(2));

            var approvedMove = SeniorityMove.Create(
                fixture.RailroadCtrlNbr,
                fixture.EmployeeCtrlNbr,
                fixture.CraftCtrlNbr,
                third.CtrlNbr,
                displacedEmployeeCtrlNbr: null,
                daysOnCurrentPosition: 10,
                moveType: SeniorityMoveType.Hangout,
                effectiveUtc: DateTime.UtcNow.AddHours(3));
            approvedMove.Approve();

            ctx.Set<SeniorityMove>().AddRange(moveToExecute, pendingMove, approvedMove);
            await ctx.SaveChangesAsync(ct);

            moveToExecuteCtrlNbr = moveToExecute.CtrlNbr;
            pendingMoveCtrlNbr = pendingMove.CtrlNbr;
            approvedMoveCtrlNbr = approvedMove.CtrlNbr;
        }

        var notifications = new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            new RailroadResolver(),
            new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance));
        var executionService = new SeniorityMoveExecutionService(
            _host.UowFactory,
            NullLogger<SeniorityMoveExecutionService>.Instance,
            notifications);

        await executionService.ExecuteAsync(moveToExecuteCtrlNbr, ct);

        await using var verifyCtx = _host.CreateReadContext();
        var moveStates = await verifyCtx.Set<SeniorityMove>()
            .Where(m => m.CtrlNbr == moveToExecuteCtrlNbr || m.CtrlNbr == pendingMoveCtrlNbr || m.CtrlNbr == approvedMoveCtrlNbr)
            .ToDictionaryAsync(m => m.CtrlNbr, ct);

        var currentAssignments = await verifyCtx.Set<PositionAssignment>()
            .Where(a => a.EmployeeCtrlNbr == fixture.EmployeeCtrlNbr)
            .ToListAsync(ct);

        Assert.Equal(SeniorityMoveStatus.Completed, moveStates[moveToExecuteCtrlNbr].Status);
        Assert.Equal(SeniorityMoveStatus.Cancelled, moveStates[pendingMoveCtrlNbr].Status);
        Assert.Equal(SeniorityMoveStatus.Cancelled, moveStates[approvedMoveCtrlNbr].Status);
        Assert.Equal(ExecutedMoveCancellationReason, moveStates[pendingMoveCtrlNbr].CancellationReason);
        Assert.Equal(ExecutedMoveCancellationReason, moveStates[approvedMoveCtrlNbr].CancellationReason);
        var assignment = Assert.Single(currentAssignments);
        Assert.Equal(targetStaffablePositionCtrlNbr, assignment.StaffablePositionCtrlNbr);
        Assert.Equal("SeniorityMove", assignment.AssignmentType);
    }

    [Fact]
    public async Task CreateCrewIncumbency_Reassignment_CancelsHangoutPendingAndApprovedMoves()
    {
        var ct = TestContext.Current.CancellationToken;
        var fixture = await SeedBaseAsync(ct);

        ControlNumber crewPositionCtrlNbr;
        await using (var ctx = _host.CreateReadContext())
        {
            var crew = Crew.Create("REGULAR", fixture.WorkAreaCtrlNbr, "Test Crew");
            ctx.Crews.Add(crew);

            var crewStaffablePosition = StaffablePosition.Create(StaffablePositionType.Crew);
            var targetPosition = StaffablePosition.Create(StaffablePositionType.Crew);
            ctx.StaffablePositions.Add(crewStaffablePosition);
            ctx.StaffablePositions.Add(targetPosition);
            await ctx.SaveChangesAsync(ct);

            var crewPosition = CrewPosition.Create(crew.CtrlNbr, fixture.CraftRoleCtrlNbr, 1, crewStaffablePosition.CtrlNbr);
            ctx.CrewPositions.Add(crewPosition);
            await ctx.SaveChangesAsync(ct);

            crewPositionCtrlNbr = crewPosition.CtrlNbr;

            await SeedPendingAndApprovedMovesAsync(ctx, fixture, targetPosition.CtrlNbr, SeniorityMoveType.Hangout, ct);
        }

        await _host.Crews.CreateCrewIncumbencyAsync(
            crewPositionCtrlNbr.Value,
            fixture.EmployeeCtrlNbr.Value,
            DateTime.UtcNow,
            null,
            ct);

        await using var verifyUow = await _host.UowFactory.CreateAsync(cancellationToken: ct);
        var moves = await verifyUow.SeniorityMoves.GetByEmployeeAsync(fixture.EmployeeCtrlNbr, ct);

        Assert.Equal(2, moves.Count);
        Assert.All(moves, move => Assert.Equal(SeniorityMoveStatus.Cancelled, move.Status));
        Assert.All(moves, move => Assert.Equal(ReassignmentCancellationReason, move.CancellationReason));
        Assert.All(moves, move => Assert.Equal(SeniorityMoveType.Hangout, move.MoveType));
    }

    [Fact]
    public async Task CreateCrewIncumbency_Reassignment_DoesNotCancelNoAccessMoves()
    {
        var ct = TestContext.Current.CancellationToken;
        var fixture = await SeedBaseAsync(ct);

        ControlNumber crewPositionCtrlNbr;
        await using (var ctx = _host.CreateReadContext())
        {
            var crew = Crew.Create("REGULAR", fixture.WorkAreaCtrlNbr, "Test Crew");
            ctx.Crews.Add(crew);

            var crewStaffablePosition = StaffablePosition.Create(StaffablePositionType.Crew);
            var targetPosition = StaffablePosition.Create(StaffablePositionType.Crew);
            ctx.StaffablePositions.Add(crewStaffablePosition);
            ctx.StaffablePositions.Add(targetPosition);
            await ctx.SaveChangesAsync(ct);

            var crewPosition = CrewPosition.Create(crew.CtrlNbr, fixture.CraftRoleCtrlNbr, 1, crewStaffablePosition.CtrlNbr);
            ctx.CrewPositions.Add(crewPosition);
            await ctx.SaveChangesAsync(ct);

            crewPositionCtrlNbr = crewPosition.CtrlNbr;

            await SeedPendingAndApprovedMovesAsync(ctx, fixture, targetPosition.CtrlNbr, SeniorityMoveType.NoAccess, ct);
        }

        await _host.Crews.CreateCrewIncumbencyAsync(
            crewPositionCtrlNbr.Value,
            fixture.EmployeeCtrlNbr.Value,
            DateTime.UtcNow,
            null,
            ct);

        await using var verifyUow = await _host.UowFactory.CreateAsync(cancellationToken: ct);
        var moves = await verifyUow.SeniorityMoves.GetByEmployeeAsync(fixture.EmployeeCtrlNbr, ct);

        Assert.Equal(2, moves.Count);
        Assert.Contains(moves, move => move.Status == SeniorityMoveStatus.Pending);
        Assert.Contains(moves, move => move.Status == SeniorityMoveStatus.Approved);
        Assert.All(moves, move => Assert.Equal(SeniorityMoveType.NoAccess, move.MoveType));
        Assert.All(moves, move => Assert.True(string.IsNullOrEmpty(move.CancellationReason)));
    }
}
