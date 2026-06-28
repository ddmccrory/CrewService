using CrewService.Application.DailyOperations;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Services;

internal sealed class AssignmentQueryService(CrewServiceDbContext dbContext) : IAssignmentQueryService
{
    public async Task<IReadOnlyList<AssignmentDto>> GetTemplatesForDateAsync(
        ControlNumber workAreaGroupCtrlNbr, ControlNumber shiftDefinitionCtrlNbr, DateOnly targetDate, ControlNumber? departmentCtrlNbr = null, CancellationToken ct = default)
    {
        var targetUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var now = DateTime.UtcNow;
        var dayBit = 1 << (int)targetDate.DayOfWeek;

        // Resolve the work area and all its descendant groups
        var descendantCtrlNbrs = await GetWorkAreaAndDescendantCtrlNbrsAsync(workAreaGroupCtrlNbr, ct);
        if (descendantCtrlNbrs.Count == 0) return [];

        // Find assignments for this work area (and descendants) that have a schedule matching the shift + day
        var schedules = await dbContext.Set<AssignmentSchedule>()
            .Where(s => s.ShiftDefinitionCtrlNbr == shiftDefinitionCtrlNbr
                        && (s.OperatingDaysMask & dayBit) != 0)
            .Select(s => new { s.AssignmentCtrlNbr, s.OnDutyTime, s.OffDutyTime })
            .ToListAsync(ct);

        var scheduleLookup = schedules
            .GroupBy(s => s.AssignmentCtrlNbr)
            .ToDictionary(g => g.Key, g => g.First());

        var scheduledAssignmentCtrlNbrs = scheduleLookup.Keys.ToList();

        if (scheduledAssignmentCtrlNbrs.Count == 0) return [];

        var assignments = await dbContext.Set<Assignment>()
            .Where(a => scheduledAssignmentCtrlNbrs.Contains(a.CtrlNbr)
                        && descendantCtrlNbrs.Contains(a.GroupCtrlNbr)
                        && a.IsActive && (departmentCtrlNbr == null || a.DepartmentCtrlNbr == departmentCtrlNbr))
            .ToListAsync(ct);

        if (assignments.Count == 0) return [];

        var assignmentCtrlNbrs = assignments.Select(a => a.CtrlNbr).ToList();

        // Find crew assignments covering these assignments on this day
        var crewAssignments = await dbContext.Set<CrewAssignment>()
            .Where(ca => assignmentCtrlNbrs.Contains(ca.AssignmentCtrlNbr)
                         && ca.StartUtc <= targetUtc && (ca.EndUtc == null || ca.EndUtc > targetUtc)
                         && (ca.DaysOfWeekMask & dayBit) != 0)
            .ToListAsync(ct);

        var crewCtrlNbrs = crewAssignments.Select(ca => ca.CrewCtrlNbr).Distinct().ToList();

        var crews = await dbContext.Set<Crew>()
            .Where(c => crewCtrlNbrs.Contains(c.CtrlNbr))
            .ToDictionaryAsync(c => c.CtrlNbr, ct);

        var positions = await dbContext.Set<CrewPosition>()
            .Where(p => crewCtrlNbrs.Contains(p.CrewCtrlNbr))
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);

        var positionCtrlNbrs = positions.Select(p => p.CtrlNbr).ToList();

        var incumbencies = await dbContext.Set<CrewIncumbency>()
            .Where(i => positionCtrlNbrs.Contains(i.CrewPositionCtrlNbr)
                        && i.StartUtc <= now
                        && (i.EndUtc == null || i.EndUtc > now))
            .ToListAsync(ct);

        // Resolve craft role names for each position
        var craftRoleCtrlNbrs = positions.Select(p => p.CraftRoleCtrlNbr).Distinct().ToList();
        var craftRoles = await dbContext.Set<CraftRole>()
            .Where(cr => craftRoleCtrlNbrs.Contains(cr.CtrlNbr))
            .ToListAsync(ct);
        var craftRoleLookup = craftRoles.ToDictionary(cr => cr.CtrlNbr, cr => cr.Name);

