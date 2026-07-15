using CrewService.Application.Notifications;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;

namespace CrewService.UnitTests.Notifications;

public sealed class NotificationAcknowledgementEnforcerTests
{
    private static readonly Guid UserGuid = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly ControlNumber RailroadCtrlNbr = ControlNumber.Create(1);

    private static Employee MakeEmployee(string userId) =>
        Employee.Create(
            ControlNumber.Create(77), userId, "E2001", "000-00-0002",
            Gender.Female, Race.PreferNotToSay,
            new DateTime(1991, 2, 2), new DateTime(2016, 2, 2),
            ControlNumber.Create(60), "e2001@example.com", "system", "System");

    private static EmployeeNotification MakeOpenAckNotification(ControlNumber employeeCtrlNbr) =>
        EmployeeNotification.Create(
            RailroadCtrlNbr,
            employeeCtrlNbr,
            NotificationCategories.BoardPlacement,
            "Ack required.",
            requiresAcknowledgement: true);

    private static NotificationAcknowledgementEnforcer BuildEnforcer(Employee? employee, Guid userGuid, params EmployeeNotification[] notifications)
    {
        var uow = new FakeNotificationUoW(vacancy: null, workArea: null, employee);
        foreach (var n in notifications)
            uow.Notifications.Seeded.Add(n);

        var query = new NotificationQueryService(new FakeNotificationUoWFactory(uow), new StubCurrentUserService(userGuid));
        return new NotificationAcknowledgementEnforcer(query, new StubCurrentUserService(userGuid));
    }

    [Fact]
    public async Task GetBlockingOpenCountAsync_ExemptNotificationsMethod_ReturnsZero()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var enforcer = BuildEnforcer(employee, UserGuid, MakeOpenAckNotification(employee.CtrlNbr));

        var count = await enforcer.GetBlockingOpenCountAsync("/CrewService.Presentation.NotificationsSrvc/GetMyUnacknowledged", TestContext.Current.CancellationToken);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetBlockingOpenCountAsync_ExemptAuthorizationMethod_ReturnsZero()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var enforcer = BuildEnforcer(employee, UserGuid, MakeOpenAckNotification(employee.CtrlNbr));

        var count = await enforcer.GetBlockingOpenCountAsync("/authorization.AuthorizationSrvc/GetEffectivePermissions", TestContext.Current.CancellationToken);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetBlockingOpenCountAsync_ExemptEmployeeLookupMethod_ReturnsZero()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var enforcer = BuildEnforcer(employee, UserGuid, MakeOpenAckNotification(employee.CtrlNbr));

        var count = await enforcer.GetBlockingOpenCountAsync("/CrewService.Presentation.EmployeeSrvc/GetEmployeeByNumber", TestContext.Current.CancellationToken);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetBlockingOpenCountAsync_NonExemptMethod_ReturnsOpenCount()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var enforcer = BuildEnforcer(employee, UserGuid, MakeOpenAckNotification(employee.CtrlNbr));

        var count = await enforcer.GetBlockingOpenCountAsync("/CrewService.Presentation.EmployeeSrvc/GetEmployee", TestContext.Current.CancellationToken);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetBlockingOpenCountAsync_NoAuthenticatedUser_ReturnsZero()
    {
        var employee = MakeEmployee(UserGuid.ToString());
        var enforcer = BuildEnforcer(employee, Guid.Empty, MakeOpenAckNotification(employee.CtrlNbr));

        var count = await enforcer.GetBlockingOpenCountAsync("/CrewService.Presentation.EmployeeSrvc/GetEmployee", TestContext.Current.CancellationToken);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetBlockingOpenCountAsync_NoLinkedEmployee_ReturnsZero()
    {
        var enforcer = BuildEnforcer(employee: null, userGuid: UserGuid);

        var count = await enforcer.GetBlockingOpenCountAsync("/CrewService.Presentation.EmployeeSrvc/GetEmployee", TestContext.Current.CancellationToken);

        Assert.Equal(0, count);
    }
}
