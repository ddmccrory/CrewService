using CrewService.Application.BackgroundWorkers;
using CrewService.Application.Authorization;
using CrewService.Application.Notifications;
using CrewService.Application.Policies;
using CrewService.Application.TenantConfig;
using CrewService.Application.Time;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.UnitTests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CrewService.UnitTests.Policies;

public sealed class PoliciesServiceNoAccessTests : IDisposable
{
    private readonly SeniorityVacancyTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public void NoAccessPolicy_CreateLegacyDefaults_UsesExpectedValues()
    {
        var policy = NoAccessPolicy.CreateLegacyDefaults(ControlNumber.Create(1), ControlNumber.Create(2));

        Assert.True(policy.IsEnabled);
        Assert.True(policy.AllowEmployeeSelfRequest);
        Assert.True(policy.RequireBulletinAccessAudit);
        Assert.True(policy.BlockIfOnExtendedAbsence);
        Assert.True(policy.RequirePositionCurrentlyAssigned);
        Assert.True(policy.ApplyExtraBoardSpecialCase);
        Assert.True(policy.RequireBoardAvailableForMoveOff);
        Assert.True(policy.AutoApproveNoAccess);
        Assert.True(policy.AllowAdminOverride);
        Assert.True(policy.BlockIfEmployeeMarkedOff);
        Assert.True(policy.BlockIfLastVacatedIncumbent);
        Assert.Equal(NoAccessEffectiveDateMode.NextDay0001, policy.DefaultEffectiveMode);
    }

    [Fact]
    public async Task RequestNoAccessByBulletinAsync_WhenPolicyRequiresAuditAndNoViewRecorded_CreatesMove()
    {
        var seeded = await SeedNoAccessScenarioAsync(requireBulletinAccessAudit: true);
        var sut = CreatePoliciesService();

        var move = await sut.RequestNoAccessByBulletinAsync(
            seeded.RailroadCtrlNbr.Value,
            seeded.CraftCtrlNbr.Value,
            seeded.BulletinCtrlNbr.Value,
            seeded.MoverEmployeeCtrlNbr.Value,
            adminOverride: false,
            TestContext.Current.CancellationToken);

        Assert.Equal(SeniorityMoveType.NoAccess, move.MoveType);
    }

    [Fact]
    public async Task RequestNoAccessByBulletinAsync_WhenPolicyRequiresAuditAndViewRecorded_Throws()
    {
        var seeded = await SeedNoAccessScenarioAsync(requireBulletinAccessAudit: true);
        await SeedBulletinAuditViewAsync(seeded.BulletinCtrlNbr, seeded.MoverEmployeeCtrlNbr);
        var sut = CreatePoliciesService();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RequestNoAccessByBulletinAsync(
                seeded.RailroadCtrlNbr.Value,
                seeded.CraftCtrlNbr.Value,
                seeded.BulletinCtrlNbr.Value,
                seeded.MoverEmployeeCtrlNbr.Value,
                adminOverride: false,
                TestContext.Current.CancellationToken));

