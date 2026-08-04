using CrewService.Application.Absence;
using CrewService.Application.AbsenceVacancy;
using CrewService.Application.Authorization;
using CrewService.Application.BackgroundWorkers;
using CrewService.Application.DailyOperations;
using CrewService.Application.TenantConfig;
using CrewService.Application.Time;
using CrewService.Application.Notifications;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Infrastructure;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Infrastructure.Models.UserAccount;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
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
    public async Task CancelAbsence_PromotesWaitlistedCompensableRequestImmediately()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedRailroadAndEmployeesAsync(ct);
        var requestDateUtc = new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc);

        AbsenceRequest activeRequest;
        await using (var seed = CreateReadContext())
        {
            var department = Department.Create(seeded.Railroad.ParentCtrlNbr!.Value, seeded.Railroad.CtrlNbr, "Transportation");
            seed.Set<Department>().Add(department);

            var craft = Craft.Create(
                parentCtrlNbr: seeded.Railroad.ParentCtrlNbr,
                dynamicGroupCtrlNbr: seeded.Railroad.CtrlNbr,
                craftName: "Trainman",
                craftPluralName: "Trainmen",
                craftNumber: 1,
                autoMarkUp: false,
                approveAllMarkOffs: false,
                markOffHours: 0,
                markUpHours: 0,
                requiredRestHours: 0,
                maximumVacationDayTime: 0,
                unpaidMealPeriodMinutes: 0,
                hoursofService: false,
                processPayroll: false,
                showNotifications: false,
                vacationAssignmentType: 0,
                departmentCtrlNbr: department.CtrlNbr);
            seed.Set<Craft>().Add(craft);

            var waitListPolicy = CraftAbsenceWaitListPolicy.Create(
                craft.CtrlNbr,
                compensableDayMaxAssignments: 1,
                vacationWeekMaxAssignments: 1,
                isEnabled: true);
            seed.Set<CraftAbsenceWaitListPolicy>().Add(waitListPolicy);

            var craftRole = CraftRole.Create(craft.CtrlNbr, "TRMN", "Trainman");
            seed.Set<CraftRole>().Add(craftRole);

            var staffablePosition = StaffablePosition.Create(StaffablePositionType.Crew);
            seed.Set<StaffablePosition>().Add(staffablePosition);

            var crew = Crew.Create("REGULAR", seeded.Railroad.CtrlNbr, "Crew 1", departmentCtrlNbr: department.CtrlNbr);
            seed.Set<Crew>().Add(crew);

            var crewPosition = CrewPosition.Create(crew.CtrlNbr, craftRole.CtrlNbr, 1, staffablePosition.CtrlNbr);
            seed.Set<CrewPosition>().Add(crewPosition);

            var assignment = PositionAssignment.Create(
                staffablePosition.CtrlNbr,
                seeded.EmployeeA.CtrlNbr,
                PositionAssignmentType.Direct,
                assignedDateUtc: requestDateUtc.AddHours(-1));
            seed.Set<PositionAssignment>().Add(assignment);

            var code = AbsenceCode.Create(
                seeded.Railroad.CtrlNbr.Value,
                "CD",
                "Comp Day",
                isExcused: true,
                isCompensated: true,
                requiresApproval: true,
                isSystemOnly: false,
                isHolidayExempt: false,
                defaultAutoMarkUpHours: null,
                isActive: true);
            seed.Set<AbsenceCode>().Add(code);

            activeRequest = AbsenceRequest.CreateWithCode(
                seeded.EmployeeA.CtrlNbr,
                requestDateUtc.AddMinutes(1),
                null,
                code.CtrlNbr,
                "MARKOFF",
                isSystemGenerated: false,
                notes: null,
                autoMarkOffOnApproval: false);
            activeRequest.Approve(seeded.EmployeeA.CtrlNbr);
            seed.Set<AbsenceRequest>().Add(activeRequest);

            var waitListRecord = AbsenceRequestWaitListRecord.CreateCompensableDay(
                seeded.EmployeeB.CtrlNbr,
                code.CtrlNbr,
                requestDateUtc,
                new DateTime(2026, 7, 27, 19, 0, 0, DateTimeKind.Utc),
                craft.CtrlNbr,
                department.CtrlNbr);
            seed.Set<AbsenceRequestWaitListRecord>().Add(waitListRecord);

            await seed.SaveChangesAsync(ct);
        }

        var service = BuildService(
            utcNow: new DateTimeOffset(2026, 7, 28, 12, 0, 0, TimeSpan.Zero),
            railroadCtrlNbr: seeded.Railroad.CtrlNbr,
            absenceCodeRepository: new AbsenceCodeRepository(_crewContext, _currentUser),
            waitListRecordRepository: new AbsenceRequestWaitListRecordRepository(_crewContext, _currentUser),
            craftWaitListPolicyRepository: new CraftAbsenceWaitListPolicyRepository(_crewContext, _currentUser));

        _ = await service.CancelAbsence(
            new CancelAbsenceMsg { AbsenceRequestCtrlNbr = activeRequest.CtrlNbr.Value },
            TestServerCallContextFactory.Create());

        var response = await service.GetAbsenceRequests(
            new GetAbsenceRequestsMsg
            {
                RequestDateUtc = Timestamp.FromDateTime(requestDateUtc),
                IncludeAllStatuses = true
            },
            TestServerCallContextFactory.Create());

        Assert.DoesNotContain(response.Requests, r => r.IsWaitlisted);
        var promoted = Assert.Single(response.Requests, r => r.EmployeeCtrlNbr == seeded.EmployeeB.CtrlNbr.Value && !r.IsWaitlisted);
        Assert.Equal(activeRequest.ScheduledStartUtc, promoted.StartUtc.ToDateTime());
    }

    [Fact]
    public async Task SubmitWithCodeAsync_CompensatedCraftCapThree_UsesAssignmentCraftWhenSeniorityMissing()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedRailroadAndEmployeesAsync(ct);
        var startUtc = new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

        ControlNumber absenceCodeCtrlNbr;
        await using (var seed = CreateReadContext())
        {
            var department = Department.Create(seeded.Railroad.ParentCtrlNbr!.Value, seeded.Railroad.CtrlNbr, "Transportation");
            seed.Set<Department>().Add(department);

            var craft = Craft.Create(
                parentCtrlNbr: seeded.Railroad.ParentCtrlNbr,
                dynamicGroupCtrlNbr: seeded.Railroad.CtrlNbr,
                craftName: "Trainman",
                craftPluralName: "Trainmen",
                craftNumber: 1,
                autoMarkUp: false,
                approveAllMarkOffs: false,
                markOffHours: 0,
                markUpHours: 0,
                requiredRestHours: 0,
                maximumVacationDayTime: 0,
                unpaidMealPeriodMinutes: 0,
                hoursofService: false,
                processPayroll: false,
                showNotifications: false,
                vacationAssignmentType: 0,
                departmentCtrlNbr: department.CtrlNbr);
            seed.Set<Craft>().Add(craft);

            var role = CraftRole.Create(craft.CtrlNbr, "TRMN", "Trainman");
            seed.Set<CraftRole>().Add(role);

            var staffablePosition = StaffablePosition.Create(StaffablePositionType.Crew);
            seed.Set<StaffablePosition>().Add(staffablePosition);

            var crew = Crew.Create("REGULAR", seeded.Railroad.CtrlNbr, "Crew 1", departmentCtrlNbr: department.CtrlNbr);
            seed.Set<Crew>().Add(crew);

            var crewPosition = CrewPosition.Create(crew.CtrlNbr, role.CtrlNbr, 1, staffablePosition.CtrlNbr);
            seed.Set<CrewPosition>().Add(crewPosition);

            var assignment = PositionAssignment.Create(
                staffablePosition.CtrlNbr,
                seeded.EmployeeA.CtrlNbr,
                PositionAssignmentType.Direct,
                assignedDateUtc: startUtc.AddDays(-1));
            seed.Set<PositionAssignment>().Add(assignment);

            var craftWaitListPolicy = CraftAbsenceWaitListPolicy.Create(
                craft.CtrlNbr,
                compensableDayMaxAssignments: 3,
                vacationWeekMaxAssignments: 3,
                isEnabled: true);
            seed.Set<CraftAbsenceWaitListPolicy>().Add(craftWaitListPolicy);

            var compDayCode = AbsenceCode.Create(
                seeded.Railroad.CtrlNbr.Value,
                "CD",
                "Comp Day",
                isExcused: true,
                isCompensated: true,
                requiresApproval: true,
                isSystemOnly: false,
                isHolidayExempt: false,
                defaultAutoMarkUpHours: null,
                isActive: true);
            seed.Set<AbsenceCode>().Add(compDayCode);

            await seed.SaveChangesAsync(ct);
            absenceCodeCtrlNbr = compDayCode.CtrlNbr;
        }

        var factory = new OrchestrationUnitOfWorkFactory(
            _connection,
            _currentUser,
            new TestFieldEncryptor(),
            NullLoggerFactory.Instance);

        var workAreaClock = new FixedWorkAreaClock(new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero));
        var employeeNotificationService = new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            new NullRailroadResolver(),
            new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance),
            userAccounts: null,
            clock: workAreaClock);
        var startProposalService = new AbsenceStartProposalService(
            factory,
            new NullDepartmentAbsenceRequestWindowPolicyRepository(),
            workAreaClock);
        var slotVacancyEvaluationService = new CallSheetSlotVacancyEvaluationService(
            workAreaClock,
            new NullRailroadResolver(),
            new AbsenceCodeRepository(_crewContext, _currentUser));

        var service = new AbsenceRequestService(
            factory,
            new AbsenceCodeRepository(_crewContext, _currentUser),
            new StaticAbsenceApprovalPolicyResolver(),
            new CraftAbsenceWaitListPolicyRepository(_crewContext, _currentUser),
            new AbsenceWaitListAllowancePolicyRepository(_crewContext, _currentUser),
            startProposalService,
            new AbsenceMarkOffSignal(),
            new AutoMarkUpSignal(),
            slotVacancyEvaluationService,
            employeeNotificationService,
            NullLogger<AbsenceRequestService>.Instance);

        _ = await service.SubmitWithCodeAsync(seeded.EmployeeA.CtrlNbr, startUtc, null, absenceCodeCtrlNbr, "MARKOFF");
        _ = await service.SubmitWithCodeAsync(seeded.EmployeeA.CtrlNbr, startUtc, null, absenceCodeCtrlNbr, "MARKOFF");
        _ = await service.SubmitWithCodeAsync(seeded.EmployeeA.CtrlNbr, startUtc, null, absenceCodeCtrlNbr, "MARKOFF");
        var fourth = await service.SubmitWithCodeAsync(seeded.EmployeeA.CtrlNbr, startUtc, null, absenceCodeCtrlNbr, "MARKOFF");

        Assert.Null(fourth.AbsenceRequest);
        Assert.NotNull(fourth.WaitListRecord);

        await using var verify = CreateReadContext();
        var approvedCompDayRequests = await verify.Set<AbsenceRequest>()
            .Where(r => r.EmployeeCtrlNbr == seeded.EmployeeA.CtrlNbr
                && r.ScheduledStartUtc == startUtc
                && r.CancelledAtUtc == null
                && r.DeniedAtUtc == null)
            .ToListAsync(ct);
        Assert.Equal(3, approvedCompDayRequests.Count);
    }

    [Fact]
    public async Task GetAbsenceRequests_WaitListItems_AreScopedToSelectedRailroad()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedRailroadAndEmployeesAsync(ct);

        await using (var seed = CreateReadContext())
        {
            var otherParent = Parent.Create("Other Parent");
            seed.Parents.Add(otherParent);
            await seed.SaveChangesAsync(ct);

            var groupType = await seed.Set<GroupType>().SingleAsync(ct);
            var otherRailroad = DynamicGroup.Create(
                groupType.CtrlNbr,
                "Other Railroad",
                parentGroupCtrlNbr: null,
                path: null,
                isWorkArea: false,
                code: "ORR",
                parentCtrlNbr: otherParent.CtrlNbr);
            seed.DynamicGroups.Add(otherRailroad);
            await seed.SaveChangesAsync(ct);

            var selectedDepartment = Department.Create(seeded.Railroad.ParentCtrlNbr!.Value, seeded.Railroad.CtrlNbr, "Selected Dept");
            var otherDepartment = Department.Create(otherParent.CtrlNbr, otherRailroad.CtrlNbr, "Other Dept");
            seed.Set<Department>().AddRange(selectedDepartment, otherDepartment);
            await seed.SaveChangesAsync(ct);

            var selectedCraft = Craft.Create(
                parentCtrlNbr: seeded.Railroad.ParentCtrlNbr,
                dynamicGroupCtrlNbr: seeded.Railroad.CtrlNbr,
                craftName: "Selected Craft",
                craftPluralName: "Selected Crafts",
                craftNumber: 1,
                autoMarkUp: false,
                approveAllMarkOffs: false,
                markOffHours: 0,
                markUpHours: 0,
                requiredRestHours: 0,
                maximumVacationDayTime: 0,
                unpaidMealPeriodMinutes: 0,
                hoursofService: false,
                processPayroll: false,
                showNotifications: false,
                vacationAssignmentType: 0,
                departmentCtrlNbr: selectedDepartment.CtrlNbr);

            var otherCraft = Craft.Create(
                parentCtrlNbr: otherParent.CtrlNbr,
                dynamicGroupCtrlNbr: otherRailroad.CtrlNbr,
                craftName: "Other Craft",
                craftPluralName: "Other Crafts",
                craftNumber: 1,
                autoMarkUp: false,
                approveAllMarkOffs: false,
                markOffHours: 0,
                markUpHours: 0,
                requiredRestHours: 0,
                maximumVacationDayTime: 0,
                unpaidMealPeriodMinutes: 0,
                hoursofService: false,
                processPayroll: false,
                showNotifications: false,
                vacationAssignmentType: 0,
                departmentCtrlNbr: otherDepartment.CtrlNbr);

            seed.Set<Craft>().AddRange(selectedCraft, otherCraft);

            var selectedCode = AbsenceCode.Create(
                seeded.Railroad.CtrlNbr.Value,
                "CD",
                "Comp Day",
                isExcused: true,
                isCompensated: true,
                requiresApproval: true,
                isSystemOnly: false,
                isHolidayExempt: false,
                defaultAutoMarkUpHours: null,
                isActive: true);
            var otherCode = AbsenceCode.Create(
                otherRailroad.CtrlNbr.Value,
                "CD",
                "Comp Day",
                isExcused: true,
                isCompensated: true,
                requiresApproval: true,
                isSystemOnly: false,
                isHolidayExempt: false,
                defaultAutoMarkUpHours: null,
                isActive: true);

            seed.Set<AbsenceCode>().AddRange(selectedCode, otherCode);

            var requestDateUtc = new DateTime(2026, 7, 30, 0, 0, 0, DateTimeKind.Utc);
            var selectedWaitList = AbsenceRequestWaitListRecord.CreateCompensableDay(
                seeded.EmployeeA.CtrlNbr,
                selectedCode.CtrlNbr,
                requestDateUtc,
                entryUtc: new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Utc),
                selectedCraft.CtrlNbr,
                selectedDepartment.CtrlNbr);

            var crossRailroadWaitList = AbsenceRequestWaitListRecord.CreateCompensableDay(
                seeded.EmployeeA.CtrlNbr,
                otherCode.CtrlNbr,
                requestDateUtc,
                entryUtc: new DateTime(2026, 7, 30, 9, 1, 0, DateTimeKind.Utc),
                otherCraft.CtrlNbr,
                otherDepartment.CtrlNbr);

            seed.Set<AbsenceRequestWaitListRecord>().AddRange(selectedWaitList, crossRailroadWaitList);
            await seed.SaveChangesAsync(ct);
        }

        var service = BuildService(
            utcNow: new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero),
            railroadCtrlNbr: seeded.Railroad.CtrlNbr,
            absenceCodeRepository: new AbsenceCodeRepository(_crewContext, _currentUser),
            waitListRecordRepository: new AbsenceRequestWaitListRecordRepository(_crewContext, _currentUser));

        var response = await service.GetAbsenceRequests(
            new GetAbsenceRequestsMsg
            {
                RequestDateUtc = Timestamp.FromDateTime(new DateTime(2026, 7, 30, 0, 0, 0, DateTimeKind.Utc)),
                IncludeAllStatuses = true,
                EmployeeCtrlNbr = seeded.EmployeeA.CtrlNbr.Value
            },
            TestServerCallContextFactory.Create());

        var waitListItems = response.Requests.Where(r => r.IsWaitlisted).ToList();
        var result = Assert.Single(waitListItems);
        Assert.Equal("CD", result.ReasonCode);
    }

    [Fact]
    public async Task WaitListItems_And_DailyCounts_UseWorkAreaLocalDayBucketing()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedRailroadAndEmployeesAsync(ct, includeWorkArea: true);
        Assert.NotNull(seeded.WorkArea);

        await using (var seed = CreateReadContext())
        {
            var department = Department.Create(seeded.Railroad.ParentCtrlNbr!.Value, seeded.Railroad.CtrlNbr, "Transportation");
            seed.Set<Department>().Add(department);

            var craft = Craft.Create(
                parentCtrlNbr: seeded.Railroad.ParentCtrlNbr,
                dynamicGroupCtrlNbr: seeded.Railroad.CtrlNbr,
                craftName: "Trainman",
                craftPluralName: "Trainmen",
                craftNumber: 1,
                autoMarkUp: false,
                approveAllMarkOffs: false,
                markOffHours: 0,
                markUpHours: 0,
                requiredRestHours: 0,
                maximumVacationDayTime: 0,
                unpaidMealPeriodMinutes: 0,
                hoursofService: false,
                processPayroll: false,
                showNotifications: false,
                vacationAssignmentType: 0,
                departmentCtrlNbr: department.CtrlNbr);
            seed.Set<Craft>().Add(craft);

            var code = AbsenceCode.Create(
                seeded.Railroad.CtrlNbr.Value,
                "VD",
                "Vacation Day",
                isExcused: true,
                isCompensated: true,
                requiresApproval: false,
                isSystemOnly: false,
                isHolidayExempt: false,
                defaultAutoMarkUpHours: 24m,
                isActive: true);
            seed.Set<AbsenceCode>().Add(code);

            var requestDateUtc = new DateTime(2026, 8, 1, 4, 30, 0, DateTimeKind.Utc); // 07/31 local in CST
            var waitList = AbsenceRequestWaitListRecord.CreateCompensableDay(
                seeded.EmployeeA.CtrlNbr,
                code.CtrlNbr,
                requestDateUtc,
                entryUtc: requestDateUtc,
                craftCtrlNbr: craft.CtrlNbr,
                departmentCtrlNbr: department.CtrlNbr);
            seed.Set<AbsenceRequestWaitListRecord>().Add(waitList);

            await seed.SaveChangesAsync(ct);
        }

        var service = BuildService(
            utcNow: new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero),
            railroadCtrlNbr: seeded.Railroad.CtrlNbr,
            workAreaTimeZones: new Dictionary<long, TimeZoneInfo>
            {
                [seeded.WorkArea!.CtrlNbr.Value] = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time")
            },
            absenceCodeRepository: new AbsenceCodeRepository(_crewContext, _currentUser),
            waitListRecordRepository: new AbsenceRequestWaitListRecordRepository(_crewContext, _currentUser));

        var requestsResponse = await service.GetAbsenceRequests(
            new GetAbsenceRequestsMsg
            {
                RequestDateUtc = Timestamp.FromDateTime(new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc)),
                IncludeAllStatuses = true,
                WorkAreaGroupCtrlNbr = seeded.WorkArea.CtrlNbr.Value
            },
            TestServerCallContextFactory.Create());

        var waitListedRequest = Assert.Single(requestsResponse.Requests, r => r.IsWaitlisted);
        Assert.Equal(new DateTime(2026, 8, 1, 4, 30, 0, DateTimeKind.Utc), waitListedRequest.StartUtc.ToDateTime());

        var countsResponse = await service.GetAbsenceRequestCountsByDay(
            new GetAbsenceRequestCountsByDayMsg
            {
                RangeStartUtc = Timestamp.FromDateTime(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)),
                RangeEndUtc = Timestamp.FromDateTime(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)),
                IncludeAllStatuses = true,
                WorkAreaGroupCtrlNbr = seeded.WorkArea.CtrlNbr.Value
            },
            TestServerCallContextFactory.Create());

        var july31Count = Assert.Single(
            countsResponse.Counts,
            c => c.DateUtc.ToDateTime().Date == new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc).Date);
        Assert.Equal(1, july31Count.CompensatedCount);
        Assert.Equal(0, july31Count.NotCompensatedCount);
    }

    [Fact]
    public async Task CancelAsync_CancelsRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedRailroadAndEmployeesAsync(ct);
        AbsenceRequest request;

        await using (var seed = CreateReadContext())
        {
            request = AbsenceRequest.Create(
                seeded.EmployeeA.CtrlNbr,
                new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc),
                null,
                "MARKOFF");
            seed.Set<AbsenceRequest>().Add(request);
            await seed.SaveChangesAsync(ct);
        }

        var (service, _) = BuildAbsenceRequestService(
            utcNow: new DateTimeOffset(2026, 7, 30, 12, 5, 0, TimeSpan.Zero),
            railroadCtrlNbr: seeded.Railroad.CtrlNbr);

        var cancelled = await service.CancelAsync(request.CtrlNbr);

        Assert.Equal("CANCELLED", cancelled.DerivedStatus);
    }

    [Fact]
    public async Task CancelWaitListAsync_RemovesWaitlistRecord()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedRailroadAndEmployeesAsync(ct);
        AbsenceRequestWaitListRecord waitListRecord;

        await using (var seed = CreateReadContext())
        {
            var code = AbsenceCode.Create(
                seeded.Railroad.CtrlNbr.Value,
                "CD",
                "Comp Day",
                isExcused: true,
                isCompensated: true,
                requiresApproval: true,
                isSystemOnly: false,
                isHolidayExempt: false,
                defaultAutoMarkUpHours: null,
                isActive: true);
            seed.Set<AbsenceCode>().Add(code);

            waitListRecord = AbsenceRequestWaitListRecord.CreateCompensableDay(
                seeded.EmployeeA.CtrlNbr,
                code.CtrlNbr,
                new DateTime(2026, 7, 30, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc),
                craftCtrlNbr: null,
                departmentCtrlNbr: null);
            seed.Set<AbsenceRequestWaitListRecord>().Add(waitListRecord);
            await seed.SaveChangesAsync(ct);
        }

        var (service, _) = BuildAbsenceRequestService(
            utcNow: new DateTimeOffset(2026, 7, 30, 12, 5, 0, TimeSpan.Zero),
            railroadCtrlNbr: seeded.Railroad.CtrlNbr,
            waitListRecordRepository: new AbsenceRequestWaitListRecordRepository(_crewContext, _currentUser));

        await service.CancelWaitListAsync(waitListRecord.CtrlNbr);

        await using (var verify = CreateReadContext())
        {
            var activeRecord = await verify.Set<AbsenceRequestWaitListRecord>()
                .SingleOrDefaultAsync(r => r.CtrlNbr == waitListRecord.CtrlNbr, ct);
            Assert.Null(activeRecord);

            var deletedRecord = await verify.Set<AbsenceRequestWaitListRecord>()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(r => r.CtrlNbr == waitListRecord.CtrlNbr, ct);
            Assert.NotNull(deletedRecord);
            Assert.True(deletedRecord!.IsDeleted);
        }

    }

    [Fact]
    public async Task SubmitWithCodeAsync_CompensatedCraftCapThreeWithoutAllowance_FourthRequestIsWaitlisted()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedRailroadAndEmployeesAsync(ct);
        var startUtc = new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

        ControlNumber absenceCodeCtrlNbr;
        await using (var seed = CreateReadContext())
        {
            var department = Department.Create(seeded.Railroad.ParentCtrlNbr!.Value, seeded.Railroad.CtrlNbr, "Transportation");
            seed.Set<Department>().Add(department);

            var craft = Craft.Create(
                parentCtrlNbr: seeded.Railroad.ParentCtrlNbr,
                dynamicGroupCtrlNbr: seeded.Railroad.CtrlNbr,
                craftName: "Trainman",
                craftPluralName: "Trainmen",
                craftNumber: 1,
                autoMarkUp: false,
                approveAllMarkOffs: false,
                markOffHours: 0,
                markUpHours: 0,
                requiredRestHours: 0,
                maximumVacationDayTime: 0,
                unpaidMealPeriodMinutes: 0,
                hoursofService: false,
                processPayroll: false,
                showNotifications: false,
                vacationAssignmentType: 0,
                departmentCtrlNbr: department.CtrlNbr);
            seed.Set<Craft>().Add(craft);

            var role = CraftRole.Create(craft.CtrlNbr, "TRMN", "Trainman");
            seed.Set<CraftRole>().Add(role);

            var staffablePosition = StaffablePosition.Create(StaffablePositionType.Crew);
            seed.Set<StaffablePosition>().Add(staffablePosition);

            var crew = Crew.Create("REGULAR", seeded.Railroad.CtrlNbr, "Crew 1", departmentCtrlNbr: department.CtrlNbr);
            seed.Set<Crew>().Add(crew);

            var crewPosition = CrewPosition.Create(crew.CtrlNbr, role.CtrlNbr, 1, staffablePosition.CtrlNbr);
            seed.Set<CrewPosition>().Add(crewPosition);

            var assignment = PositionAssignment.Create(
                staffablePosition.CtrlNbr,
                seeded.EmployeeA.CtrlNbr,
                PositionAssignmentType.Direct,
                assignedDateUtc: startUtc.AddDays(-1));
            seed.Set<PositionAssignment>().Add(assignment);

            var roster = Roster.Create(craft.CtrlNbr, seeded.Railroad.CtrlNbr, null, "Trainman Roster", "Trainman Rosters", 1);
            seed.Set<Roster>().Add(roster);

            var seniorityState = SeniorityState.Create("Active", StateType.Active, seeded.Railroad.ParentCtrlNbr!.Value);
            seed.Set<SeniorityState>().Add(seniorityState);

            var seniority = Seniority.Create(
                roster.CtrlNbr,
                seeded.EmployeeA.CtrlNbr,
                lastActiveRoster: true,
                rosterDate: startUtc.Date,
                rank: 1,
                seniorityStateCtrlNbr: seniorityState.CtrlNbr,
                canTrain: false);
            seed.Set<Seniority>().Add(seniority);

            var craftWaitListPolicy = CraftAbsenceWaitListPolicy.Create(
                craft.CtrlNbr,
                compensableDayMaxAssignments: 3,
                vacationWeekMaxAssignments: 3,
                isEnabled: true);
            seed.Set<CraftAbsenceWaitListPolicy>().Add(craftWaitListPolicy);

            // Intentionally do not create any AbsenceWaitListAllowancePolicy row for this code/year.
            var compDayCode = AbsenceCode.Create(
                seeded.Railroad.CtrlNbr.Value,
                "CD",
                "Comp Day",
                isExcused: true,
                isCompensated: true,
                requiresApproval: true,
                isSystemOnly: false,
                isHolidayExempt: false,
                defaultAutoMarkUpHours: null,
                isActive: true);
            seed.Set<AbsenceCode>().Add(compDayCode);

            await seed.SaveChangesAsync(ct);
            absenceCodeCtrlNbr = compDayCode.CtrlNbr;
        }

        var factory = new OrchestrationUnitOfWorkFactory(
            _connection,
            _currentUser,
            new TestFieldEncryptor(),
            NullLoggerFactory.Instance);

        var workAreaClock = new FixedWorkAreaClock(new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero));
        var employeeNotificationService = new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            new NullRailroadResolver(),
            new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance),
            userAccounts: null,
            clock: workAreaClock);
        var startProposalService = new AbsenceStartProposalService(
            factory,
            new NullDepartmentAbsenceRequestWindowPolicyRepository(),
            workAreaClock);
        var slotVacancyEvaluationService = new CallSheetSlotVacancyEvaluationService(
            workAreaClock,
            new NullRailroadResolver(),
            new AbsenceCodeRepository(_crewContext, _currentUser));

        var service = new AbsenceRequestService(
            factory,
            new AbsenceCodeRepository(_crewContext, _currentUser),
            new StaticAbsenceApprovalPolicyResolver(),
            new CraftAbsenceWaitListPolicyRepository(_crewContext, _currentUser),
            new AbsenceWaitListAllowancePolicyRepository(_crewContext, _currentUser),
            startProposalService,
            new AbsenceMarkOffSignal(),
            new AutoMarkUpSignal(),
            slotVacancyEvaluationService,
            employeeNotificationService,
            NullLogger<AbsenceRequestService>.Instance);

        var first = await service.SubmitWithCodeAsync(seeded.EmployeeA.CtrlNbr, startUtc, null, absenceCodeCtrlNbr, "MARKOFF");
        var second = await service.SubmitWithCodeAsync(seeded.EmployeeA.CtrlNbr, startUtc, null, absenceCodeCtrlNbr, "MARKOFF");
        var third = await service.SubmitWithCodeAsync(seeded.EmployeeA.CtrlNbr, startUtc, null, absenceCodeCtrlNbr, "MARKOFF");
        var fourth = await service.SubmitWithCodeAsync(seeded.EmployeeA.CtrlNbr, startUtc, null, absenceCodeCtrlNbr, "MARKOFF");

        Assert.NotNull(first.AbsenceRequest);
        Assert.Null(first.WaitListRecord);
        Assert.NotNull(second.AbsenceRequest);
        Assert.Null(second.WaitListRecord);
        Assert.NotNull(third.AbsenceRequest);
        Assert.Null(third.WaitListRecord);

        Assert.Null(fourth.AbsenceRequest);
        Assert.NotNull(fourth.WaitListRecord);
        Assert.Equal(AbsenceRequestWaitListType.CompensableDay, fourth.WaitListRecord!.WaitListType);

        await using var verify = CreateReadContext();
        var savedWaitListRecord = await verify.Set<AbsenceRequestWaitListRecord>()
            .SingleOrDefaultAsync(r => r.CtrlNbr == fourth.WaitListRecord.CtrlNbr, ct);
        Assert.NotNull(savedWaitListRecord);
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
        Dictionary<long, TimeZoneInfo>? workAreaTimeZones = null,
        IAbsenceCodeRepository? absenceCodeRepository = null,
        IAbsenceRequestWaitListRecordRepository? waitListRecordRepository = null,
        ICraftAbsenceWaitListPolicyRepository? craftWaitListPolicyRepository = null,
        IAbsenceWaitListAllowancePolicyRepository? waitListAllowancePolicyRepository = null)
    {
        var actorContextResolver = new FixedActorContextResolver(railroadCtrlNbr.Value);
        var workAreaClock = new FixedWorkAreaClock(utcNow, workAreaTimeZones);

        var factory = new OrchestrationUnitOfWorkFactory(
            _connection,
            _currentUser,
            new TestFieldEncryptor(),
            NullLoggerFactory.Instance);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IRequestActorContextResolver>(actorContextResolver);
        services.AddSingleton<IWorkAreaClock>(workAreaClock);
        services.AddSingleton<IOrchestrationUnitOfWorkFactory>(factory);
        services.AddSingleton<IRailroadResolver, NullRailroadResolver>();
        services.AddSingleton<IAbsenceCodeRepository>(absenceCodeRepository ?? new NullAbsenceCodeRepository());
        services.AddSingleton<IDepartmentAbsenceRequestWindowPolicyRepository, NullDepartmentAbsenceRequestWindowPolicyRepository>();
        services.AddSingleton<IAbsenceRequestWaitListRecordRepository>(waitListRecordRepository ?? new NullAbsenceRequestWaitListRecordRepository());
        services.AddSingleton<ICraftAbsenceWaitListPolicyRepository>(craftWaitListPolicyRepository ?? new NullCraftAbsenceWaitListPolicyRepository());
        services.AddSingleton<IAbsenceWaitListAllowancePolicyRepository>(waitListAllowancePolicyRepository ?? new NullAbsenceWaitListAllowancePolicyRepository());
        services.AddSingleton<IAbsenceApprovalPolicyResolver, StaticAbsenceApprovalPolicyResolver>();
        services.AddSingleton<IAbsenceMarkOffSignal, AbsenceMarkOffSignal>();
        services.AddSingleton<IAutoMarkUpSignal, AutoMarkUpSignal>();
        services.AddSingleton<IWaitListReassignmentSignal, WaitListReassignmentSignal>();
        services.AddSingleton<IWorkerScheduleRepository, NullWorkerScheduleRepository>();
        services.AddSingleton<IAbsenceRequestWaitListLinkRepository>(new AbsenceRequestWaitListLinkRepository(_crewContext, _currentUser));
        services.AddSingleton(_ => new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance));
        services.AddSingleton(sp => new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            sp.GetRequiredService<IRailroadResolver>(),
            sp.GetRequiredService<NotificationTypeConfigResolver>(),
            userAccounts: null,
            clock: sp.GetRequiredService<IWorkAreaClock>()));
        services.AddTransient<AbsenceStartProposalService>();
        services.AddTransient<CallSheetSlotVacancyEvaluationService>();
        services.AddTransient<AbsenceRequestService>();
        services.AddTransient<AbsenceWaitListReassignmentEvaluator>();
        services.AddTransient<AbsenceWaitListReassignmentProcessor>();

        return new AbsenceService(services.BuildServiceProvider());
    }

    private (AbsenceRequestService Service, WaitListReassignmentSignal WaitListSignal) BuildAbsenceRequestService(
        DateTimeOffset utcNow,
        ControlNumber railroadCtrlNbr,
        Dictionary<long, TimeZoneInfo>? workAreaTimeZones = null,
        IAbsenceRequestWaitListRecordRepository? waitListRecordRepository = null)
    {
        var actorContextResolver = new FixedActorContextResolver(railroadCtrlNbr.Value);
        var workAreaClock = new FixedWorkAreaClock(utcNow, workAreaTimeZones);

        var factory = new OrchestrationUnitOfWorkFactory(
            _connection,
            _currentUser,
            new TestFieldEncryptor(),
            NullLoggerFactory.Instance);

        var waitListSignal = new WaitListReassignmentSignal();
        var employeeNotificationService = new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            new NullRailroadResolver(),
            new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance),
            userAccounts: null,
            clock: workAreaClock);

        var startProposalService = new AbsenceStartProposalService(
            factory,
            new NullDepartmentAbsenceRequestWindowPolicyRepository(),
            workAreaClock);
        var slotVacancyEvaluationService = new CallSheetSlotVacancyEvaluationService(
            workAreaClock,
            new NullRailroadResolver(),
            new NullAbsenceCodeRepository());

        var service = new AbsenceRequestService(
            factory,
            new NullAbsenceCodeRepository(),
            new StaticAbsenceApprovalPolicyResolver(),
            new NullCraftAbsenceWaitListPolicyRepository(),
            new NullAbsenceWaitListAllowancePolicyRepository(),
            startProposalService,
            new AbsenceMarkOffSignal(),
            new AutoMarkUpSignal(),
            slotVacancyEvaluationService,
            employeeNotificationService,
            NullLogger<AbsenceRequestService>.Instance);

        return (service, waitListSignal);
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

    private sealed class NullAbsenceRequestWaitListLinkRepository : IAbsenceRequestWaitListLinkRepository
    {
        public Task<List<AbsenceRequestWaitListLink>> GetByRequestAsync(ControlNumber absenceRequestCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(new List<AbsenceRequestWaitListLink>());

        public Task<List<AbsenceRequestWaitListLink>> GetByWaitListRecordAsync(ControlNumber waitListRecordCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(new List<AbsenceRequestWaitListLink>());

        public Task<List<AbsenceRequestWaitListLink>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<AbsenceRequestWaitListLink>());
        public Task<List<AbsenceRequestWaitListLink>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<AbsenceRequestWaitListLink>());
        public Task<AbsenceRequestWaitListLink?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<AbsenceRequestWaitListLink?>(null);
        public Task<AbsenceRequestWaitListLink?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<AbsenceRequestWaitListLink?>(null);
        public Task AddAsync(AbsenceRequestWaitListLink entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(AbsenceRequestWaitListLink entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public void Add(AbsenceRequestWaitListLink entity) { }
        public void Update(AbsenceRequestWaitListLink entity) { }
        public void Remove(AbsenceRequestWaitListLink entity) { }
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

    private sealed class NullCraftAbsenceWaitListPolicyRepository : ICraftAbsenceWaitListPolicyRepository
    {
        public Task<CraftAbsenceWaitListPolicy?> GetByCraftAsync(ControlNumber craftCtrlNbr)
            => Task.FromResult<CraftAbsenceWaitListPolicy?>(null);

        public Task<List<CraftAbsenceWaitListPolicy>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<CraftAbsenceWaitListPolicy>());
        public Task<List<CraftAbsenceWaitListPolicy>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<CraftAbsenceWaitListPolicy>());
        public Task<CraftAbsenceWaitListPolicy?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<CraftAbsenceWaitListPolicy?>(null);
        public Task<CraftAbsenceWaitListPolicy?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<CraftAbsenceWaitListPolicy?>(null);
        public Task AddAsync(CraftAbsenceWaitListPolicy entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(CraftAbsenceWaitListPolicy entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public void Add(CraftAbsenceWaitListPolicy entity) { }
        public void Update(CraftAbsenceWaitListPolicy entity) { }
        public void Remove(CraftAbsenceWaitListPolicy entity) { }
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

    private sealed class NullWorkerScheduleRepository : IWorkerScheduleRepository
    {
        public Task<IReadOnlyList<WorkerSchedule>> GetDueByTypeAsync(string workerType, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WorkerSchedule>>([]);

        public Task<IReadOnlyList<WorkerSchedule>> GetEnabledByTypeAsync(string workerType, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WorkerSchedule>>([]);

        public Task<IReadOnlyList<WorkerSchedule>> GetAllAsync(string? workerType = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WorkerSchedule>>([]);

        public Task<WorkerSchedule?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult<WorkerSchedule?>(null);

        public Task AddAsync(WorkerSchedule schedule, CancellationToken ct = default) => Task.CompletedTask;

        public Task UpdateAsync(WorkerSchedule schedule, CancellationToken ct = default) => Task.CompletedTask;
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