        // Resolve group names/codes for each assignment's group
        var groupCtrlNbrs = assignments.Select(a => a.GroupCtrlNbr).Distinct().ToList();
        var groups = await dbContext.Set<DynamicGroup>()
            .Where(g => groupCtrlNbrs.Contains(g.CtrlNbr))
            .ToDictionaryAsync(g => g.CtrlNbr, ct);

        var result = new List<AssignmentDto>();

        foreach (var assignment in assignments)
        {
            var crewIds = crewAssignments
                .Where(ca => ca.AssignmentCtrlNbr == assignment.CtrlNbr)
                .Select(ca => ca.CrewCtrlNbr)
                .ToHashSet();

            var positionDtos = positions
                .Where(p => crewIds.Contains(p.CrewCtrlNbr))
                .Select(p =>
                {
                    var incumbent = incumbencies
                        .FirstOrDefault(i => i.CrewPositionCtrlNbr == p.CtrlNbr);
                    craftRoleLookup.TryGetValue(p.CraftRoleCtrlNbr, out var roleName);
                    crews.TryGetValue(p.CrewCtrlNbr, out var crew);
                    return new CrewPositionDto(
                        p.CtrlNbr,
                        incumbent?.EmployeeCtrlNbr,
                        p.DisplayOrder,
                        roleName ?? string.Empty,
                        crew?.Name ?? string.Empty,
                        crew?.CrewType ?? "REGULAR");
                })
                .ToList();

            var schedule = scheduleLookup[assignment.CtrlNbr];
            groups.TryGetValue(assignment.GroupCtrlNbr, out var group);
            result.Add(new AssignmentDto(
                assignment.CtrlNbr,
                assignment.GroupCtrlNbr,
                assignment.DepartmentCtrlNbr,
                assignment.Code,
                assignment.Name,
                schedule.OnDutyTime,
                schedule.OffDutyTime,
                group?.Name ?? string.Empty,
                group?.Code ?? string.Empty,
                positionDtos));
        }

        return result;
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

    public async Task<IReadOnlyList<AssignmentDto>> GetExtraAssignmentsForShiftAsync(
        ControlNumber workAreaGroupCtrlNbr, ControlNumber shiftDefinitionCtrlNbr, DateOnly targetDate, ControlNumber? departmentCtrlNbr = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var dayBit = 1 << (int)targetDate.DayOfWeek;

        var descendantCtrlNbrs = await GetWorkAreaAndDescendantCtrlNbrsAsync(workAreaGroupCtrlNbr, ct);
        if (descendantCtrlNbrs.Count == 0) return [];

        var schedules = await dbContext.Set<AssignmentSchedule>()
            .Where(s => s.ShiftDefinitionCtrlNbr == shiftDefinitionCtrlNbr
                        && (s.OperatingDaysMask & dayBit) != 0)
            .Select(s => new { s.AssignmentCtrlNbr, s.OnDutyTime, s.OffDutyTime })
            .ToListAsync(ct);

        var scheduleLookup = schedules
            .GroupBy(s => s.AssignmentCtrlNbr)
            .ToDictionary(g => g.Key, g => g.First());

        var scheduledAssignmentCtrlNbrs = scheduleLookup.Keys.ToList();
        if (scheduledAssignmentCtrlNbrs.Count == 0) return [];

        var assignments = await dbContext.Set<Assignment>()
            .Where(a => scheduledAssignmentCtrlNbrs.Contains(a.CtrlNbr)
                        && descendantCtrlNbrs.Contains(a.GroupCtrlNbr)
                        && a.IsActive
                        && a.IsExtra
                        && (departmentCtrlNbr == null || a.DepartmentCtrlNbr == departmentCtrlNbr))
            .ToListAsync(ct);

        if (assignments.Count == 0) return [];

        var assignmentCtrlNbrs = assignments.Select(a => a.CtrlNbr).ToList();

        // For extra assignment templates, find any crew linked to the assignment
        // (regardless of date/day filters) so we can show the crew's position roster.
        var crewAssignments = await dbContext.Set<CrewAssignment>()
            .Where(ca => assignmentCtrlNbrs.Contains(ca.AssignmentCtrlNbr)
                         && (ca.EndUtc == null || ca.EndUtc > now))
            .ToListAsync(ct);

        var crewCtrlNbrs = crewAssignments.Select(ca => ca.CrewCtrlNbr).Distinct().ToList();

        var crews = await dbContext.Set<Crew>()
            .Where(c => crewCtrlNbrs.Contains(c.CtrlNbr))
            .ToDictionaryAsync(c => c.CtrlNbr, ct);

        var positions = await dbContext.Set<CrewPosition>()
            .Where(p => crewCtrlNbrs.Contains(p.CrewCtrlNbr))
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);

