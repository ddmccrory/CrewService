using CrewService.Application.DailyOperations;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.DailyOperations;

internal sealed class AssignmentTemplateQueryService(CrewServiceDbContext dbContext) : IAssignmentTemplateQueryService
{
    public async Task<IReadOnlyList<AssignmentTemplateDto>> GetTemplatesForDateAsync(
        ControlNumber workAreaGroupCtrlNbr, DateOnly targetDate, CancellationToken ct = default)
    {
        var targetUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var now = DateTime.UtcNow;

        var templates = await dbContext.Set<AssignmentTemplate>()
            .Where(t => t.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && t.IsActive)
            .ToListAsync(ct);

        if (templates.Count == 0) return [];

        var templateCtrlNbrs = templates.Select(t => t.CtrlNbr).ToList();

        var attachments = await dbContext.Set<CrewAttachmentTemplate>()
            .Where(a => templateCtrlNbrs.Contains(a.AssignmentTemplateCtrlNbr)
                        && a.StartUtc <= targetUtc
                        && (a.EndUtc == null || a.EndUtc > targetUtc))
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

        var result = new List<AssignmentTemplateDto>();

        foreach (var template in templates)
        {
            var crewIds = attachments
                .Where(a => a.AssignmentTemplateCtrlNbr == template.CtrlNbr)
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

            result.Add(new AssignmentTemplateDto(
                template.CtrlNbr,
                template.WorkAreaGroupCtrlNbr,
                positionDtos));
        }

        return result;
    }
}
