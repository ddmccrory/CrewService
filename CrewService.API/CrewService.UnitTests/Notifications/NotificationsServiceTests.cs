using CrewService.Application.Employees;
using CrewService.Application.Authorization;
using CrewService.Application.Notifications;
using CrewService.Application.Time;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;
using CrewService.Presentation;
using CrewService.Presentation.Services.Modules;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace CrewService.UnitTests.Notifications;

public sealed class NotificationsServiceTests
{
    private static readonly ControlNumber RailroadCtrlNbr = ControlNumber.Create(1);
    private static readonly ControlNumber EmployeeCtrlNbr = ControlNumber.Create(2);

    private static Employee MakeEmployee(string userId = "ack-user") =>
        Employee.Create(
            ControlNumber.Create(50),
            userId,
            "E1001",
            "000-00-0001",
            Gender.Male,
            Race.PreferNotToSay,
            new DateTime(1990, 1, 1),
            new DateTime(2015, 1, 1),
            ControlNumber.Create(60),
            "e1001@example.com",
            "system",
            "System");

    [Fact]
    public async Task RecordManualAcknowledgement_WithValidRequest_RecordsAcknowledgement()
    {
        var employee = MakeEmployee();
        var uow = new FakeNotificationUoW(vacancy: null, workArea: null, employee: employee);
        var notification = EmployeeNotification.Create(
            RailroadCtrlNbr,
            employee.CtrlNbr,
            NotificationCategories.SeniorityMove,
            "Pending acknowledgement",
            requiresAcknowledgement: true);
        uow.Notifications.Seeded.Add(notification);

        var queryService = new NotificationQueryService(
            new FakeNotificationUoWFactory(uow),
            new FixedCurrentUserService(Guid.NewGuid(), "dispatcher.user"));

        var sut = new NotificationsService(BuildServiceProvider(queryService));
        var context = TestServerCallContextFactory.Create("Dispatcher");

        var response = await sut.RecordManualAcknowledgement(
            new RecordManualAcknowledgementRequest
            {
                CtrlNbr = notification.CtrlNbr.Value,
                Method = "PhoneCall",
                Confirmed = true,
                PhoneNumber = "5551234567",
                Notes = "Reached employee"
            },
            context);

        Assert.Equal(notification.CtrlNbr.Value, response.CtrlNbr);
        Assert.True(response.IsAcknowledged);
        Assert.Single(notification.Acknowledgements);
        Assert.Equal(AcknowledgementMethod.PhoneCall, notification.Acknowledgements[0].Method);
        Assert.True(notification.Acknowledgements[0].Confirmed);
    }

