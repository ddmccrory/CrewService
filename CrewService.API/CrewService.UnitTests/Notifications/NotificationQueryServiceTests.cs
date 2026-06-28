using CrewService.Application.Notifications;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Notifications;

public class NotificationQueryServiceTests
{
    private static readonly Guid UserGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly ControlNumber RailroadCtrlNbr = ControlNumber.Create(1);

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

        var result = await service.AcknowledgeAsync(notification.CtrlNbr, TestContext.Current.CancellationToken);

        Assert.True(result.IsAcknowledged);
        Assert.Equal(0, await service.GetMyUnacknowledgedCountAsync(TestContext.Current.CancellationToken));
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
    public async Task GetMyNotifications_NoLinkedEmployee_Throws()
    {
        var (service, _) = Build(employee: null, UserGuid);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetMyNotificationsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetMyNotifications_NoAuthenticatedUser_Throws()
    {
        var (service, _) = Build(MakeEmployee(UserGuid.ToString()), Guid.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetMyNotificationsAsync(TestContext.Current.CancellationToken));
    }
}

/// <summary>Test current-user with a configurable user id.</summary>
internal sealed class StubCurrentUserService(Guid userId) : ICurrentUserService
{
    public Guid GetUserId() => userId;
    public string GetUserName() => "test-user";
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
