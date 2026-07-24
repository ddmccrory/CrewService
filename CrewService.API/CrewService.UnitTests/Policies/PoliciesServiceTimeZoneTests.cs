using CrewService.Application.BackgroundWorkers;
using CrewService.Application.Authorization;
using CrewService.Application.Notifications;
using CrewService.Application.Policies;
using CrewService.Application.TenantConfig;
using CrewService.Application.Time;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.UnitTests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CrewService.UnitTests.Policies;

public sealed class PoliciesServiceTimeZoneTests : IDisposable
{
    private readonly SeniorityVacancyTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task GetAllSeniorityMoves_BoardTarget_ResolvesWorkAreaTimeZoneId()
    {
        var ct = TestContext.Current.CancellationToken;

        ControlNumber railroadCtrlNbr;
        ControlNumber craftCtrlNbr;
        ControlNumber employeeCtrlNbr;

        await using (var ctx = _host.CreateReadContext())
        {
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
                railroadCtrlNbr: railroad.CtrlNbr,
                timeZoneId: "UTC");
            ctx.DynamicGroups.Add(workArea);

            var craft = Craft.Create(null, workArea.CtrlNbr, "Engineer", "Engineers", 1, false, false, 0, 0, 0, 0, 0, false, false, false, 0);
            ctx.Crafts.Add(craft);
            await ctx.SaveChangesAsync(ct);

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

            var board = RosterBoard.Create(
                craft.CtrlNbr,
                roster.CtrlNbr,
                "Trainman Extra Board",
                BoardType.ExtraBoard,
                RotationType.StandardRotation,
                isActive: true,
                requiredPositions: 1);
            ctx.Set<RosterBoard>().Add(board);

            var targetStaffablePosition = StaffablePosition.Create(StaffablePositionType.Board);
            ctx.StaffablePositions.Add(targetStaffablePosition);
            await ctx.SaveChangesAsync(ct);

            board.AddPosition(employee.CtrlNbr, 1, targetStaffablePosition.CtrlNbr);
            ctx.Set<RosterBoard>().Update(board);

            var move = SeniorityMove.Create(
                railroad.CtrlNbr,
                employee.CtrlNbr,
                craft.CtrlNbr,
                targetStaffablePosition.CtrlNbr,
                displacedEmployeeCtrlNbr: null,
                daysOnCurrentPosition: 0,
                moveType: SeniorityMoveType.Hangout,
                effectiveUtc: DateTime.UtcNow.AddHours(2));
            ctx.Set<SeniorityMove>().Add(move);
            await ctx.SaveChangesAsync(ct);

            railroadCtrlNbr = railroad.CtrlNbr;
            craftCtrlNbr = craft.CtrlNbr;
            employeeCtrlNbr = employee.CtrlNbr;
        }

        var notifications = new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            new RailroadResolver(),
            new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance));
        var execution = new SeniorityMoveExecutionService(
            _host.UowFactory,
            NullLogger<SeniorityMoveExecutionService>.Instance,
            notifications);
        var service = new PoliciesService(
            _host.UowFactory,
            new SeniorityMoveSignal(),
            new AbsenceMarkOffSignal(),
            new WorkAreaClock(TimeProvider.System, _host.UowFactory),
            notifications,
            new TestCurrentUserService(),
            execution,
            new TestRequestActorContextResolver(),
            new RequestActorContextPolicy(),
            new RailroadResolver());

        var moves = await service.GetAllSeniorityMovesAsync(ct);
        var item = Assert.Single(moves);

        Assert.Equal(employeeCtrlNbr, item.Move.EmployeeCtrlNbr);
        Assert.Equal(craftCtrlNbr, item.Move.CraftCtrlNbr);
        Assert.Equal(railroadCtrlNbr, item.Move.RailroadCtrlNbr);
        Assert.Equal("UTC", item.WorkAreaTimeZoneId);
    }
}