    [Fact]
    public async Task RecordManualAcknowledgement_WithInvalidCtrlNbr_ThrowsInvalidArgument()
    {
        var queryService = new NotificationQueryService(
            new FakeNotificationUoWFactory(new FakeNotificationUoW(vacancy: null, workArea: null, employee: null)),
            new FixedCurrentUserService(Guid.NewGuid(), "dispatcher.user"));
        var sut = new NotificationsService(BuildServiceProvider(queryService));
        var context = TestServerCallContextFactory.Create("Dispatcher");

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            sut.RecordManualAcknowledgement(
                new RecordManualAcknowledgementRequest
                {
                    CtrlNbr = 0,
                    Method = "PhoneCall",
                    Confirmed = true
                },
                context));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-method")]
    [InlineData("manual_ack")]
    public async Task RecordManualAcknowledgement_WithInvalidMethod_ThrowsInvalidArgument(string method)
    {
        var queryService = new NotificationQueryService(
            new FakeNotificationUoWFactory(new FakeNotificationUoW(vacancy: null, workArea: null, employee: null)),
            new FixedCurrentUserService(Guid.NewGuid(), "dispatcher.user"));
        var sut = new NotificationsService(BuildServiceProvider(queryService));
        var context = TestServerCallContextFactory.Create("Dispatcher");

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            sut.RecordManualAcknowledgement(
                new RecordManualAcknowledgementRequest
                {
                    CtrlNbr = 123,
                    Method = method,
                    Confirmed = true
                },
                context));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Theory]
    [InlineData("Worker")]
    [InlineData("Conductor")]
    public async Task ReviewEndpoints_UnauthorizedRole_ThrowsPermissionDenied(string role)
    {
        var queryService = new NotificationQueryService(
            new FakeNotificationUoWFactory(new FakeNotificationUoW(vacancy: null, workArea: null, employee: null)),
            new FixedCurrentUserService(Guid.NewGuid(), "review.user"));
        var sut = new NotificationsService(BuildServiceProvider(queryService));
        var context = TestServerCallContextFactory.Create(userRoles: [role]);

        var railroadEx = await Assert.ThrowsAsync<RpcException>(() =>
            sut.GetRailroadNotifications(new RailroadNotificationsRequest { RailroadCtrlNbr = 1 }, context));
        Assert.Equal(StatusCode.PermissionDenied, railroadEx.StatusCode);

        var countEx = await Assert.ThrowsAsync<RpcException>(() =>
            sut.GetRailroadUnacknowledgedCount(new RailroadNotificationsRequest { RailroadCtrlNbr = 1 }, context));
        Assert.Equal(StatusCode.PermissionDenied, countEx.StatusCode);

        var employeeEx = await Assert.ThrowsAsync<RpcException>(() =>
            sut.GetEmployeeNotifications(new EmployeeNotificationsRequest { EmployeeCtrlNbr = 1 }, context));
        Assert.Equal(StatusCode.PermissionDenied, employeeEx.StatusCode);
    }

    [Theory]
    [InlineData(Roles.SystemAdmin)]
    [InlineData(Roles.ParentAdmin)]
    [InlineData(Roles.RailroadAdmin)]
    [InlineData("CrewManager")]
    [InlineData("Dispatcher")]
    public async Task ReviewEndpoints_AllowedRoles_ReturnEmptyForInvalidRequest(string role)
    {
        var queryService = new NotificationQueryService(
            new FakeNotificationUoWFactory(new FakeNotificationUoW(vacancy: null, workArea: null, employee: null)),
            new FixedCurrentUserService(Guid.NewGuid(), "review.user"));
        var sut = new NotificationsService(BuildServiceProvider(queryService));
        var context = TestServerCallContextFactory.Create(userRoles: [role]);

        var railroad = await sut.GetRailroadNotifications(new RailroadNotificationsRequest { RailroadCtrlNbr = 0 }, context);
        Assert.Empty(railroad.Notifications);

        var count = await sut.GetRailroadUnacknowledgedCount(new RailroadNotificationsRequest { RailroadCtrlNbr = 0 }, context);
        Assert.Equal(0, count.Count);

        var employee = await sut.GetEmployeeNotifications(new EmployeeNotificationsRequest { EmployeeCtrlNbr = 0 }, context);
        Assert.Empty(employee.Notifications);
    }

    private static ServiceProvider BuildServiceProvider(NotificationQueryService? queryService = null)
    {
        var services = new ServiceCollection();
        if (queryService is not null)
            services.AddSingleton(queryService);
        services.AddSingleton<IWorkAreaClock, StubWorkAreaClock>();
        services.AddSingleton<IRequestActorContextResolver, StubRequestActorContextResolver>();
        services.AddSingleton<IRequestActorContextPolicy, RequestActorContextPolicy>();
        return services.BuildServiceProvider();
    }

    private sealed class StubRequestActorContextResolver : IRequestActorContextResolver
    {
        public Task<RequestActorContext> ResolveAsync(
            long? requestedEmployeeCtrlNbr = null,
            long? parentCtrlNbr = null,
            long? railroadCtrlNbr = null,
            long? workAreaCtrlNbr = null,
            CancellationToken ct = default)
        {
            var context = new RequestActorContext(
                CurrentUserId: "test-user",
                CurrentEmployeeCtrlNbr: null,
                RequestedEmployeeCtrlNbr: requestedEmployeeCtrlNbr,
                IsLinkedEmployee: false,
                IsSelfEmployeeContext: false,
                IsActingOnBehalfOfEmployee: requestedEmployeeCtrlNbr.HasValue,
                ParentCtrlNbr: parentCtrlNbr,
                RailroadCtrlNbr: railroadCtrlNbr,
                WorkAreaCtrlNbr: workAreaCtrlNbr);

            return Task.FromResult(context);
        }
    }

    private sealed class StubWorkAreaClock : IWorkAreaClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public TimeZoneInfo? ResolveTimeZone(string? timeZoneId) => TimeZoneInfo.Utc;
        public DateTimeOffset CombineLocalToUtc(DateOnly localDate, TimeOnly localTime, TimeZoneInfo? tz)
            => new(localDate.ToDateTime(localTime, DateTimeKind.Utc));
        public string FormatLocalIso(DateTime utc, TimeZoneInfo? tz) => utc.ToString("O");
        public DateTime ParseToUtc(string value, TimeZoneInfo? tz) => DateTime.Parse(value).ToUniversalTime();
        public Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(ControlNumber workAreaCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<TimeZoneInfo?>(TimeZoneInfo.Utc);
        public Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(IOrchestrationUnitOfWork uow, ControlNumber workAreaCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<TimeZoneInfo?>(TimeZoneInfo.Utc);
        public Task<TimeZoneInfo?> GetCrewTimeZoneAsync(IOrchestrationUnitOfWork uow, ControlNumber crewCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<TimeZoneInfo?>(TimeZoneInfo.Utc);
    }

    private sealed class FixedCurrentUserService(Guid userId, string userName) : ICurrentUserService
    {
        public Guid GetUserId() => userId;
        public string GetUserName() => userName;
        public string? GetUserIdentifier() => userId.ToString();
        public bool IsInRole(string roleName) => false;
        public long? GetParentCtrlNbr() => null;
        public void SetAuditOverride(string name) { }
    }

    private static class TestServerCallContextFactory
    {
        public static ServerCallContext Create(params string[] userRoles)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                userRoles.Select(r => new Claim(ClaimTypes.Role, r)),
                authenticationType: "TestAuth"));

            return TestServerCallContext.Create("/CrewService.Presentation.NotificationsSrvc/Test", httpContext);
        }
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        private readonly HttpContext _httpContext;
        private readonly Dictionary<object, object> _userState = new();

        private TestServerCallContext(string method, HttpContext httpContext)
        {
            _httpContext = httpContext;
            MethodCore = method;
            _userState["__HttpContext"] = _httpContext;
        }

        public static ServerCallContext Create(string method, HttpContext httpContext) => new TestServerCallContext(method, httpContext);

        protected override string MethodCore { get; }
        protected override string HostCore => "localhost";
        protected override string PeerCore => "peer";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => [];
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore { get; } = [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new("anonymous", new Dictionary<string, List<AuthProperty>>());
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
        protected override IDictionary<object, object> UserStateCore => _userState;
    }
}
