using CrewService.Application.Notifications;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Notifications;

public class NotificationQueryServiceTests
{
    private static readonly Guid UserGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly ControlNumber RailroadCtrlNbr = ControlNumber.Create(1);
    private static readonly ControlNumber CraftCtrlNbr = ControlNumber.Create(42);

    private static Employee MakeEmployee(string userId) =>
        Employee.Create(
            ControlNumber.Create(50), userId, "E1001", "000-00-0001",
            Gender.Male, Race.PreferNotToSay,
            new DateTime(1990, 1, 1), new DateTime(2015, 1, 1),
            ControlNumber.Create(60), "e1001@example.com", "system", "System");

    private static EmployeeNotification MakeNotification(ControlNumber employeeCtrlNbr, bool requiresAck) =>
        EmployeeNotification.Create(
            RailroadCtrlNbr, employeeCtrlNbr,
            requiresAck ? NotificationCategories.SeniorityMove : NotificationCategories.GeneralInformation,
            "Test message.", requiresAck);

    private static (NotificationQueryService Service, FakeNotificationUoW Uow) Build(Employee? employee, Guid userId)
    {
        var uow = new FakeNotificationUoW(vacancy: null, workArea: null, employee);
        var service = new NotificationQueryService(new FakeNotificationUoWFactory(uow), new StubCurrentUserService(userId));
        return (service, uow);
    }

    [Fact]
    public async Task GetMyNotifications_ReturnsEmployeeHistory()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var (service, uow) = Build(employee, UserGuid);
        uow.Notifications.Seeded.Add(MakeNotification(employee.CtrlNbr, requiresAck: true));
        uow.Notifications.Seeded.Add(MakeNotification(employee.CtrlNbr, requiresAck: false));
        uow.Notifications.Seeded.Add(MakeNotification(ControlNumber.Create(999), requiresAck: true));

