using CrewService.Application.BackgroundWorkers;
using CrewService.Application.TenantConfig;
using CrewService.Application.Time;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using CrewService.Presentation;
using CrewService.Presentation.Services;
using CrewService.Presentation.Services.Modules;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.UnitTests.BackgroundWorkers;

public sealed class NextProcessingEventPresentationTests
{
    [Fact]
    public async Task Bulletins_GetNextBulletinEvent_ReturnsEmpty_WhenResolverHasNoEvents()
    {
        var workArea = BuildWorkArea("WA", "UTC");
        var resolver = new FakeResolver();
        var clock = new FakeWorkAreaClock(new Dictionary<long, string> { [workArea.CtrlNbr.Value] = "UTC" });
        var provider = BuildServiceProvider(resolver, clock, [workArea]);

        var sut = new BulletinsService(provider);

        var response = await sut.GetNextBulletinEvent(
            new GetNextBulletinEventRequest { RailroadCtrlNbr = workArea.OwningRailroadCtrlNbr.Value },
            TestServerCallContextFactory.Create());

        Assert.Equal(string.Empty, response.NextEventUtc);
    }

    [Fact]
    public async Task Bulletins_GetNextBulletinEvent_ReturnsLocalizedFromEarliestWorkAreaEvent()
    {
        var workAreaOne = BuildWorkArea("WA1", "UTC");
        var workAreaTwo = BuildWorkArea("WA2", "Central Standard Time");

        var resolver = new FakeResolver();
        resolver.Set("Bulletin", workAreaOne.CtrlNbr, workAreaOne.OwningRailroadCtrlNbr, DateTime.UtcNow.AddHours(2));
        resolver.Set("Bulletin", workAreaTwo.CtrlNbr, workAreaTwo.OwningRailroadCtrlNbr, DateTime.UtcNow.AddHours(1));

        var clock = new FakeWorkAreaClock(new Dictionary<long, string>
        {
            [workAreaOne.CtrlNbr.Value] = "UTC",
            [workAreaTwo.CtrlNbr.Value] = "Central Standard Time"
        });

        var provider = BuildServiceProvider(resolver, clock, [workAreaOne, workAreaTwo]);
        var sut = new BulletinsService(provider);

        var response = await sut.GetNextBulletinEvent(
            new GetNextBulletinEventRequest(),
            TestServerCallContextFactory.Create());

        Assert.Contains("@Central Standard Time", response.NextEventUtc, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Policies_GetNextSeniorityMoveEvent_ReturnsLocalizedFromResolver()
    {
        var workArea = BuildWorkArea("WA", "Mountain Standard Time");

        var resolver = new FakeResolver();
        resolver.Set("SeniorityMove", workArea.CtrlNbr, workArea.OwningRailroadCtrlNbr, DateTime.UtcNow.AddHours(3));

        var clock = new FakeWorkAreaClock(new Dictionary<long, string> { [workArea.CtrlNbr.Value] = "Mountain Standard Time" });
        var provider = BuildServiceProvider(resolver, clock, [workArea]);

        var sut = new PoliciesService(provider);

        var response = await sut.GetNextSeniorityMoveEvent(
            new GetNextSeniorityMoveEventRequest(),
            TestServerCallContextFactory.Create());

        Assert.Contains("@Mountain Standard Time", response.NextEventLocal, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Seniority_GetNextStateChangeEventAsync_ReturnsLocalizedFromResolver()
    {
        var workArea = BuildWorkArea("WA", "Eastern Standard Time");

        var resolver = new FakeResolver();
        resolver.Set("SeniorityStateChange", workArea.CtrlNbr, workArea.OwningRailroadCtrlNbr, DateTime.UtcNow.AddHours(4));

        var clock = new FakeWorkAreaClock(new Dictionary<long, string> { [workArea.CtrlNbr.Value] = "Eastern Standard Time" });
        var provider = BuildServiceProvider(resolver, clock, [workArea]);

        var sut = new SeniorityService(
            seniorityAppService: null!,
            employeeNameService: null!,
            workAreaClock: clock,
            serviceProvider: provider);

        var response = await sut.GetNextStateChangeEventAsync(
            new GetNextStateChangeEventRequest { RailroadCtrlNbr = workArea.OwningRailroadCtrlNbr.Value },
            TestServerCallContextFactory.Create());

        Assert.Contains("@Eastern Standard Time", response.NextEventLocal, StringComparison.Ordinal);
    }

    private static DynamicGroup BuildWorkArea(string code, string timeZoneId)
    {
        var groupType = ControlNumber.Create();
        var parent = ControlNumber.Create();
        var railroad = DynamicGroup.Create(groupType, $"RR-{code}", null, null, false, code: $"R{code}", parentCtrlNbr: parent);

        return DynamicGroup.Create(
            groupType,
            $"WorkArea-{code}",
            null,
            null,
            isWorkArea: true,
            code: code,
            parentCtrlNbr: parent,
            railroadCtrlNbr: railroad.CtrlNbr,
            timeZoneId: timeZoneId);
    }

    private static ServiceProvider BuildServiceProvider(
        IBackgroundJobNextRunResolver resolver,
        IWorkAreaClock clock,
        IReadOnlyList<DynamicGroup> workAreas)
    {
        var services = new ServiceCollection();
        services.AddSingleton(resolver);
        services.AddSingleton(clock);
        services.AddSingleton<IRailroadResolver>(new RailroadResolver());
        services.AddSingleton<IDynamicGroupRepository>(new FakeDynamicGroupRepository(workAreas));
        return services.BuildServiceProvider();
    }

    private sealed class FakeResolver : IBackgroundJobNextRunResolver
    {
        private readonly Dictionary<(string WorkerType, long WorkAreaCtrlNbr, long RailroadCtrlNbr), DateTime> _values = new();

        public void Set(string workerType, ControlNumber workAreaCtrlNbr, ControlNumber railroadCtrlNbr, DateTime nextUtc)
            => _values[(workerType, workAreaCtrlNbr.Value, railroadCtrlNbr.Value)] = DateTime.SpecifyKind(nextUtc, DateTimeKind.Utc);

        public Task<BackgroundJobNextRunResult?> ResolveAsync(string workerType, ControlNumber workAreaGroupCtrlNbr, ControlNumber owningRailroadCtrlNbr, CancellationToken ct = default)
        {
            return Task.FromResult(
                _values.TryGetValue((workerType, workAreaGroupCtrlNbr.Value, owningRailroadCtrlNbr.Value), out var next)
                    ? new BackgroundJobNextRunResult(next)
                    : null);
        }
    }

    private sealed class FakeWorkAreaClock(Dictionary<long, string> timeZoneByWorkAreaCtrlNbr) : IWorkAreaClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
            => string.IsNullOrWhiteSpace(timeZoneId)
                ? TimeZoneInfo.Utc
                : TimeZoneInfo.CreateCustomTimeZone(timeZoneId, TimeSpan.Zero, timeZoneId, timeZoneId);

        public DateTimeOffset CombineLocalToUtc(DateOnly localDate, TimeOnly localTime, TimeZoneInfo? tz)
            => new(localDate.ToDateTime(localTime), TimeSpan.Zero);

        public string FormatLocalIso(DateTime utc, TimeZoneInfo? tz)
            => $"{DateTime.SpecifyKind(utc, DateTimeKind.Utc):O}@{(tz?.Id ?? "UTC")}";

        public DateTime ParseToUtc(string value, TimeZoneInfo? tz)
            => DateTime.SpecifyKind(DateTime.Parse(value), DateTimeKind.Utc);

        public Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(ControlNumber workAreaCtrlNbr, CancellationToken ct = default)
        {
            var id = timeZoneByWorkAreaCtrlNbr.TryGetValue(workAreaCtrlNbr.Value, out var tzId) ? tzId : "UTC";
            return Task.FromResult<TimeZoneInfo?>(ResolveTimeZone(id));
        }

        public Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(IOrchestrationUnitOfWork uow, ControlNumber workAreaCtrlNbr, CancellationToken ct = default)
            => GetWorkAreaTimeZoneAsync(workAreaCtrlNbr, ct);

        public Task<TimeZoneInfo?> GetCrewTimeZoneAsync(IOrchestrationUnitOfWork uow, ControlNumber crewCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<TimeZoneInfo?>(TimeZoneInfo.Utc);
    }

    private sealed class FakeDynamicGroupRepository(IReadOnlyList<DynamicGroup> workAreas) : IDynamicGroupRepository
    {
        public Task<List<DynamicGroup>> GetWorkAreasAsync(ControlNumber? railroadCtrlNbr = null)
            => Task.FromResult(railroadCtrlNbr is null
                ? workAreas.ToList()
                : workAreas.Where(w => w.OwningRailroadCtrlNbr == railroadCtrlNbr).ToList());

        public Task<DynamicGroup?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(workAreas.FirstOrDefault(w => w.CtrlNbr == ctrlNbr));

        public Task<List<DynamicGroup>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<DynamicGroup>());
        public Task<DynamicGroup?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<DynamicGroup?>(null);
        public Task AddAsync(DynamicGroup entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(DynamicGroup entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public void Add(DynamicGroup entity) { }
        public void Update(DynamicGroup entity) { }
        public void Remove(DynamicGroup entity) { }
        public Task<List<DynamicGroup>> GetByParentCtrlNbrAsync(ControlNumber? parentGroupCtrlNbr) => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs) => Task.FromResult(new List<DynamicGroup>());
        public Task<DynamicGroup?> GetByGroupTypeAndNameIncludingDeletedAsync(ControlNumber groupTypeCtrlNbr, string name) => Task.FromResult<DynamicGroup?>(null);
        public Task<List<DynamicGroup>> GetWorkAreasWithDescendantsAsync() => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetAncestorsAsync(ControlNumber groupCtrlNbr) => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetTreeAsync(ControlNumber? rootCtrlNbr = null) => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetByGroupTypeNameAsync(string typeName, ControlNumber? parentCtrlNbr = null) => Task.FromResult(new List<DynamicGroup>());
        public Task BackfillPathsAsync() => Task.CompletedTask;
    }

    private static class TestServerCallContextFactory
    {
        public static ServerCallContext Create()
        {
            var httpContext = new DefaultHttpContext();
            return TestServerCallContext.Create("/CrewService.Presentation.Tests/Test", httpContext);
        }
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        private readonly Dictionary<object, object> _userState = new();

        private TestServerCallContext(string method, HttpContext httpContext)
        {
            MethodCore = method;
            _userState["__HttpContext"] = httpContext;
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