        var positionCtrlNbrs = positions.Select(p => p.CtrlNbr).ToList();

        var incumbencies = await dbContext.Set<CrewIncumbency>()
            .Where(i => positionCtrlNbrs.Contains(i.CrewPositionCtrlNbr)
                        && i.StartUtc <= now
                        && (i.EndUtc == null || i.EndUtc > now))
            .ToListAsync(ct);

        var craftRoleCtrlNbrs = positions.Select(p => p.CraftRoleCtrlNbr).Distinct().ToList();
        var craftRoles = await dbContext.Set<CraftRole>()
            .Where(cr => craftRoleCtrlNbrs.Contains(cr.CtrlNbr))
            .ToListAsync(ct);
        var craftRoleLookup = craftRoles.ToDictionary(cr => cr.CtrlNbr, cr => cr.Name);

        var groupCtrlNbrs = assignments.Select(a => a.GroupCtrlNbr).Distinct().ToList();
        var groups = await dbContext.Set<DynamicGroup>()
            .Where(g => groupCtrlNbrs.Contains(g.CtrlNbr))
            .ToDictionaryAsync(g => g.CtrlNbr, ct);

        var result = new List<AssignmentDto>();

        foreach (var assignment in assignments)
        {
            var crewIds = crewAssignments
                .Where(ca => ca.AssignmentCtrlNbr == assignment.CtrlNbr)
                .Select(ca => ca.CrewCtrlNbr)
                .ToHashSet();

            var positionDtos = positions
                .Where(p => crewIds.Contains(p.CrewCtrlNbr))
                .Select(p =>
                {
                    var incumbent = incumbencies
                        .FirstOrDefault(i => i.CrewPositionCtrlNbr == p.CtrlNbr);
                    craftRoleLookup.TryGetValue(p.CraftRoleCtrlNbr, out var roleName);
                    crews.TryGetValue(p.CrewCtrlNbr, out var crew);
                    return new CrewPositionDto(
                        p.CtrlNbr,
                        incumbent?.EmployeeCtrlNbr,
                        p.DisplayOrder,
                        roleName ?? string.Empty,
                        crew?.Name ?? string.Empty,
                        crew?.CrewType ?? "REGULAR");
                })
                .ToList();

            var schedule = scheduleLookup[assignment.CtrlNbr];
            groups.TryGetValue(assignment.GroupCtrlNbr, out var group);
            result.Add(new AssignmentDto(
                assignment.CtrlNbr,
                assignment.GroupCtrlNbr,
                assignment.DepartmentCtrlNbr,
                assignment.Code,
                assignment.Name,
                schedule.OnDutyTime,
                schedule.OffDutyTime,
                group?.Name ?? string.Empty,
                group?.Code ?? string.Empty,
                positionDtos));
        }

        return result;
    }
}
