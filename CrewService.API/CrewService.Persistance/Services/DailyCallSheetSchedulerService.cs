using CrewService.Application.DailyOperations;
using CrewService.Application.Time;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Services;

internal sealed class DailyCallSheetSchedulerService(
    CrewServiceDbContext dbContext,
    IWorkAreaClock clock) : IDailyCallSheetSchedulerService
{
    private const int GlobalFutureGuardMinutes = 3;

    private sealed record CandidateSeed(
        DailyCallSheetDueWorkItem Item,
        DateTime OnDutyUtc,
        DateTime CallingStartUtc,
        bool IsHoliday,
        string ShiftCode,
        string ShiftDisplayName,
        string? DepartmentName,
        CallSheetRule? Rule);

    private sealed record GroupedCandidate(
        DailyCallSheetDueWorkItem Item,
        DateTime EventUtc,
        DateTime EarliestOnDutyUtc,
        CallSheetRule? Rule,
        string ShiftCode,
        string ShiftDisplayName,
        string? DepartmentName);

    public async Task<DateTime?> GetNextCallSheetEventUtcAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
    {
        var candidate = await GetNextCallSheetEventCandidateAsync(workAreaGroupCtrlNbr, ct);
        return candidate?.EventUtc;
    }

    public async Task<DailyCallSheetNextEventCandidate?> GetNextCallSheetEventCandidateAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
    {
        var nowUtc = DateTime.SpecifyKind(clock.UtcNow.UtcDateTime, DateTimeKind.Utc);
        var descendants = await GetWorkAreaAndDescendantCtrlNbrsAsync(workAreaGroupCtrlNbr, ct);
        if (descendants.Count == 0)
            return null;

        var startDate = DateOnly.FromDateTime(nowUtc);
        var endDate = startDate.AddDays(7);
        var seeds = await BuildCandidateSeedsAsync(descendants, startDate, endDate, ct);
        if (seeds.Count == 0)
            return null;

        var groupedCandidates = GroupCandidates(seeds);
        if (groupedCandidates.Count == 0)
            return null;

        groupedCandidates = await FilterAlreadyGeneratedCandidatesAsync(groupedCandidates, ct);
        if (groupedCandidates.Count == 0)
            return null;

        var projected = groupedCandidates
            .Where(c => c.EventUtc > nowUtc && c.EarliestOnDutyUtc > nowUtc)
            .OrderBy(c => c.EventUtc)
            .ToList();

        if (projected.Count == 0)
            return null;

        var next = projected[0];

        return new DailyCallSheetNextEventCandidate(
            next.EventUtc,
            next.Item,
            next.ShiftCode,
            next.ShiftDisplayName,
            next.DepartmentName);
    }

    public async Task<IReadOnlyList<DailyCallSheetDueWorkItem>> GetDueWorkItemsAsync(
        ControlNumber workAreaGroupCtrlNbr,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        var descendants = await GetWorkAreaAndDescendantCtrlNbrsAsync(workAreaGroupCtrlNbr, ct);
        if (descendants.Count == 0)
            return [];
        var startDate = DateOnly.FromDateTime(nowUtc.AddDays(-1));
        var endDate = DateOnly.FromDateTime(nowUtc);
        var seeds = await BuildCandidateSeedsAsync(descendants, startDate, endDate, ct);
        if (seeds.Count == 0)
            return [];

        var groupedCandidates = GroupCandidates(seeds);
        if (groupedCandidates.Count == 0)
            return [];

        groupedCandidates = await FilterAlreadyGeneratedCandidatesAsync(groupedCandidates, ct);
        if (groupedCandidates.Count == 0)
            return [];

        var due = groupedCandidates
            .Where(c => IsCandidateDueNow(c, nowUtc))
            .ToList();

        if (due.Count == 0)
            return [];

        return due.Select(d => d.Item).ToList();
    }

    private async Task<List<GroupedCandidate>> FilterAlreadyGeneratedCandidatesAsync(
        List<GroupedCandidate> candidates,
        CancellationToken ct)
    {
        if (candidates.Count == 0)
            return [];

        var items = candidates.Select(c => c.Item).ToList();
        var workAreaIds = items.Select(d => d.WorkAreaGroupCtrlNbr).Distinct().ToList();
        var minDate = items.Min(d => d.TargetDate).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var maxDateExclusive = items.Max(d => d.TargetDate).AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var workInstances = await dbContext.Set<WorkInstance>()
            .Where(w => workAreaIds.Contains(w.WorkAreaGroupCtrlNbr)
                        && w.StartUtc >= minDate
                        && w.StartUtc < maxDateExclusive)
            .ToListAsync(ct);

        var workInstanceIds = workInstances.Select(w => w.CtrlNbr).ToList();
        if (workInstanceIds.Count == 0)
            return candidates;

        var existingShifts = await dbContext.Set<ShiftInstance>()
            .Where(s => workInstanceIds.Contains(s.WorkInstanceCtrlNbr))
            .ToListAsync(ct);

        var workInstanceById = workInstances.ToDictionary(w => w.CtrlNbr);
        var existingShiftKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var shift in existingShifts)
        {
            if (!workInstanceById.TryGetValue(shift.WorkInstanceCtrlNbr, out var workInstance))
                continue;

            var day = DateOnly.FromDateTime(workInstance.StartUtc);
            var key = BuildShiftKey(workInstance.WorkAreaGroupCtrlNbr, shift.ShiftDefinitionCtrlNbr, day, shift.DepartmentCtrlNbr);
            existingShiftKeys.Add(key);
        }

        return candidates
            .Where(c => !existingShiftKeys.Contains(BuildShiftKey(
                c.Item.WorkAreaGroupCtrlNbr,
                c.Item.ShiftDefinitionCtrlNbr,
                c.Item.TargetDate,
                c.Item.DepartmentCtrlNbr)))
            .ToList();
    }

    private async Task<List<CandidateSeed>> BuildCandidateSeedsAsync(
        IReadOnlyList<ControlNumber> descendants,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct)
    {
        var groups = await dbContext.Set<DynamicGroup>()
            .Where(g => descendants.Contains(g.CtrlNbr))
            .ToDictionaryAsync(g => g.CtrlNbr, ct);

        var assignments = await dbContext.Set<Assignment>()
            .Where(a => a.IsActive && descendants.Contains(a.GroupCtrlNbr))
            .Select(a => new { a.CtrlNbr, a.DepartmentCtrlNbr })
            .ToListAsync(ct);

        if (assignments.Count == 0)
            return [];

        var assignmentIds = assignments.Select(a => a.CtrlNbr).ToList();

        var schedules = await dbContext.Set<AssignmentSchedule>()
            .Where(s => assignmentIds.Contains(s.AssignmentCtrlNbr))
            .ToListAsync(ct);

        if (schedules.Count == 0)
            return [];

        var shiftDefs = await dbContext.Set<ShiftDefinition>()
            .Where(sd => sd.IsActive && descendants.Contains(sd.WorkAreaGroupCtrlNbr))
            .ToListAsync(ct);

        if (shiftDefs.Count == 0)
            return [];

        var departmentIds = assignments
            .Where(a => a.DepartmentCtrlNbr is not null)
            .Select(a => a.DepartmentCtrlNbr!)
            .Distinct()
            .ToList();

        var departments = departmentIds.Count == 0
            ? new Dictionary<ControlNumber, Department>()
            : await dbContext.Set<Department>()
                .Where(d => departmentIds.Contains(d.CtrlNbr))
                .ToDictionaryAsync(d => d.CtrlNbr, ct);

        var rules = departmentIds.Count == 0
            ? new Dictionary<ControlNumber, CallSheetRule>()
            : await dbContext.Set<CallSheetRule>()
                .Where(r => departmentIds.Contains(r.DepartmentCtrlNbr))
                .ToDictionaryAsync(r => r.DepartmentCtrlNbr, ct);

        var holidayDatesByWorkArea = await dbContext.Set<Holiday>()
            .Where(h => h.IsActive
                        && descendants.Contains(h.WorkAreaGroupCtrlNbr)
                        && h.ObservedDate >= startDate
                        && h.ObservedDate <= endDate)
            .GroupBy(h => h.WorkAreaGroupCtrlNbr)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(h => h.ObservedDate).ToHashSet(),
                ct);

        var shiftDefLookup = shiftDefs.ToDictionary(sd => sd.CtrlNbr);
        var assignmentLookup = assignments.ToDictionary(a => a.CtrlNbr);

        var seeds = new List<CandidateSeed>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayBit = 1 << (int)date.DayOfWeek;
            foreach (var schedule in schedules)
            {
                if ((schedule.OperatingDaysMask & dayBit) == 0)
                    continue;

                if (!shiftDefLookup.TryGetValue(schedule.ShiftDefinitionCtrlNbr, out var shiftDef))
                    continue;

                if (!assignmentLookup.TryGetValue(schedule.AssignmentCtrlNbr, out var assignment))
                    continue;

                groups.TryGetValue(shiftDef.WorkAreaGroupCtrlNbr, out var workArea);
                var tz = clock.ResolveTimeZone(workArea?.TimeZoneId) ?? TimeZoneInfo.Utc;
                var onDutyUtc = clock.CombineLocalToUtc(date, schedule.OnDutyTime, tz).UtcDateTime;

                CallSheetRule? rule = null;
                if (assignment.DepartmentCtrlNbr is { } dept && rules.TryGetValue(dept, out var configured))
                    rule = configured;

                if (rule is not null && !rule.IsEnabled)
                    continue;

                var callingStartUtc = onDutyUtc;
                if (rule is not null)
                {
                    var localOnDuty = date.ToDateTime(schedule.OnDutyTime);
                    var localCallingStart = localOnDuty.AddMinutes(-rule.CallLeadMinutes);
                    callingStartUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localCallingStart, DateTimeKind.Unspecified), tz);
                }

                var shiftCode = shiftDef.ShiftCode.ToUpperInvariant();
                var shiftDisplayName = shiftDef.DisplayName;
                var isHoliday = holidayDatesByWorkArea.TryGetValue(shiftDef.WorkAreaGroupCtrlNbr, out var holidayDates)
                    && holidayDates.Contains(date);

                var item = new DailyCallSheetDueWorkItem(
                    shiftDef.WorkAreaGroupCtrlNbr,
                    schedule.ShiftDefinitionCtrlNbr,
                    date,
                    assignment.DepartmentCtrlNbr);

                var departmentName = assignment.DepartmentCtrlNbr is { } deptCtrlNbr
                    && departments.TryGetValue(deptCtrlNbr, out var department)
                    ? department.Name
                    : null;

                seeds.Add(new CandidateSeed(item, onDutyUtc, callingStartUtc, isHoliday, shiftCode, shiftDisplayName, departmentName, rule));
            }
        }

        return seeds;
    }

    private static List<GroupedCandidate> GroupCandidates(List<CandidateSeed> seeds)
    {
        var groupedCandidates = new List<GroupedCandidate>();

        foreach (var group in seeds.GroupBy(c => new
            {
                c.Item.WorkAreaGroupCtrlNbr,
                c.Item.ShiftDefinitionCtrlNbr,
                c.Item.TargetDate,
                c.Item.DepartmentCtrlNbr
            }))
        {
            var first = group.First();
            var earliestOnDutyUtc = group.Min(x => x.OnDutyUtc);

            if (first.Rule is null)
            {
                groupedCandidates.Add(new GroupedCandidate(
                    first.Item,
                    earliestOnDutyUtc,
                    earliestOnDutyUtc,
                    null,
                    first.ShiftCode,
                    first.ShiftDisplayName,
                    first.DepartmentName));
                continue;
            }

            var eventUtc = group.Min(x => x.CallingStartUtc)
                .AddMinutes(first.Rule.GlobalPreCreateOffsetMinutes);

            if (group.Any(x => x.IsHoliday))
            {
                if (first.Rule.HolidayAdjustment.Equals(CallSheetHolidayAdjustmentType.SkipHoliday, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (first.Rule.HolidayAdjustment.Equals(CallSheetHolidayAdjustmentType.AddDay, StringComparison.OrdinalIgnoreCase))
                {
                    eventUtc = eventUtc.AddDays(1);
                }
                else if (first.Rule.HolidayAdjustment.Equals(CallSheetHolidayAdjustmentType.CustomOffset, StringComparison.OrdinalIgnoreCase))
                {
                    eventUtc = eventUtc.AddMinutes(first.Rule.HolidayCustomOffsetMinutes ?? 0);
                }
            }

            groupedCandidates.Add(new GroupedCandidate(
                first.Item,
                eventUtc,
                earliestOnDutyUtc,
                first.Rule,
                first.ShiftCode,
                first.ShiftDisplayName,
                first.DepartmentName));
        }

        return groupedCandidates;
    }

    private static bool IsCandidateDueNow(GroupedCandidate candidate, DateTime nowUtc)
    {
        var graceMinutes = GlobalFutureGuardMinutes;

        return candidate.EventUtc <= nowUtc
            && candidate.EventUtc >= nowUtc.AddMinutes(-graceMinutes);
    }

    private async Task<List<ControlNumber>> GetWorkAreaAndDescendantCtrlNbrsAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct)
    {
        var workArea = await dbContext.Set<DynamicGroup>()
            .SingleOrDefaultAsync(g => g.CtrlNbr == workAreaGroupCtrlNbr, ct);

        if (workArea is null)
            return [];

        if (workArea.Path is not null)
        {
            var prefix = workArea.Path + "/";
            return await dbContext.Set<DynamicGroup>()
                .Where(g => g.Path != null && (g.Path == workArea.Path || g.Path.StartsWith(prefix)))
                .Select(g => g.CtrlNbr)
                .ToListAsync(ct);
        }

        var result = new List<ControlNumber> { workArea.CtrlNbr };
        var queue = new Queue<ControlNumber>();
        queue.Enqueue(workArea.CtrlNbr);

        while (queue.Count > 0)
        {
            var parentCtrlNbr = queue.Dequeue();
            var childCtrlNbrs = await dbContext.Set<DynamicGroup>()
                .Where(g => g.ParentGroupCtrlNbr == parentCtrlNbr)
                .Select(g => g.CtrlNbr)
                .ToListAsync(ct);

            foreach (var childCtrlNbr in childCtrlNbrs)
            {
                result.Add(childCtrlNbr);
                queue.Enqueue(childCtrlNbr);
            }
        }

        return result;
    }

    private static string BuildShiftKey(
        ControlNumber workAreaCtrlNbr,
        ControlNumber shiftDefinitionCtrlNbr,
        DateOnly targetDate,
        ControlNumber? departmentCtrlNbr)
    {
        var dept = departmentCtrlNbr?.Value.ToString() ?? "none";
        return $"{workAreaCtrlNbr.Value}|{shiftDefinitionCtrlNbr.Value}|{targetDate:yyyy-MM-dd}|{dept}";
    }
}
