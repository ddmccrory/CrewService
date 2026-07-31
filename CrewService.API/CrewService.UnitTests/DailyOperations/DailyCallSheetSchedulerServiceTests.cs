using CrewService.Application.Time;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Services;
using CrewService.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace CrewService.UnitTests.DailyOperations;

public class DailyCallSheetSchedulerServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();

    [Fact]
    public async Task GetNextCallSheetEventUtcAsync_ReturnsEarliestFutureEventInWorkAreaTimezone()
    {
        await using var context = _dbFactory.CreateContext();

        var workArea = await SeedWorkAreaAsync(context, "America/Chicago", TestContext.Current.CancellationToken);
        var shiftDefinition = ShiftDefinition.Create(workArea.CtrlNbr, "1", "First", 1, isActive: true);
        await context.Set<ShiftDefinition>().AddAsync(shiftDefinition, TestContext.Current.CancellationToken);

        var assignmentEarly = Assignment.Create(workArea.CtrlNbr, "A1", "Early");
        var assignmentLater = Assignment.Create(workArea.CtrlNbr, "A2", "Later");
        await context.Set<Assignment>().AddRangeAsync([assignmentEarly, assignmentLater], TestContext.Current.CancellationToken);

        await context.Set<AssignmentSchedule>().AddRangeAsync(
        [
            AssignmentSchedule.Create(assignmentEarly.CtrlNbr, shiftDefinition.CtrlNbr, operatingDaysMask: DayMask(DayOfWeek.Monday), onDutyTime: new TimeOnly(7, 0), offDutyTime: new TimeOnly(15, 0)),
            AssignmentSchedule.Create(assignmentLater.CtrlNbr, shiftDefinition.CtrlNbr, operatingDaysMask: DayMask(DayOfWeek.Monday), onDutyTime: new TimeOnly(9, 0), offDutyTime: new TimeOnly(17, 0))
        ], TestContext.Current.CancellationToken);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var clock = new FakeWorkAreaClock(new DateTimeOffset(2026, 7, 6, 11, 0, 0, TimeSpan.Zero)); // Monday 06:00 CDT
        var sut = new DailyCallSheetSchedulerService(context, clock);

        var next = await sut.GetNextCallSheetEventUtcAsync(workArea.CtrlNbr, TestContext.Current.CancellationToken);

        Assert.NotNull(next);
        Assert.Equal(new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc), next!.Value); // 07:00 CDT
    }

    [Fact]
    public async Task GetDueWorkItemsAsync_ExcludesAlreadyGeneratedShiftInstances()
    {
        await using var context = _dbFactory.CreateContext();

        var workArea = await SeedWorkAreaAsync(context, "America/Chicago", TestContext.Current.CancellationToken);
        var shiftDefinition = ShiftDefinition.Create(workArea.CtrlNbr, "1", "First", 1, isActive: true);
        await context.Set<ShiftDefinition>().AddAsync(shiftDefinition, TestContext.Current.CancellationToken);

        var department = Department.Create(parentCtrlNbr: null, dynamicGroupCtrlNbr: workArea.CtrlNbr, name: "Transportation");
        await context.Set<Department>().AddAsync(department, TestContext.Current.CancellationToken);

        var assignment = Assignment.Create(workArea.CtrlNbr, "A1", "Scheduled", false, true, department.CtrlNbr);
        await context.Set<Assignment>().AddAsync(assignment, TestContext.Current.CancellationToken);
        await context.Set<CallSheetRule>().AddAsync(
            CallSheetRule.Create(
                department.CtrlNbr,
                callLeadMinutes: 90,
                callDurationMinutes: 30,
                holidayAdjustment: CallSheetHolidayAdjustmentType.None,
                holidayCustomOffsetMinutes: null,
                globalPreCreateOffsetMinutes: 0,
                isEnabled: true),
            TestContext.Current.CancellationToken);

        await context.Set<AssignmentSchedule>().AddAsync(
            AssignmentSchedule.Create(assignment.CtrlNbr, shiftDefinition.CtrlNbr, operatingDaysMask: DayMask(DayOfWeek.Monday), onDutyTime: new TimeOnly(7, 0), offDutyTime: new TimeOnly(15, 0)),
            TestContext.Current.CancellationToken);

        var targetDate = new DateOnly(2026, 7, 6); // Monday
        var dayStartUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var workInstance = WorkInstance.Create(null, workArea.CtrlNbr, dayStartUtc, dayStartUtc.AddDays(1), callTimeUtc: null);
        await context.Set<WorkInstance>().AddAsync(workInstance, TestContext.Current.CancellationToken);
        await context.Set<ShiftInstance>().AddAsync(
            ShiftInstance.Create(workInstance.CtrlNbr, shiftDefinition.CtrlNbr, shiftDefinition.ShiftCode, shiftDefinition.DisplayName, department.CtrlNbr, department.Name),
            TestContext.Current.CancellationToken);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var clock = new FakeWorkAreaClock(new DateTimeOffset(2026, 7, 6, 14, 0, 0, TimeSpan.Zero)); // Monday 09:00 CDT
        var sut = new DailyCallSheetSchedulerService(context, clock);

        var due = await sut.GetDueWorkItemsAsync(workArea.CtrlNbr, clock.UtcNow.UtcDateTime, TestContext.Current.CancellationToken);

        Assert.Empty(due);
    }

    [Fact]
    public async Task GetDueWorkItemsAsync_WithoutCallSheetRule_ReturnsEmpty()
    {
        await using var context = _dbFactory.CreateContext();

        var workArea = await SeedWorkAreaAsync(context, "America/Chicago", TestContext.Current.CancellationToken);
        var shiftDefinition = ShiftDefinition.Create(workArea.CtrlNbr, "1", "First", 1, isActive: true);
        await context.Set<ShiftDefinition>().AddAsync(shiftDefinition, TestContext.Current.CancellationToken);

        var assignment = Assignment.Create(workArea.CtrlNbr, "A1", "Scheduled");
        await context.Set<Assignment>().AddAsync(assignment, TestContext.Current.CancellationToken);
        await context.Set<AssignmentSchedule>().AddAsync(
            AssignmentSchedule.Create(assignment.CtrlNbr, shiftDefinition.CtrlNbr, operatingDaysMask: DayMask(DayOfWeek.Monday), onDutyTime: new TimeOnly(7, 0), offDutyTime: new TimeOnly(15, 0)),
            TestContext.Current.CancellationToken);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var nowUtc = new DateTime(2026, 7, 6, 14, 0, 0, DateTimeKind.Utc); // Monday 09:00 CDT
        var sut = new DailyCallSheetSchedulerService(context, new FakeWorkAreaClock(new DateTimeOffset(nowUtc)));

        var due = await sut.GetDueWorkItemsAsync(workArea.CtrlNbr, nowUtc, TestContext.Current.CancellationToken);

        Assert.Empty(due);
    }

    [Fact]
    public async Task GetDueWorkItemsAsync_WhenEventHitsOnDutyBoundary_ReturnsDueItem()
    {
        await using var context = _dbFactory.CreateContext();

        var workArea = await SeedWorkAreaAsync(context, "America/Chicago", TestContext.Current.CancellationToken);
        var shiftDefinition = ShiftDefinition.Create(workArea.CtrlNbr, "1", "First", 1, isActive: true);
        await context.Set<ShiftDefinition>().AddAsync(shiftDefinition, TestContext.Current.CancellationToken);

        var department = Department.Create(parentCtrlNbr: null, dynamicGroupCtrlNbr: workArea.CtrlNbr, name: "Transportation");
        await context.Set<Department>().AddAsync(department, TestContext.Current.CancellationToken);

        var assignment = Assignment.Create(workArea.CtrlNbr, "A1", "Scheduled", false, true, department.CtrlNbr);
        await context.Set<Assignment>().AddAsync(assignment, TestContext.Current.CancellationToken);
        await context.Set<CallSheetRule>().AddAsync(
            CallSheetRule.Create(
                department.CtrlNbr,
                callLeadMinutes: 0,
                callDurationMinutes: 30,
                holidayAdjustment: CallSheetHolidayAdjustmentType.None,
                holidayCustomOffsetMinutes: null,
                globalPreCreateOffsetMinutes: 0,
                isEnabled: true),
            TestContext.Current.CancellationToken);

        await context.Set<AssignmentSchedule>().AddAsync(
            AssignmentSchedule.Create(
                assignment.CtrlNbr,
                shiftDefinition.CtrlNbr,
                operatingDaysMask: DayMask(DayOfWeek.Monday),
                onDutyTime: new TimeOnly(7, 0),
                offDutyTime: new TimeOnly(15, 0)),
            TestContext.Current.CancellationToken);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var nowUtc = new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc); // Monday 07:00 CDT
        var sut = new DailyCallSheetSchedulerService(context, new FakeWorkAreaClock(new DateTimeOffset(nowUtc)));

        var due = await sut.GetDueWorkItemsAsync(workArea.CtrlNbr, nowUtc, TestContext.Current.CancellationToken);

        var item = Assert.Single(due);
        Assert.Equal(workArea.CtrlNbr, item.WorkAreaGroupCtrlNbr);
        Assert.Equal(shiftDefinition.CtrlNbr, item.ShiftDefinitionCtrlNbr);
        Assert.Equal(new DateOnly(2026, 7, 6), item.TargetDate);
        Assert.Equal(department.CtrlNbr, item.DepartmentCtrlNbr);
    }

    private static int DayMask(DayOfWeek dayOfWeek) => 1 << (int)dayOfWeek;

    private static async Task<DynamicGroup> SeedWorkAreaAsync(CrewServiceDbContext context, string timeZoneId, CancellationToken ct)
    {
        var groupType = GroupType.Create("Railroad", "Railroad", isWorkArea: true);
        await context.Set<GroupType>().AddAsync(groupType, ct);

        var workArea = DynamicGroup.Create(
            groupType.CtrlNbr,
            "Houston Yard",
            parentGroupCtrlNbr: null,
            path: null,
            isWorkArea: true,
            code: "HOU",
            parentCtrlNbr: null,
            railroadCtrlNbr: null,
            timeZoneId: timeZoneId);

        workArea.BuildPath(null);
        await context.Set<DynamicGroup>().AddAsync(workArea, ct);
        await context.SaveChangesAsync(ct);
        return workArea;
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class FakeWorkAreaClock(DateTimeOffset utcNow) : IWorkAreaClock
    {
        public DateTimeOffset UtcNow => utcNow;

        public TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId)) return null;
            try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
            catch (TimeZoneNotFoundException) { return null; }
            catch (InvalidTimeZoneException) { return null; }
        }

        public DateTimeOffset CombineLocalToUtc(DateOnly localDate, TimeOnly localTime, TimeZoneInfo? tz)
        {
            var local = localDate.ToDateTime(localTime, DateTimeKind.Unspecified);
            if (tz is null)
                return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Utc));

            var offset = tz.GetUtcOffset(local);
            return new DateTimeOffset(local, offset);
        }

        public string FormatLocalIso(DateTime utc, TimeZoneInfo? tz) => throw new NotImplementedException();
        public DateTime ParseToUtc(string value, TimeZoneInfo? tz) => throw new NotImplementedException();
        public Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(ControlNumber workAreaCtrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(CrewService.Domain.Interfaces.IOrchestrationUnitOfWork uow, ControlNumber workAreaCtrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<TimeZoneInfo?> GetCrewTimeZoneAsync(CrewService.Domain.Interfaces.IOrchestrationUnitOfWork uow, ControlNumber crewCtrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
    }
}
