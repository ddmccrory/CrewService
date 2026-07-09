using CrewService.Application.DailyOperations;
using CrewService.Application.Time;
using CrewService.Domain.Modules.Crews;
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
    public async Task<DateTime?> GetNextCallSheetEventUtcAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
    {
        var nowUtc = DateTime.SpecifyKind(clock.UtcNow.UtcDateTime, DateTimeKind.Utc);
        var descendants = await GetWorkAreaAndDescendantCtrlNbrsAsync(workAreaGroupCtrlNbr, ct);
        if (descendants.Count == 0)
            return null;

        var groups = await dbContext.Set<DynamicGroup>()
            .Where(g => descendants.Contains(g.CtrlNbr))
            .ToListAsync(ct);

        var assignments = await dbContext.Set<Assignment>()
            .Where(a => a.IsActive && descendants.Contains(a.GroupCtrlNbr))
            .Select(a => new { a.CtrlNbr, a.DepartmentCtrlNbr })
            .ToListAsync(ct);

        if (assignments.Count == 0)
            return null;

        var assignmentIds = assignments.Select(a => a.CtrlNbr).ToList();

        var schedules = await dbContext.Set<AssignmentSchedule>()
            .Where(s => assignmentIds.Contains(s.AssignmentCtrlNbr))
            .ToListAsync(ct);

        if (schedules.Count == 0)
            return null;

        var shiftDefs = await dbContext.Set<ShiftDefinition>()
            .Where(sd => sd.IsActive && descendants.Contains(sd.WorkAreaGroupCtrlNbr))
            .ToListAsync(ct);

        if (shiftDefs.Count == 0)
            return null;

        var shiftDefLookup = shiftDefs.ToDictionary(sd => sd.CtrlNbr);
        var assignmentLookup = assignments.ToDictionary(a => a.CtrlNbr);
        var workAreaByShift = shiftDefs
            .GroupBy(sd => sd.CtrlNbr)
            .ToDictionary(g => g.Key, g => g.First().WorkAreaGroupCtrlNbr);

        var candidates = new List<(DailyCallSheetDueWorkItem Item, DateTime EventUtc)>();

        for (var dayOffset = 0; dayOffset <= 7; dayOffset++)
        {
            var date = DateOnly.FromDateTime(nowUtc).AddDays(dayOffset);
            var dayBit = 1 << (int)date.DayOfWeek;

            foreach (var schedule in schedules)
            {
                if ((schedule.OperatingDaysMask & dayBit) == 0)
                    continue;

                if (!shiftDefLookup.ContainsKey(schedule.ShiftDefinitionCtrlNbr))
                    continue;

                if (!assignmentLookup.TryGetValue(schedule.AssignmentCtrlNbr, out var assignment))
                    continue;

                var localWorkArea = workAreaByShift[schedule.ShiftDefinitionCtrlNbr];
                var localGroup = groups.FirstOrDefault(g => g.CtrlNbr == localWorkArea);
                var tz = clock.ResolveTimeZone(localGroup?.TimeZoneId);
                var eventUtc = clock.CombineLocalToUtc(date, schedule.OnDutyTime, tz).UtcDateTime;

                if (eventUtc <= nowUtc)
                    continue;

                candidates.Add((
                    new DailyCallSheetDueWorkItem(
                        localWorkArea,
                        schedule.ShiftDefinitionCtrlNbr,
                        date,
                        assignment.DepartmentCtrlNbr),
                    eventUtc));
            }
        }

        if (candidates.Count == 0)
            return null;

        var nextEventUtc = candidates
            .GroupBy(c => new
            {
                c.Item.WorkAreaGroupCtrlNbr,
                c.Item.ShiftDefinitionCtrlNbr,
                c.Item.TargetDate,
                c.Item.DepartmentCtrlNbr
            })
            .Select(g => g.Min(x => x.EventUtc))
            .Min();

        return nextEventUtc;
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

        var shiftDefLookup = shiftDefs.ToDictionary(sd => sd.CtrlNbr);
        var assignmentLookup = assignments.ToDictionary(a => a.CtrlNbr);

        var startDate = DateOnly.FromDateTime(nowUtc.AddDays(-1));
        var endDate = DateOnly.FromDateTime(nowUtc);
        var due = new List<(DailyCallSheetDueWorkItem Item, DateTime EventUtc)>();

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
                var tz = clock.ResolveTimeZone(workArea?.TimeZoneId);
                var eventUtc = clock.CombineLocalToUtc(date, schedule.OnDutyTime, tz).UtcDateTime;

                if (eventUtc > nowUtc)
                    continue;

                if (eventUtc < nowUtc.AddDays(-1))
                    continue;

                due.Add((
                    new DailyCallSheetDueWorkItem(
                        shiftDef.WorkAreaGroupCtrlNbr,
                        schedule.ShiftDefinitionCtrlNbr,
                        date,
                        assignment.DepartmentCtrlNbr),
                    eventUtc));
            }
        }

        if (due.Count == 0)
            return [];

        var distinctDue = due
            .GroupBy(d => new
            {
                d.Item.WorkAreaGroupCtrlNbr,
                d.Item.ShiftDefinitionCtrlNbr,
                d.Item.TargetDate,
                d.Item.DepartmentCtrlNbr
            })
            .Select(g => g.OrderBy(x => x.EventUtc).First().Item)
            .ToList();

        var workAreaIds = distinctDue.Select(d => d.WorkAreaGroupCtrlNbr).Distinct().ToList();
        var minDate = distinctDue.Min(d => d.TargetDate).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var maxDateExclusive = distinctDue.Max(d => d.TargetDate).AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var workInstances = await dbContext.Set<WorkInstance>()
            .Where(w => workAreaIds.Contains(w.WorkAreaGroupCtrlNbr)
                        && w.StartUtc >= minDate
                        && w.StartUtc < maxDateExclusive)
            .ToListAsync(ct);

        var workInstanceIds = workInstances.Select(w => w.CtrlNbr).ToList();
        var existingShifts = workInstanceIds.Count == 0
            ? []
            : await dbContext.Set<ShiftInstance>()
                .Where(s => workInstanceIds.Contains(s.WorkInstanceCtrlNbr))
                .ToListAsync(ct);

        var existingShiftKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var shift in existingShifts)
        {
            var wi = workInstances.FirstOrDefault(w => w.CtrlNbr == shift.WorkInstanceCtrlNbr);
            if (wi is null)
                continue;

            var day = DateOnly.FromDateTime(wi.StartUtc);
            var key = BuildShiftKey(wi.WorkAreaGroupCtrlNbr, shift.ShiftDefinitionCtrlNbr, day, shift.DepartmentCtrlNbr);
            existingShiftKeys.Add(key);
        }

        var results = new List<DailyCallSheetDueWorkItem>();
        foreach (var item in distinctDue)
        {
            var key = BuildShiftKey(item.WorkAreaGroupCtrlNbr, item.ShiftDefinitionCtrlNbr, item.TargetDate, item.DepartmentCtrlNbr);
            if (existingShiftKeys.Contains(key))
                continue;

            results.Add(item);
        }

        return results;
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