        var result = await service.GetMyNotificationsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, n => Assert.Equal(employee.CtrlNbr, n.EmployeeCtrlNbr));
    }

    [Fact]
    public async Task GetMyUnacknowledgedCount_CountsOnlyOpenAckRequired()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var (service, uow) = Build(employee, UserGuid);
        uow.Notifications.Seeded.Add(MakeNotification(employee.CtrlNbr, requiresAck: true));
        uow.Notifications.Seeded.Add(MakeNotification(employee.CtrlNbr, requiresAck: false));

        var count = await service.GetMyUnacknowledgedCountAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Acknowledge_ConfirmsOwnedNotification()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var (service, uow) = Build(employee, UserGuid);
        var notification = MakeNotification(employee.CtrlNbr, requiresAck: true);
        uow.Notifications.Seeded.Add(notification);
        var projection = PositionChangeRecord.Create(
            RailroadCtrlNbr,
            employee.CtrlNbr,
            PositionChangeSourceTypes.Notification,
            sourceCtrlNbr: null,
            PositionChangeTypes.Informational,
            "Open change",
            requiresAcknowledgement: true,
            employeeNotificationCtrlNbr: notification.CtrlNbr);
        uow.PositionChanges.Seeded.Add(projection);

        var result = await service.AcknowledgeAsync(notification.CtrlNbr, TestContext.Current.CancellationToken);

        Assert.True(result.IsAcknowledged);
        Assert.False(projection.IsOpen);
        Assert.Equal(PositionChangeClosedReasons.Acknowledged, projection.ClosedReason);
        Assert.Equal(0, await service.GetMyUnacknowledgedCountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Acknowledge_BoardPlacementWithRosterBoardSubject_SchedulesHangoutAutoMove()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var (service, uow) = Build(employee, UserGuid);
        var sourceBoard = SetupHangoutAutoMoveScenario(uow, employee);

        var notification = EmployeeNotification.Create(
            RailroadCtrlNbr,
            employee.CtrlNbr,
            NotificationCategories.BoardPlacement,
            "Placed on hangout board.",
            requiresAcknowledgement: true,
            subject: NotificationSubject.Create(NotificationSubjectTypes.RosterBoard, sourceBoard.CtrlNbr));
        uow.Notifications.Seeded.Add(notification);
        var projection = PositionChangeRecord.Create(
            RailroadCtrlNbr,
            employee.CtrlNbr,
            NotificationSubjectTypes.RosterBoard,
            sourceBoard.CtrlNbr,
            PositionChangeTypes.BoardPlacement,
            "Placed on hangout board.",
            requiresAcknowledgement: true,
            employeeNotificationCtrlNbr: notification.CtrlNbr);
        uow.PositionChanges.Seeded.Add(projection);

        await service.AcknowledgeAsync(notification.CtrlNbr, TestContext.Current.CancellationToken);

        var move = Assert.Single(uow.SeniorityMoveRepo.AddedEntities);
        Assert.Equal(employee.CtrlNbr, move.EmployeeCtrlNbr);
        Assert.Equal(SeniorityMoveType.Hangout, move.MoveType);
        var targetBoard = Assert.Single(uow.RosterBoardRepo.SeededBoards, b => b.BoardType == BoardType.ExtraBoard);
        Assert.DoesNotContain(targetBoard.Positions, p => p.EmployeeCtrlNbr == employee.CtrlNbr);
        Assert.Empty(uow.StaffablePositionRepo.AddedEntities);
        Assert.False(projection.IsOpen);
        Assert.Equal(PositionChangeClosedReasons.Acknowledged, projection.ClosedReason);
    }

    [Fact]
    public async Task RecordManualAcknowledgement_BoardPlacementWithRosterBoardSubject_SchedulesHangoutAutoMove()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var (service, uow) = Build(employee, UserGuid);
        var sourceBoard = SetupHangoutAutoMoveScenario(uow, employee);

        var notification = EmployeeNotification.Create(
            RailroadCtrlNbr,
            employee.CtrlNbr,
            NotificationCategories.BoardPlacement,
            "Placed on hangout board.",
            requiresAcknowledgement: true,
            subject: NotificationSubject.Create(NotificationSubjectTypes.RosterBoard, sourceBoard.CtrlNbr));
        uow.Notifications.Seeded.Add(notification);

        await service.RecordManualAcknowledgementAsync(
            notification.CtrlNbr,
            AcknowledgementMethod.PhoneCall,
            confirmed: true,
            phoneNumber: "5551234567",
            notes: "Reached employee",
            TestContext.Current.CancellationToken);

        var move = Assert.Single(uow.SeniorityMoveRepo.AddedEntities);
        Assert.Equal(employee.CtrlNbr, move.EmployeeCtrlNbr);
        Assert.Equal(SeniorityMoveType.Hangout, move.MoveType);
        var targetBoard = Assert.Single(uow.RosterBoardRepo.SeededBoards, b => b.BoardType == BoardType.ExtraBoard);
        Assert.DoesNotContain(targetBoard.Positions, p => p.EmployeeCtrlNbr == employee.CtrlNbr);
        Assert.Empty(uow.StaffablePositionRepo.AddedEntities);
    }

    [Fact]
    public async Task RecordManualAcknowledgement_WhenAlreadyOnTargetBoard_DoesNotScheduleDuplicateHangoutMove()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var (service, uow) = Build(employee, UserGuid);
        var sourceBoard = SetupHangoutAutoMoveScenario(uow, employee);
        var targetBoard = Assert.Single(uow.RosterBoardRepo.SeededBoards, b => b.BoardType == BoardType.ExtraBoard);

        var existingTargetStaffablePosition = StaffablePosition.Create(StaffablePositionType.Board);
        var existingTargetPosition = targetBoard.AddPosition(employee.CtrlNbr, positionOrder: 1, existingTargetStaffablePosition.CtrlNbr);

        // Deterministic idempotency is based on the employee's current assignment board.
        // Make the current assignment explicitly point at the target board position.
        uow.PositionAssignmentRepo.Seeded.Clear();
        uow.PositionAssignmentRepo.Seeded.Add(PositionAssignment.Create(
            existingTargetStaffablePosition.CtrlNbr,
            employee.CtrlNbr,
            PositionAssignmentType.Board,
            assignmentSourceCtrlNbr: existingTargetPosition.CtrlNbr,
            assignedDateUtc: DateTime.UtcNow.AddMinutes(-30)));

        var notification = EmployeeNotification.Create(
            RailroadCtrlNbr,
            employee.CtrlNbr,
            NotificationCategories.BoardPlacement,
            "Placed on hangout board.",
            requiresAcknowledgement: true,
            subject: NotificationSubject.Create(NotificationSubjectTypes.RosterBoard, sourceBoard.CtrlNbr));
        uow.Notifications.Seeded.Add(notification);

        await service.RecordManualAcknowledgementAsync(
            notification.CtrlNbr,
            AcknowledgementMethod.PhoneCall,
            confirmed: true,
            phoneNumber: "5551234567",
            notes: "Reached employee",
            TestContext.Current.CancellationToken);

        Assert.Empty(uow.SeniorityMoveRepo.AddedEntities);
    }

    [Fact]
    public async Task RecordManualAcknowledgement_NewBoardPlacement_ReplacesExistingActiveHangoutMove()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var (service, uow) = Build(employee, UserGuid);
        var sourceBoard = SetupHangoutAutoMoveScenario(uow, employee);
        var targetBoard = Assert.Single(uow.RosterBoardRepo.SeededBoards, b => b.BoardType == BoardType.ExtraBoard);

        var existingMove = SeniorityMove.Create(
            RailroadCtrlNbr,
            employee.CtrlNbr,
            CraftCtrlNbr,
            targetBoard.CtrlNbr,
            displacedEmployeeCtrlNbr: null,
            daysOnCurrentPosition: 1,
            moveType: SeniorityMoveType.Hangout,
            effectiveUtc: DateTime.UtcNow.AddHours(12),
            willWork: null);
        uow.SeniorityMoveRepo.Seeded.Add(existingMove);

        var notification = EmployeeNotification.Create(
            RailroadCtrlNbr,
            employee.CtrlNbr,
            NotificationCategories.BoardPlacement,
            "Placed on hangout board.",
            requiresAcknowledgement: true,
            subject: NotificationSubject.Create(NotificationSubjectTypes.RosterBoard, sourceBoard.CtrlNbr));
        uow.Notifications.Seeded.Add(notification);

        await service.RecordManualAcknowledgementAsync(
            notification.CtrlNbr,
            AcknowledgementMethod.PhoneCall,
            confirmed: true,
            phoneNumber: "5551234567",
            notes: "Reached employee",
            TestContext.Current.CancellationToken);

        Assert.Equal(SeniorityMoveStatus.Cancelled, existingMove.Status);
        Assert.Equal("Superseded by a newer acknowledged hangout placement.", existingMove.CancellationReason);

        var replacementMove = Assert.Single(uow.SeniorityMoveRepo.AddedEntities);
        Assert.Equal(SeniorityMoveType.Hangout, replacementMove.MoveType);
        Assert.Equal(targetBoard.CtrlNbr, replacementMove.TargetPositionCtrlNbr);
    }

    [Fact]
    public async Task RecordManualAcknowledgement_ConfirmedOnAlreadyAcknowledgedBoardPlacement_StillSchedulesHangoutAutoMove()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var (service, uow) = Build(employee, UserGuid);
        var sourceBoard = SetupHangoutAutoMoveScenario(uow, employee);

        var notification = EmployeeNotification.Create(
            RailroadCtrlNbr,
            employee.CtrlNbr,
            NotificationCategories.BoardPlacement,
            "Placed on hangout board.",
            requiresAcknowledgement: true,
            subject: NotificationSubject.Create(NotificationSubjectTypes.RosterBoard, sourceBoard.CtrlNbr));
        notification.AcknowledgeElectronically("already-acknowledged");
        uow.Notifications.Seeded.Add(notification);

        await service.RecordManualAcknowledgementAsync(
            notification.CtrlNbr,
            AcknowledgementMethod.PhoneCall,
            confirmed: true,
            phoneNumber: "5551234567",
            notes: "Reached employee",
            TestContext.Current.CancellationToken);

        var move = Assert.Single(uow.SeniorityMoveRepo.AddedEntities);
        Assert.Equal(employee.CtrlNbr, move.EmployeeCtrlNbr);
        Assert.Equal(SeniorityMoveType.Hangout, move.MoveType);
    }

    [Fact]
    public async Task Acknowledge_OtherEmployeesNotification_Throws()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var (service, uow) = Build(employee, UserGuid);
        var foreign = MakeNotification(ControlNumber.Create(999), requiresAck: true);
        uow.Notifications.Seeded.Add(foreign);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AcknowledgeAsync(foreign.CtrlNbr, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Acknowledge_MissingNotification_Throws()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var (service, _) = Build(employee, UserGuid);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.AcknowledgeAsync(ControlNumber.Create(12345), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetMyNotifications_NoLinkedEmployee_ReturnsEmpty()
    {
        var (service, _) = Build(employee: null, UserGuid);

        var result = await service.GetMyNotificationsAsync(TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyNotifications_NoAuthenticatedUser_Throws()
    {
        var (service, _) = Build(MakeEmployee(UserGuid.ToString()), Guid.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetMyNotificationsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetMyUnacknowledged_NoLinkedEmployee_ReturnsEmpty()
    {
        var (service, _) = Build(employee: null, UserGuid);

        var result = await service.GetMyUnacknowledgedAsync(TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyUnacknowledgedCount_NoLinkedEmployee_ReturnsZero()
    {
        var (service, _) = Build(employee: null, UserGuid);

        var result = await service.GetMyUnacknowledgedCountAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, result);
    }

    private static RosterBoard SetupHangoutAutoMoveScenario(FakeNotificationUoW uow, Employee employee)
    {
        var sourceStaffablePosition = StaffablePosition.Create(StaffablePositionType.Board);
        var sourceBoard = RosterBoard.Create(
            CraftCtrlNbr,
            ControlNumber.Create(701),
            "Trainman Hangout",
            BoardType.Hangout);
        sourceBoard.AddPosition(employee.CtrlNbr, positionOrder: 1, sourceStaffablePosition.CtrlNbr);

        var targetBoard = RosterBoard.Create(
            CraftCtrlNbr,
            ControlNumber.Create(702),
            "Trainman Extra Board",
            BoardType.ExtraBoard);

        uow.RosterBoardRepo.SeededBoards.Add(sourceBoard);
        uow.RosterBoardRepo.SeededBoards.Add(targetBoard);
        uow.CraftOperationsPolicyRepo.SeededPolicy = CraftOperationsPolicy.Create(
            CraftCtrlNbr,
            hangoutAutoMoveEnabled: true,
            hangoutAutoMoveTargetBoardType: BoardType.ExtraBoard.ToString(),
            hangoutAutoMoveDelayHours: 48);
        uow.PositionAssignmentRepo.Seeded.Add(PositionAssignment.Create(
            sourceStaffablePosition.CtrlNbr,
            employee.CtrlNbr,
            PositionAssignmentType.Board,
            assignedDateUtc: DateTime.UtcNow.AddDays(-2)));

        return sourceBoard;
    }
}

/// <summary>Test current-user with a configurable user id.</summary>
internal sealed class StubCurrentUserService(Guid userId) : ICurrentUserService
{
    public Guid GetUserId() => userId;
    public string GetUserName() => "test-user";
    public string? GetUserIdentifier() => userId.ToString();
    public bool IsInRole(string roleName) => false;
    public long? GetParentCtrlNbr() => null;
    public void SetAuditOverride(string name) { }
}

/// <summary>Factory that always returns the same pre-seeded fake UoW.</summary>
internal sealed class FakeNotificationUoWFactory(FakeNotificationUoW uow) : IOrchestrationUnitOfWorkFactory
{
    public Task<IOrchestrationUnitOfWork> CreateAsync(
        OrchestrationUnitOfWorkOptions? options = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IOrchestrationUnitOfWork>(uow);
}
