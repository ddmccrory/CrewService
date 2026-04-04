using CrewService.Application.DailyOperations;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.DailyOperations;

internal sealed class AssignmentQueryService(CrewServiceDbContext dbContext) : IAssignmentQueryService
{
    public async Task<IReadOnlyList<AssignmentDto>> GetTemplatesForDateAsync(
        ControlNumber workAreaGroupCtrlNbr, ControlNumber shiftDefinitionCtrlNbr, DateOnly targetDate, CancellationToken ct = default)
    {
        var targetUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var now = DateTime.UtcNow;
        var dayBit = 1 << (int)targetDate.DayOfWeek;

        // Find assignments for this work area that have a schedule matching the shift + day
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
                        && a.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr
                        && a.IsActive)
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
                    return new CrewPositionDto(
                        p.CtrlNbr,
                        incumbent?.EmployeeCtrlNbr,
                        p.DisplayOrder,
                        roleName ?? string.Empty);
                })
                .ToList();

            var schedule = scheduleLookup[assignment.CtrlNbr];
            result.Add(new AssignmentDto(
                assignment.CtrlNbr,
                workAreaGroupCtrlNbr,
                assignment.DepartmentCtrlNbr,
                assignment.Code,
                assignment.Name,
                schedule.OnDutyTime,
                schedule.OffDutyTime,
                positionDtos));
        }

        return result;
    }
}
