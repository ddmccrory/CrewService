using CrewService.Application.Absence;
using CrewService.Application.AbsenceVacancy;
using CrewService.Application.Authorization;
using CrewService.Application.BackgroundWorkers;
using CrewService.Application.TenantConfig;
using CrewService.Application.Time;
using CrewService.Application.Notifications;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.Infrastructure.Models.UserAccount;
using CrewService.Persistance.Data;
using CrewService.Persistance.UnitOfWork;
using CrewService.Presentation;
using CrewService.Presentation.Services.Modules;
using CrewService.UnitTests.Fixtures;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrewService.UnitTests.Absence;

public sealed class AbsenceServiceFiltersTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CrewServiceDbContext _crewContext;
    private readonly UserAccessDbContext _userContext;
    private readonly ICurrentUserService _currentUser = new TestCurrentUserService();

    public AbsenceServiceFiltersTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var crewOptions = new DbContextOptionsBuilder<CrewServiceDbContext>()
            .UseSqlite(_connection)
            .Options;
        _crewContext = new CrewServiceDbContext(crewOptions, _currentUser, new TestFieldEncryptor());
        _crewContext.Database.EnsureCreated();

        var userOptions = new DbContextOptionsBuilder<UserAccessDbContext>()
            .UseSqlite(_connection)
            .Options;
        _userContext = new UserAccessDbContext(userOptions);
    }

    [Fact]
    public async Task GetScheduledAbsences_CurrentMonthOnly_IncludesApprovedOutsideNext24Hours()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedRailroadAndEmployeesAsync(ct);

        await using (var seed = CreateReadContext())
        {
            var inMonth = AbsenceRequest.Create(seeded.EmployeeA.CtrlNbr, new DateTime(2026, 7, 30, 19, 1, 0, DateTimeKind.Utc), null, "MARKOFF");
            inMonth.Approve(seeded.EmployeeA.CtrlNbr);

            var outOfMonth = AbsenceRequest.Create(seeded.EmployeeA.CtrlNbr, new DateTime(2026, 8, 1, 0, 1, 0, DateTimeKind.Utc), null, "MARKOFF");
            outOfMonth.Approve(seeded.EmployeeA.CtrlNbr);

            seed.Set<AbsenceRequest>().AddRange(inMonth, outOfMonth);
            await seed.SaveChangesAsync(ct);
        }

        var service = BuildService(
            utcNow: new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero),
            railroadCtrlNbr: seeded.Railroad.CtrlNbr);

        var response = await service.GetScheduledAbsences(
            new GetScheduledAbsencesMsg
            {
                EmployeeCtrlNbr = seeded.EmployeeA.CtrlNbr.Value,
                CurrentMonthOnly = true
            },
            TestServerCallContextFactory.Create());

        var result = Assert.Single(response.Requests);
        Assert.Equal(seeded.EmployeeA.CtrlNbr.Value, result.EmployeeCtrlNbr);
        Assert.Equal(new DateTime(2026, 7, 30, 19, 1, 0, DateTimeKind.Utc), result.StartUtc.ToDateTime());
    }

    [Fact]
    public async Task GetScheduledAbsences_CurrentMonthOnly_UsesWorkAreaLocalMonthBoundary()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedRailroadAndEmployeesAsync(ct, includeWorkArea: true);
        Assert.NotNull(seeded.WorkArea);

        await using (var seed = CreateReadContext())
        {
            // 2026-07-31 23:30 local (UTC-05) => 2026-08-01 04:30Z, should be included for July local month.
            var localJuly = AbsenceRequest.Create(seeded.EmployeeA.CtrlNbr, new DateTime(2026, 8, 1, 4, 30, 0, DateTimeKind.Utc), null, "MARKOFF");
            localJuly.Approve(seeded.EmployeeA.CtrlNbr);

            // 2026-08-01 00:30 local (UTC-05) => 2026-08-01 05:30Z, should be excluded for July local month.
            var localAugust = AbsenceRequest.Create(seeded.EmployeeA.CtrlNbr, new DateTime(2026, 8, 1, 5, 30, 0, DateTimeKind.Utc), null, "MARKOFF");
            localAugust.Approve(seeded.EmployeeA.CtrlNbr);

            seed.Set<AbsenceRequest>().AddRange(localJuly, localAugust);
            await seed.SaveChangesAsync(ct);
        }

        var service = BuildService(
            utcNow: new DateTimeOffset(2026, 8, 1, 1, 0, 0, TimeSpan.Zero),
            railroadCtrlNbr: seeded.Railroad.CtrlNbr,
            workAreaTimeZones: new Dictionary<long, TimeZoneInfo>
            {
                [seeded.WorkArea!.CtrlNbr.Value] = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time")
            });

        var response = await service.GetScheduledAbsences(
            new GetScheduledAbsencesMsg
            {
                WorkAreaGroupCtrlNbr = seeded.WorkArea.CtrlNbr.Value,
                EmployeeCtrlNbr = seeded.EmployeeA.CtrlNbr.Value,
                CurrentMonthOnly = true
            },
            TestServerCallContextFactory.Create());

        var result = Assert.Single(response.Requests);
        Assert.Equal(new DateTime(2026, 8, 1, 4, 30, 0, DateTimeKind.Utc), result.StartUtc.ToDateTime());
    }

    [Fact]
    public async Task GetScheduledAbsences_AppliesEmployeeFilter()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedRailroadAndEmployeesAsync(ct);

        await using (var seed = CreateReadContext())
        {
            var empA = AbsenceRequest.Create(seeded.EmployeeA.CtrlNbr, new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc), null, "MARKOFF");
            empA.Approve(seeded.EmployeeA.CtrlNbr);

            var empB = AbsenceRequest.Create(seeded.EmployeeB.CtrlNbr, new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc), null, "MARKOFF");
            empB.Approve(seeded.EmployeeB.CtrlNbr);

            seed.Set<AbsenceRequest>().AddRange(empA, empB);
            await seed.SaveChangesAsync(ct);
        }

        var service = BuildService(
            utcNow: new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero),
            railroadCtrlNbr: seeded.Railroad.CtrlNbr);

        var response = await service.GetScheduledAbsences(
            new GetScheduledAbsencesMsg
            {
                EmployeeCtrlNbr = seeded.EmployeeA.CtrlNbr.Value,
                CurrentMonthOnly = true
            },
            TestServerCallContextFactory.Create());

        var result = Assert.Single(response.Requests);
        Assert.Equal(seeded.EmployeeA.CtrlNbr.Value, result.EmployeeCtrlNbr);
    }

    [Fact]
    public async Task GetOpenAbsences_AppliesEmployeeFilter()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedRailroadAndEmployeesAsync(ct);

        await using (var seed = CreateReadContext())
        {
            var empA = AbsenceRequest.Create(seeded.EmployeeA.CtrlNbr, new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc), null, "MARKOFF");
            empA.Approve(seeded.EmployeeA.CtrlNbr);
            empA.Exercise(new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc));

            var empB = AbsenceRequest.Create(seeded.EmployeeB.CtrlNbr, new DateTime(2026, 7, 20, 13, 0, 0, DateTimeKind.Utc), null, "MARKOFF");
            empB.Approve(seeded.EmployeeB.CtrlNbr);
            empB.Exercise(new DateTime(2026, 7, 20, 13, 0, 0, DateTimeKind.Utc));

            seed.Set<AbsenceRequest>().AddRange(empA, empB);
            await seed.SaveChangesAsync(ct);
        }

        var service = BuildService(
            utcNow: new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero),
            railroadCtrlNbr: seeded.Railroad.CtrlNbr);

        var response = await service.GetOpenAbsences(
            new GetOpenAbsencesMsg
            {
                RangeStartUtc = Timestamp.FromDateTime(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)),
                RangeEndUtc = Timestamp.FromDateTime(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)),
                EmployeeCtrlNbr = seeded.EmployeeA.CtrlNbr.Value
            },
            TestServerCallContextFactory.Create());

        var result = Assert.Single(response.Requests);
        Assert.Equal(seeded.EmployeeA.CtrlNbr.Value, result.EmployeeCtrlNbr);
    }

    [Fact]
    public async Task GetAbsenceRequests_AppliesEmployeeFilter()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedRailroadAndEmployeesAsync(ct);

        await using (var seed = CreateReadContext())
        {
            var empA = AbsenceRequest.Create(seeded.EmployeeA.CtrlNbr, new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc), null, "MARKOFF");
            var empB = AbsenceRequest.Create(seeded.EmployeeB.CtrlNbr, new DateTime(2026, 7, 30, 13, 0, 0, DateTimeKind.Utc), null, "MARKOFF");

            seed.Set<AbsenceRequest>().AddRange(empA, empB);
            await seed.SaveChangesAsync(ct);
        }

        var service = BuildService(
            utcNow: new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero),
            railroadCtrlNbr: seeded.Railroad.CtrlNbr);

        var response = await service.GetAbsenceRequests(
            new GetAbsenceRequestsMsg
            {
                RequestDateUtc = Timestamp.FromDateTime(new DateTime(2026, 7, 30, 0, 0, 0, DateTimeKind.Utc)),
                IncludeAllStatuses = true,
                EmployeeCtrlNbr = seeded.EmployeeA.CtrlNbr.Value
            },
            TestServerCallContextFactory.Create());

        var result = Assert.Single(response.Requests);
        Assert.Equal(seeded.EmployeeA.CtrlNbr.Value, result.EmployeeCtrlNbr);
    }

    [Fact]
    public void AbsenceProtoContracts_RoundTrip_NewFilterFields()
    {
        var scheduled = new GetScheduledAbsencesMsg
        {
            EmployeeCtrlNbr = 10,
            CurrentMonthOnly = true
        };
        var scheduledRoundTrip = GetScheduledAbsencesMsg.Parser.ParseFrom(scheduled.ToByteArray());
        Assert.True(scheduledRoundTrip.HasEmployeeCtrlNbr);
        Assert.Equal(10L, scheduledRoundTrip.EmployeeCtrlNbr);
        Assert.True(scheduledRoundTrip.CurrentMonthOnly);

        var requests = new GetAbsenceRequestsMsg
        {
            RequestDateUtc = Timestamp.FromDateTime(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)),
            IncludeAllStatuses = true,
            EmployeeCtrlNbr = 20
        };
        var requestsRoundTrip = GetAbsenceRequestsMsg.Parser.ParseFrom(requests.ToByteArray());
        Assert.True(requestsRoundTrip.HasEmployeeCtrlNbr);
        Assert.Equal(20L, requestsRoundTrip.EmployeeCtrlNbr);

        var open = new GetOpenAbsencesMsg
        {
            RangeStartUtc = Timestamp.FromDateTime(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)),
            RangeEndUtc = Timestamp.FromDateTime(new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc)),
            EmployeeCtrlNbr = 30
        };
        var openRoundTrip = GetOpenAbsencesMsg.Parser.ParseFrom(open.ToByteArray());
        Assert.True(openRoundTrip.HasEmployeeCtrlNbr);
        Assert.Equal(30L, openRoundTrip.EmployeeCtrlNbr);
    }

    private AbsenceService BuildService(
        DateTimeOffset utcNow,
        ControlNumber railroadCtrlNbr,
        Dictionary<long, TimeZoneInfo>? workAreaTimeZones = null)
    {
        var actorContextResolver = new FixedActorContextResolver(railroadCtrlNbr.Value);
        var workAreaClock = new FixedWorkAreaClock(utcNow, workAreaTimeZones);

        var factory = new OrchestrationUnitOfWorkFactory(
            _connection,
            _crewContext,
            _userContext,
            _currentUser,
            NullLoggerFactory.Instance);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IRequestActorContextResolver>(actorContextResolver);
        services.AddSingleton<IWorkAreaClock>(workAreaClock);
        services.AddSingleton<IOrchestrationUnitOfWorkFactory>(factory);
        services.AddSingleton<IRailroadResolver, NullRailroadResolver>();
        services.AddSingleton<IAbsenceCodeRepository, NullAbsenceCodeRepository>();
        services.AddSingleton<IDepartmentAbsenceRequestWindowPolicyRepository, NullDepartmentAbsenceRequestWindowPolicyRepository>();
        services.AddSingleton<IAbsenceRequestWaitListRecordRepository, NullAbsenceRequestWaitListRecordRepository>();
        services.AddSingleton<IDepartmentAbsenceWaitListPolicyRepository, NullDepartmentAbsenceWaitListPolicyRepository>();
        services.AddSingleton<IAbsenceWaitListAllowancePolicyRepository, NullAbsenceWaitListAllowancePolicyRepository>();
        services.AddSingleton<IAbsenceApprovalPolicyResolver, StaticAbsenceApprovalPolicyResolver>();
        services.AddSingleton<IAbsenceMarkOffSignal, AbsenceMarkOffSignal>();
        services.AddSingleton<IAutoMarkUpSignal, AutoMarkUpSignal>();
        services.AddSingleton<IWaitListReassignmentSignal, WaitListReassignmentSignal>();
        services.AddSingleton(_ => new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance));
        services.AddSingleton(sp => new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            sp.GetRequiredService<IRailroadResolver>(),
            sp.GetRequiredService<NotificationTypeConfigResolver>(),
            userAccounts: null,
            clock: sp.GetRequiredService<IWorkAreaClock>()));
        services.AddTransient<AbsenceStartProposalService>();
        services.AddTransient<AbsenceRequestService>();

        return new AbsenceService(services.BuildServiceProvider());
    }

    private async Task<(DynamicGroup Railroad, DynamicGroup? WorkArea, Employee EmployeeA, Employee EmployeeB)> SeedRailroadAndEmployeesAsync(
        CancellationToken ct,
        bool includeWorkArea = false)
    {
        await using var context = CreateReadContext();

        var parent = Parent.Create("Test Parent");
        context.Parents.Add(parent);

        var groupType = GroupType.Create("Railroad", "Railroad", isWorkArea: true);
        context.Set<GroupType>().Add(groupType);
        await context.SaveChangesAsync(ct);

        var railroad = DynamicGroup.Create(
            groupType.CtrlNbr,
            "Test Railroad",
            parentGroupCtrlNbr: null,
            path: null,
            isWorkArea: false,
            code: "TRR",
            parentCtrlNbr: parent.CtrlNbr);
        context.DynamicGroups.Add(railroad);

        DynamicGroup? workArea = null;
        if (includeWorkArea)
        {
            workArea = DynamicGroup.Create(
                groupType.CtrlNbr,
                "Test Work Area",
                parentGroupCtrlNbr: railroad.CtrlNbr,
                path: null,
                isWorkArea: true,
                code: "TWA",
                parentCtrlNbr: parent.CtrlNbr,
                railroadCtrlNbr: railroad.CtrlNbr,
                timeZoneId: "Central Standard Time");
            context.DynamicGroups.Add(workArea);
        }

        await context.SaveChangesAsync(ct);

        var status = EmploymentStatus.Create(railroad.CtrlNbr, "ACT", "Active", 1, "A");
        context.EmploymentStatuses.Add(status);
        await context.SaveChangesAsync(ct);

        var employeeA = CreateEmployee(railroad.CtrlNbr, status.CtrlNbr, "EMP001", "emp001", "000-00-1001");
        var employeeB = CreateEmployee(railroad.CtrlNbr, status.CtrlNbr, "EMP002", "emp002", "000-00-1002");
        context.Employees.AddRange(employeeA, employeeB);
        await context.SaveChangesAsync(ct);

        return (railroad, workArea, employeeA, employeeB);
    }

    private CrewServiceDbContext CreateReadContext()
    {
        var options = new DbContextOptionsBuilder<CrewServiceDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new CrewServiceDbContext(options, _currentUser, new TestFieldEncryptor());
    }

    private static Employee CreateEmployee(
        ControlNumber clientCtrlNbr,
        ControlNumber employmentStatusCtrlNbr,
        string employeeNumber,
        string userId,
        string ssn)
    {
        return Employee.Create(
            clientCtrlNbr,
            userId,
            employeeNumber,
            ssn,
            Gender.Female,
            Race.White,
            new DateTime(1990, 1, 1),
            DateTime.UtcNow.Date,
            employmentStatusCtrlNbr,
            $"{userId}@example.com",
            "system",
            "System");
    }

    public void Dispose()
    {
        _crewContext.Dispose();
        _userContext.Dispose();
        _connection.Dispose();
    }

    private sealed class FixedActorContextResolver(long railroadCtrlNbr) : IRequestActorContextResolver
    {
        public Task<RequestActorContext> ResolveAsync(
            long? requestedEmployeeCtrlNbr = null,
            long? parentCtrlNbr = null,
            long? railroadCtrlNbrOverride = null,
            long? workAreaCtrlNbr = null,
            CancellationToken ct = default)
        {
            var context = new RequestActorContext(
                CurrentUserId: "00000000-0000-0000-0000-000000000001",
                CurrentEmployeeCtrlNbr: null,
                RequestedEmployeeCtrlNbr: requestedEmployeeCtrlNbr,
                IsLinkedEmployee: false,
                IsSelfEmployeeContext: false,
                IsActingOnBehalfOfEmployee: requestedEmployeeCtrlNbr.HasValue,
                ParentCtrlNbr: parentCtrlNbr,
                RailroadCtrlNbr: railroadCtrlNbrOverride ?? railroadCtrlNbr,
                WorkAreaCtrlNbr: workAreaCtrlNbr);

            return Task.FromResult(context);
        }
    }

    private sealed class FixedWorkAreaClock(
        DateTimeOffset utcNow,
        IReadOnlyDictionary<long, TimeZoneInfo>? workAreaTimeZones = null) : IWorkAreaClock
    {
        public DateTimeOffset UtcNow => utcNow;

        public TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
            => string.IsNullOrWhiteSpace(timeZoneId) ? null : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        public DateTimeOffset CombineLocalToUtc(DateOnly localDate, TimeOnly localTime, TimeZoneInfo? tz)
        {
            var local = localDate.ToDateTime(localTime, DateTimeKind.Unspecified);
            var utc = tz is null ? DateTime.SpecifyKind(local, DateTimeKind.Utc) : TimeZoneInfo.ConvertTimeToUtc(local, tz);
            return new DateTimeOffset(utc, TimeSpan.Zero);
        }

        public string FormatLocalIso(DateTime utc, TimeZoneInfo? tz)
        {
            var utcKind = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            var local = tz is null
                ? new DateTimeOffset(utcKind, TimeSpan.Zero)
                : TimeZoneInfo.ConvertTime(new DateTimeOffset(utcKind, TimeSpan.Zero), tz);
            return local.ToString("o");
        }

        public DateTime ParseToUtc(string value, TimeZoneInfo? tz)
        {
            if (DateTimeOffset.TryParse(value, out var dto))
                return dto.UtcDateTime;

            var unspecified = DateTime.SpecifyKind(DateTime.Parse(value), DateTimeKind.Unspecified);
            return tz is null ? DateTime.SpecifyKind(unspecified, DateTimeKind.Utc) : TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
        }

        public Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(ControlNumber workAreaCtrlNbr, CancellationToken ct = default)
        {
            if (workAreaTimeZones is not null && workAreaTimeZones.TryGetValue(workAreaCtrlNbr.Value, out var tz))
                return Task.FromResult<TimeZoneInfo?>(tz);

            return Task.FromResult<TimeZoneInfo?>(null);
        }

        public Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(IOrchestrationUnitOfWork uow, ControlNumber workAreaCtrlNbr, CancellationToken ct = default)
            => GetWorkAreaTimeZoneAsync(workAreaCtrlNbr, ct);

        public Task<TimeZoneInfo?> GetCrewTimeZoneAsync(IOrchestrationUnitOfWork uow, ControlNumber crewCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<TimeZoneInfo?>(null);
    }

    private sealed class NullRailroadResolver : IRailroadResolver
    {
        public Task<ControlNumber?> ResolveFromWorkAreaAsync(IOrchestrationUnitOfWork uow, ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<ControlNumber?>(null);

        public ControlNumber? ResolveFromGroup(DynamicGroup? group) => null;
    }

    private sealed class NullAbsenceCodeRepository : IAbsenceCodeRepository
    {
        public Task<List<AbsenceCode>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default) => Task.FromResult(new List<AbsenceCode>());
        public Task<AbsenceCodeCraftOverride?> GetOverrideAsync(ControlNumber absenceCodeCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default) => Task.FromResult<AbsenceCodeCraftOverride?>(null);
        public Task<List<AbsenceCode>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<AbsenceCode>());
        public Task<List<AbsenceCode>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<AbsenceCode>());
        public Task<AbsenceCode?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<AbsenceCode?>(null);
        public Task<AbsenceCode?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<AbsenceCode?>(null);
        public Task AddAsync(AbsenceCode entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(AbsenceCode entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public void Add(AbsenceCode entity) { }
        public void Update(AbsenceCode entity) { }
        public void Remove(AbsenceCode entity) { }
    }

    private sealed class NullAbsenceRequestWaitListRecordRepository : IAbsenceRequestWaitListRecordRepository
    {
        public Task<List<AbsenceRequestWaitListRecord>> GetPendingByDateAsync(DateTime requestDateUtc, string waitListType, CancellationToken ct = default)
            => Task.FromResult(new List<AbsenceRequestWaitListRecord>());

        public Task<List<AbsenceRequestWaitListRecord>> GetPendingByDateRangeAsync(DateTime rangeStartUtc, DateTime rangeEndUtc, string waitListType, CancellationToken ct = default)
            => Task.FromResult(new List<AbsenceRequestWaitListRecord>());

        public Task<List<AbsenceRequestWaitListRecord>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<AbsenceRequestWaitListRecord>());
        public Task<List<AbsenceRequestWaitListRecord>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<AbsenceRequestWaitListRecord>());
        public Task<AbsenceRequestWaitListRecord?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<AbsenceRequestWaitListRecord?>(null);
        public Task<AbsenceRequestWaitListRecord?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<AbsenceRequestWaitListRecord?>(null);
        public Task AddAsync(AbsenceRequestWaitListRecord entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(AbsenceRequestWaitListRecord entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public void Add(AbsenceRequestWaitListRecord entity) { }
        public void Update(AbsenceRequestWaitListRecord entity) { }
        public void Remove(AbsenceRequestWaitListRecord entity) { }
    }

    private sealed class NullDepartmentAbsenceRequestWindowPolicyRepository : IDepartmentAbsenceRequestWindowPolicyRepository
    {
        public Task<DepartmentAbsenceRequestWindowPolicy?> GetByDepartmentAsync(ControlNumber departmentCtrlNbr)
            => Task.FromResult<DepartmentAbsenceRequestWindowPolicy?>(null);

        public Task<List<DepartmentAbsenceRequestWindowPolicy>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<DepartmentAbsenceRequestWindowPolicy>());
        public Task<List<DepartmentAbsenceRequestWindowPolicy>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<DepartmentAbsenceRequestWindowPolicy>());
        public Task<DepartmentAbsenceRequestWindowPolicy?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<DepartmentAbsenceRequestWindowPolicy?>(null);
        public Task<DepartmentAbsenceRequestWindowPolicy?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<DepartmentAbsenceRequestWindowPolicy?>(null);
        public Task AddAsync(DepartmentAbsenceRequestWindowPolicy entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(DepartmentAbsenceRequestWindowPolicy entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public void Add(DepartmentAbsenceRequestWindowPolicy entity) { }
        public void Update(DepartmentAbsenceRequestWindowPolicy entity) { }
        public void Remove(DepartmentAbsenceRequestWindowPolicy entity) { }
    }

    private sealed class NullDepartmentAbsenceWaitListPolicyRepository : IDepartmentAbsenceWaitListPolicyRepository
    {
        public Task<DepartmentAbsenceWaitListPolicy?> GetByDepartmentAsync(ControlNumber departmentCtrlNbr)
            => Task.FromResult<DepartmentAbsenceWaitListPolicy?>(null);

        public Task<List<DepartmentAbsenceWaitListPolicy>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<DepartmentAbsenceWaitListPolicy>());
        public Task<List<DepartmentAbsenceWaitListPolicy>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<DepartmentAbsenceWaitListPolicy>());
        public Task<DepartmentAbsenceWaitListPolicy?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<DepartmentAbsenceWaitListPolicy?>(null);
        public Task<DepartmentAbsenceWaitListPolicy?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<DepartmentAbsenceWaitListPolicy?>(null);
        public Task AddAsync(DepartmentAbsenceWaitListPolicy entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(DepartmentAbsenceWaitListPolicy entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public void Add(DepartmentAbsenceWaitListPolicy entity) { }
        public void Update(DepartmentAbsenceWaitListPolicy entity) { }
        public void Remove(DepartmentAbsenceWaitListPolicy entity) { }
    }

    private sealed class NullAbsenceWaitListAllowancePolicyRepository : IAbsenceWaitListAllowancePolicyRepository
    {
        public Task<AbsenceWaitListAllowancePolicy?> GetByCraftTypeCodeYearAsync(ControlNumber craftCtrlNbr, string waitListType, string allowanceCode, int calendarYear)
            => Task.FromResult<AbsenceWaitListAllowancePolicy?>(null);

        public Task<List<AbsenceWaitListAllowancePolicy>> GetByCraftAndTypeAsync(ControlNumber craftCtrlNbr, string waitListType, int calendarYear)
            => Task.FromResult(new List<AbsenceWaitListAllowancePolicy>());

        public Task<List<AbsenceWaitListAllowancePolicy>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<AbsenceWaitListAllowancePolicy>());
        public Task<List<AbsenceWaitListAllowancePolicy>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<AbsenceWaitListAllowancePolicy>());
        public Task<AbsenceWaitListAllowancePolicy?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<AbsenceWaitListAllowancePolicy?>(null);
        public Task<AbsenceWaitListAllowancePolicy?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<AbsenceWaitListAllowancePolicy?>(null);
        public Task AddAsync(AbsenceWaitListAllowancePolicy entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(AbsenceWaitListAllowancePolicy entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public void Add(AbsenceWaitListAllowancePolicy entity) { }
        public void Update(AbsenceWaitListAllowancePolicy entity) { }
        public void Remove(AbsenceWaitListAllowancePolicy entity) { }
    }

    private static class TestServerCallContextFactory
    {
        public static ServerCallContext Create()
        {
            var httpContext = new DefaultHttpContext();
            return TestServerCallContext.Create("/CrewService.Presentation.MarkOffSrvc/Test", httpContext);
        }
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        private readonly string _method;
        private readonly HttpContext _httpContext;

        private TestServerCallContext(string method, HttpContext httpContext)
        {
            _method = method;
            _httpContext = httpContext;
        }

        public static ServerCallContext Create(string method, HttpContext httpContext) => new TestServerCallContext(method, httpContext);

        protected override string MethodCore => _method;
        protected override string HostCore => "localhost";
        protected override string PeerCore => "127.0.0.1";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => new();
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore { get; } = new();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new(string.Empty, new Dictionary<string, List<AuthProperty>>());
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
        protected override IDictionary<object, object> UserStateCore =>
            _httpContext.Items.ToDictionary(kvp => kvp.Key!, kvp => kvp.Value!);
    }
}