        Assert.Contains("viewed this bulletin", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestNoAccessByBulletinAsync_WhenValid_CreatesNoAccessMove()
    {
        var seeded = await SeedNoAccessScenarioAsync(requireBulletinAccessAudit: false);
        var sut = CreatePoliciesService();

        var move = await sut.RequestNoAccessByBulletinAsync(
            seeded.RailroadCtrlNbr.Value,
            seeded.CraftCtrlNbr.Value,
            seeded.BulletinCtrlNbr.Value,
            seeded.MoverEmployeeCtrlNbr.Value,
            adminOverride: false,
            TestContext.Current.CancellationToken);

        Assert.Equal(SeniorityMoveType.NoAccess, move.MoveType);
        Assert.Equal(SeniorityMoveStatus.Approved, move.Status);
        Assert.Equal(seeded.RailroadCtrlNbr, move.RailroadCtrlNbr);
        Assert.Equal(seeded.CraftCtrlNbr, move.CraftCtrlNbr);
        Assert.Equal(seeded.TargetPositionCtrlNbr, move.TargetPositionCtrlNbr);
        Assert.Equal(seeded.DisplacedEmployeeCtrlNbr, move.DisplacedEmployeeCtrlNbr);
    }

    [Fact]
    public async Task RequestNoAccessByBulletinAsync_WhenEmployeeMarkedOff_Throws()
    {
        var seeded = await SeedNoAccessScenarioAsync(requireBulletinAccessAudit: false);
        await SeedActiveMarkOffAsync(seeded.MoverEmployeeCtrlNbr);
        var sut = CreatePoliciesService();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RequestNoAccessByBulletinAsync(
                seeded.RailroadCtrlNbr.Value,
                seeded.CraftCtrlNbr.Value,
                seeded.BulletinCtrlNbr.Value,
                seeded.MoverEmployeeCtrlNbr.Value,
                adminOverride: false,
                TestContext.Current.CancellationToken));

        Assert.Contains("marked off", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestNoAccessByBulletinAsync_WhenEmployeeMarkedOffAndPolicyToggleDisabled_CreatesMove()
    {
        var seeded = await SeedNoAccessScenarioAsync(
            requireBulletinAccessAudit: false,
            blockIfEmployeeMarkedOff: false);
        await SeedActiveMarkOffAsync(seeded.MoverEmployeeCtrlNbr);
        var sut = CreatePoliciesService();

        var move = await sut.RequestNoAccessByBulletinAsync(
            seeded.RailroadCtrlNbr.Value,
            seeded.CraftCtrlNbr.Value,
            seeded.BulletinCtrlNbr.Value,
            seeded.MoverEmployeeCtrlNbr.Value,
            adminOverride: false,
            TestContext.Current.CancellationToken);

        Assert.Equal(SeniorityMoveType.NoAccess, move.MoveType);
    }

    [Fact]
    public async Task RequestNoAccessByBulletinAsync_WhenEmployeeWasLastVacatedIncumbent_Throws()
    {
        var seeded = await SeedNoAccessScenarioAsync(
            requireBulletinAccessAudit: false,
            previousIncumbentIsMover: true);

        var sut = CreatePoliciesService();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RequestNoAccessByBulletinAsync(
                seeded.RailroadCtrlNbr.Value,
                seeded.CraftCtrlNbr.Value,
                seeded.BulletinCtrlNbr.Value,
                seeded.MoverEmployeeCtrlNbr.Value,
                adminOverride: false,
                TestContext.Current.CancellationToken));

        Assert.Contains("most recently vacated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestNoAccessByBulletinAsync_WhenEmployeeWasLastVacatedIncumbentAndPolicyToggleDisabled_CreatesMove()
    {
        var seeded = await SeedNoAccessScenarioAsync(
            requireBulletinAccessAudit: false,
            previousIncumbentIsMover: true,
            blockIfLastVacatedIncumbent: false);

        var sut = CreatePoliciesService();

        var move = await sut.RequestNoAccessByBulletinAsync(
            seeded.RailroadCtrlNbr.Value,
            seeded.CraftCtrlNbr.Value,
            seeded.BulletinCtrlNbr.Value,
            seeded.MoverEmployeeCtrlNbr.Value,
            adminOverride: false,
            TestContext.Current.CancellationToken);

        Assert.Equal(SeniorityMoveType.NoAccess, move.MoveType);
    }

    [Fact]
    public async Task RequestNoAccessByBulletinAsync_WhenTargetPositionUnassigned_CreatesNoAccessMove()
    {
        var seeded = await SeedNoAccessScenarioAsync(
            requireBulletinAccessAudit: false,
            targetAssigned: false);
        var sut = CreatePoliciesService();

        var move = await sut.RequestNoAccessByBulletinAsync(
            seeded.RailroadCtrlNbr.Value,
            seeded.CraftCtrlNbr.Value,
            seeded.BulletinCtrlNbr.Value,
            seeded.MoverEmployeeCtrlNbr.Value,
            adminOverride: false,
            TestContext.Current.CancellationToken);

        Assert.Equal(SeniorityMoveType.NoAccess, move.MoveType);
        Assert.Null(move.DisplacedEmployeeCtrlNbr);
    }

    [Fact]
    public async Task RequestNoAccessByBulletinAsync_WhenBoardTargetBulletin_CreatesNoAccessMoveToBoardSlot()
    {
        var seeded = await SeedNoAccessScenarioAsync(
            requireBulletinAccessAudit: false,
            targetType: StaffablePositionType.Board,
            applyExtraBoardSpecialCase: false,
            targetAssigned: false);
        var sut = CreatePoliciesService();

        var move = await sut.RequestNoAccessByBulletinAsync(
            seeded.RailroadCtrlNbr.Value,
            seeded.CraftCtrlNbr.Value,
            seeded.BulletinCtrlNbr.Value,
            seeded.MoverEmployeeCtrlNbr.Value,
            adminOverride: false,
            TestContext.Current.CancellationToken);

        Assert.Equal(SeniorityMoveType.NoAccess, move.MoveType);
        Assert.Equal(seeded.TargetPositionCtrlNbr, move.TargetPositionCtrlNbr);
        Assert.Null(move.DisplacedEmployeeCtrlNbr);
    }

    [Fact]
    public async Task RequestNoAccessByBulletinAsync_WhenExtraBoardSpecialCaseEnabledAndTargetIsExtraBoard_ThrowsWhenNotEligible()
    {
        var seeded = await SeedNoAccessScenarioAsync(
            requireBulletinAccessAudit: false,
            targetType: StaffablePositionType.Board,
            targetBoardType: BoardType.ExtraBoard,
            targetAssigned: false,
            applyExtraBoardSpecialCase: true,
            includeLessSeniorExtraBoardEmployee: false);
        var sut = CreatePoliciesService();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RequestNoAccessByBulletinAsync(
                seeded.RailroadCtrlNbr.Value,
                seeded.CraftCtrlNbr.Value,
                seeded.BulletinCtrlNbr.Value,
                seeded.MoverEmployeeCtrlNbr.Value,
                adminOverride: false,
                TestContext.Current.CancellationToken));

        Assert.Contains("eligible to move to the extra board", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestNoAccessByBulletinAsync_WhenExtraBoardSpecialCaseEnabledAndTargetIsExtraBoard_AllowsWhenEligible()
    {
        var seeded = await SeedNoAccessScenarioAsync(
            requireBulletinAccessAudit: false,
            targetType: StaffablePositionType.Board,
            targetBoardType: BoardType.ExtraBoard,
            targetAssigned: false,
            applyExtraBoardSpecialCase: true,
            includeLessSeniorExtraBoardEmployee: true);
        var sut = CreatePoliciesService();

        var move = await sut.RequestNoAccessByBulletinAsync(
            seeded.RailroadCtrlNbr.Value,
            seeded.CraftCtrlNbr.Value,
            seeded.BulletinCtrlNbr.Value,
            seeded.MoverEmployeeCtrlNbr.Value,
            adminOverride: false,
            TestContext.Current.CancellationToken);

        Assert.Equal(SeniorityMoveType.NoAccess, move.MoveType);
    }

    private PoliciesService CreatePoliciesService()
    {
        var notifications = new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            new RailroadResolver(),
            new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance));

        var execution = new SeniorityMoveExecutionService(
            _host.UowFactory,
            NullLogger<SeniorityMoveExecutionService>.Instance,
            notifications);

        return new PoliciesService(
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
    }

    private async Task<NoAccessScenarioSeed> SeedNoAccessScenarioAsync(
        bool requireBulletinAccessAudit,
        bool previousIncumbentIsMover = false,
        string targetType = StaffablePositionType.Crew,
        bool targetAssigned = true,
        bool blockIfEmployeeMarkedOff = true,
        bool blockIfLastVacatedIncumbent = true,
        bool applyExtraBoardSpecialCase = true,
        BoardType targetBoardType = BoardType.ExtraBoard,
        bool includeLessSeniorExtraBoardEmployee = false)
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = _host.CreateReadContext();

        var parent = Parent.Create("No Access Parent");
        ctx.Parents.Add(parent);
        await ctx.SaveChangesAsync(ct);

        var groupType = GroupType.Create("Railroad", null, true);
        ctx.Set<GroupType>().Add(groupType);
        await ctx.SaveChangesAsync(ct);

        var railroad = DynamicGroup.Create(
            groupType.CtrlNbr,
            "No Access Railroad",
            null,
            null,
            false,
            "RR",
            parentCtrlNbr: parent.CtrlNbr);
        ctx.DynamicGroups.Add(railroad);
        await ctx.SaveChangesAsync(ct);

        var workArea = DynamicGroup.Create(
            groupType.CtrlNbr,
            "No Access Work Area",
            null,
            null,
            true,
            "WA",
            parentCtrlNbr: parent.CtrlNbr,
            railroadCtrlNbr: railroad.CtrlNbr,
            timeZoneId: "UTC");
        ctx.DynamicGroups.Add(workArea);
        await ctx.SaveChangesAsync(ct);

        var craft = Craft.Create(
            parent.CtrlNbr,
            workArea.CtrlNbr,
            "Conductor",
            "Conductors",
            12,
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

        var status = EmploymentStatus.Create(workArea.CtrlNbr, "ACT", "Active", 1, "A");
        ctx.EmploymentStatuses.Add(status);
        await ctx.SaveChangesAsync(ct);

        var mover = Employee.Create(
            workArea.CtrlNbr,
            "mover",
            "E100",
            "000-00-0100",
            Gender.Male,
            Race.White,
            new DateTime(1990, 1, 1),
            DateTime.UtcNow,
            status.CtrlNbr,
            "mover@example.com",
            "admin",
            "Admin User");

        var displaced = Employee.Create(
            workArea.CtrlNbr,
            "displaced",
            "E200",
            "000-00-0200",
            Gender.Male,
            Race.White,
            new DateTime(1991, 1, 1),
            DateTime.UtcNow,
            status.CtrlNbr,
            "displaced@example.com",
            "admin",
            "Admin User");

        var lessSenior = Employee.Create(
            workArea.CtrlNbr,
            "lesssenior",
            "E300",
            "000-00-0300",
            Gender.Male,
            Race.White,
            new DateTime(1992, 1, 1),
            DateTime.UtcNow,
            status.CtrlNbr,
            "lesssenior@example.com",
            "admin",
            "Admin User");

        ctx.Employees.AddRange(mover, displaced, lessSenior);
        await ctx.SaveChangesAsync(ct);

        var roster = Roster.Create(
            craft.CtrlNbr,
            workArea.CtrlNbr,
            railroadPayrollDepartmentCtrlNbr: null,
            rosterName: "Conductor Roster",
            rosterPluralName: "Conductor Rosters",
            rosterNumber: 1);
        ctx.Rosters.Add(roster);

        var activeSeniorityState = SeniorityState.Create("Active", StateType.Active, parent.CtrlNbr.Value);
        ctx.SeniorityStates.Add(activeSeniorityState);
        await ctx.SaveChangesAsync(ct);

        var moverCurrentPosition = StaffablePosition.Create(StaffablePositionType.Crew);
        var targetPosition = StaffablePosition.Create(targetType);
        ctx.StaffablePositions.AddRange(moverCurrentPosition, targetPosition);

        var moverAssignment = PositionAssignment.Create(
            moverCurrentPosition.CtrlNbr,
            mover.CtrlNbr,
            PositionAssignmentType.Direct,
            assignedDateUtc: DateTime.UtcNow.AddDays(-14));

        ctx.PositionAssignments.Add(moverAssignment);

        if (targetAssigned)
        {
            var targetAssignment = PositionAssignment.Create(
                targetPosition.CtrlNbr,
                displaced.CtrlNbr,
                PositionAssignmentType.Direct,
                assignedDateUtc: DateTime.UtcNow.AddDays(-7));
            ctx.PositionAssignments.Add(targetAssignment);
        }

        ControlNumber vacancyTargetCtrlNbr = targetPosition.CtrlNbr;
        if (targetType == StaffablePositionType.Board)
        {
            var board = RosterBoard.Create(
                craft.CtrlNbr,
                roster.CtrlNbr,
                targetBoardType == BoardType.Hangout ? "Hangout Board" : "Extra Board",
                targetBoardType,
                RotationType.StandardRotation,
                isActive: true);
            var boardPosition = board.AddPosition(
                displaced.CtrlNbr,
                1,
                targetPosition.CtrlNbr);

            if (includeLessSeniorExtraBoardEmployee)
            {
                var helperPosition = StaffablePosition.Create(StaffablePositionType.Board);
                ctx.StaffablePositions.Add(helperPosition);
                board.AddPosition(lessSenior.CtrlNbr, 2, helperPosition.CtrlNbr);
                ctx.PositionAssignments.Add(PositionAssignment.Create(
                    helperPosition.CtrlNbr,
                    lessSenior.CtrlNbr,
                    PositionAssignmentType.Board));
            }

            ctx.Set<RosterBoard>().Add(board);
            vacancyTargetCtrlNbr = boardPosition.CtrlNbr;

            var moverBoardPosition = StaffablePosition.Create(StaffablePositionType.Board);
            ctx.StaffablePositions.Add(moverBoardPosition);

            var moverBoard = RosterBoard.Create(
                craft.CtrlNbr,
                roster.CtrlNbr,
                "Mover Board",
                BoardType.ExtraBoard,
                RotationType.StandardRotation,
                isActive: true);
            moverBoard.SetAllowSeniorityMove(true);
            moverBoard.AddPosition(mover.CtrlNbr, 1, moverBoardPosition.CtrlNbr);
            ctx.Set<RosterBoard>().Add(moverBoard);

            ctx.PositionAssignments.Remove(moverAssignment);
            ctx.PositionAssignments.Add(PositionAssignment.Create(
                moverBoardPosition.CtrlNbr,
                mover.CtrlNbr,
                PositionAssignmentType.Board));
        }

        var moverSeniority = Seniority.Create(
            rosterCtrlNbr: roster.CtrlNbr,
            employeeCtrlNbr: mover.CtrlNbr,
            lastActiveRoster: true,
            rosterDate: DateTime.UtcNow.Date.AddDays(-200),
            rank: 1,
            seniorityStateCtrlNbr: activeSeniorityState.CtrlNbr,
            canTrain: false);
        ctx.Set<Seniority>().Add(moverSeniority);

        if (includeLessSeniorExtraBoardEmployee)
        {
            var lessSeniorSeniority = Seniority.Create(
                rosterCtrlNbr: roster.CtrlNbr,
                employeeCtrlNbr: lessSenior.CtrlNbr,
                lastActiveRoster: true,
                rosterDate: DateTime.UtcNow.Date.AddDays(-10),
                rank: 99,
                seniorityStateCtrlNbr: activeSeniorityState.CtrlNbr,
                canTrain: false);
            ctx.Set<Seniority>().Add(lessSeniorSeniority);
        }

        var vacancy = PositionVacancy.Create(
            workArea.CtrlNbr,
            targetType,
            vacancyTargetCtrlNbr,
            craft.CtrlNbr,
            "NO_ACCESS_TEST",
            previousIncumbentCtrlNbr: previousIncumbentIsMover ? mover.CtrlNbr : displaced.CtrlNbr,
            targetName: "Test Crew Position");

        var bulletin = Bulletin.Create(
            vacancy.CtrlNbr,
            craft.CtrlNbr,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(8),
            DateTime.UtcNow.AddDays(1));

        ctx.Set<PositionVacancy>().Add(vacancy);
        ctx.Set<Bulletin>().Add(bulletin);

        var policy = NoAccessPolicy.Create(
            railroad.CtrlNbr,
            craft.CtrlNbr,
            isEnabled: true,
            allowEmployeeSelfRequest: true,
            requireBulletinAccessAudit: requireBulletinAccessAudit,
            blockIfOnExtendedAbsence: true,
            requirePositionCurrentlyAssigned: true,
            applyExtraBoardSpecialCase: applyExtraBoardSpecialCase,
            requireBoardAvailableForMoveOff: true,
            autoApproveNoAccess: true,
            allowAdminOverride: true,
            blockIfEmployeeMarkedOff: blockIfEmployeeMarkedOff,
            blockIfLastVacatedIncumbent: blockIfLastVacatedIncumbent,
            defaultEffectiveMode: NoAccessEffectiveDateMode.NextDay0001);

        ctx.Set<NoAccessPolicy>().Add(policy);
        await ctx.SaveChangesAsync(ct);

        return new NoAccessScenarioSeed(
            railroad.CtrlNbr,
            craft.CtrlNbr,
            bulletin.CtrlNbr,
            mover.CtrlNbr,
            targetAssigned ? displaced.CtrlNbr : ControlNumber.Create(0),
            targetPosition.CtrlNbr);
    }

    private async Task SeedBulletinAuditViewAsync(ControlNumber bulletinCtrlNbr, ControlNumber employeeCtrlNbr)
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = _host.CreateReadContext();
        var bulletin = await ctx.Set<Bulletin>().FindAsync([bulletinCtrlNbr], ct)
            ?? throw new InvalidOperationException("Bulletin not found for audit seed.");

        var viewedAt = bulletin.BidWindowOpensUtc.AddMinutes(1);
        ctx.Set<BulletinAccessAudit>().Add(BulletinAccessAudit.Create(bulletinCtrlNbr, employeeCtrlNbr, viewedAt));
        await ctx.SaveChangesAsync(ct);
    }

    private async Task SeedActiveMarkOffAsync(ControlNumber employeeCtrlNbr)
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = _host.CreateReadContext();
        var markOff = AbsenceRequest.Create(
            employeeCtrlNbr,
            DateTime.UtcNow.AddHours(-2),
            endUtc: null,
            reasonCode: "MARKOFF",
            notes: "Test markoff");
        markOff.Approve(employeeCtrlNbr);

        ctx.Set<AbsenceRequest>().Add(markOff);
        await ctx.SaveChangesAsync(ct);
    }

    private sealed record NoAccessScenarioSeed(
        ControlNumber RailroadCtrlNbr,
        ControlNumber CraftCtrlNbr,
        ControlNumber BulletinCtrlNbr,
        ControlNumber MoverEmployeeCtrlNbr,
        ControlNumber DisplacedEmployeeCtrlNbr,
        ControlNumber TargetPositionCtrlNbr);
}
