using CrewService.Application.DailyOperations;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.DailyOperations;

internal sealed class AssignmentQueryService(CrewServiceDbContext dbContext) : IAssignmentQueryService
{
    public async Task<IReadOnlyList<AssignmentDto>> GetTemplatesForDateAsync(
        ControlNumber workAreaGroupCtrlNbr, DateOnly targetDate, CancellationToken ct = default)
    {
        var targetUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var now = DateTime.UtcNow;

        var assignmentTypeIds = await dbContext.Set<GroupType>()
            .Where(gt => gt.Name == "Assignment")
            .Select(gt => gt.CtrlNbr)
            .ToListAsync(ct);

        if (assignmentTypeIds.Count == 0) return [];

        var workAreaPath = (await dbContext.Set<DynamicGroup>()
            .Where(g => g.CtrlNbr == workAreaGroupCtrlNbr)
            .Select(g => g.Path)
            .FirstOrDefaultAsync(ct)) ?? "";

        var assignments = await dbContext.Set<DynamicGroup>()
            .Where(g => assignmentTypeIds.Contains(g.GroupTypeCtrlNbr)
                && g.Path != null && g.Path.StartsWith(workAreaPath + "/"))
            .ToListAsync(ct);

        if (assignments.Count == 0) return [];

        var assignmentCtrlNbrs = assignments.Select(a => a.CtrlNbr).ToList();

        var dayBit = 1 << (int)targetDate.DayOfWeek;
        var attachments = await dbContext.Set<CrewAssignment>()
            .Where(a => assignmentCtrlNbrs.Contains(a.AssignmentGroupCtrlNbr)
                        && a.StartUtc <= targetUtc && (a.EndUtc == null || a.EndUtc > targetUtc) && (a.DaysOfWeekMask & dayBit) != 0)
            .ToListAsync(ct);

        var crewCtrlNbrs = attachments.Select(a => a.CrewCtrlNbr).Distinct().ToList();

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

        var result = new List<AssignmentDto>();

        foreach (var assignment in assignments)
        {
            var crewIds = attachments
                .Where(a => a.AssignmentGroupCtrlNbr == assignment.CtrlNbr)
                .Select(a => a.CrewCtrlNbr)
                .ToHashSet();

            var positionDtos = positions
                .Where(p => crewIds.Contains(p.CrewCtrlNbr))
                .Select(p =>
                {
                    var incumbent = incumbencies
                        .FirstOrDefault(i => i.CrewPositionCtrlNbr == p.CtrlNbr);
                    return new CrewPositionDto(
                        p.CtrlNbr,
                        incumbent?.EmployeeCtrlNbr,
                        p.DisplayOrder);
                })
                .ToList();

            result.Add(new AssignmentDto(
                assignment.CtrlNbr,
                workAreaGroupCtrlNbr,
                positionDtos));
        }

        return result;
    }
}
