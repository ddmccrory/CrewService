using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Crews;

internal sealed class AssignmentRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Assignment>(dbContext, currentUserService), IAssignmentRepository
{
    public async Task<List<Assignment>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr)
    {
        var descendantCtrlNbrs = await GetWorkAreaAndDescendantCtrlNbrsAsync(workAreaGroupCtrlNbr);

        return await DbContext.Set<Assignment>()
            .Where(a => descendantCtrlNbrs.Contains(a.GroupCtrlNbr))
            .OrderBy(a => a.Code)
            .ToListAsync();
    }

    public async Task<List<Assignment>> GetByWorkAreaAndDepartmentAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber departmentCtrlNbr)
    {
        var descendantCtrlNbrs = await GetWorkAreaAndDescendantCtrlNbrsAsync(workAreaGroupCtrlNbr);

        return await DbContext.Set<Assignment>()
            .Where(a => descendantCtrlNbrs.Contains(a.GroupCtrlNbr) && a.DepartmentCtrlNbr == departmentCtrlNbr)
            .OrderBy(a => a.Code)
            .ToListAsync();
    }

    public async Task<List<Assignment>> GetAllByRailroadAsync(ControlNumber railroadCtrlNbr)
    {
        var railroadGroupCtrlNbrs = await DbContext.Set<DynamicGroup>()
            .Where(g => g.RailroadCtrlNbr == railroadCtrlNbr)
            .Select(g => g.CtrlNbr)
            .ToListAsync();

        return await DbContext.Set<Assignment>()
            .Where(a => railroadGroupCtrlNbrs.Contains(a.GroupCtrlNbr))
            .OrderBy(a => a.Code)
            .ToListAsync();
    }

    private async Task<List<ControlNumber>> GetWorkAreaAndDescendantCtrlNbrsAsync(ControlNumber workAreaGroupCtrlNbr)
    {
        var workArea = await DbContext.Set<DynamicGroup>()
            .SingleOrDefaultAsync(g => g.CtrlNbr == workAreaGroupCtrlNbr);

        if (workArea is null)
            return [];

        if (workArea.Path is not null)
        {
            var prefix = workArea.Path + "/";
            return await DbContext.Set<DynamicGroup>()
                .Where(g => g.Path != null && (g.Path == workArea.Path || g.Path.StartsWith(prefix)))
                .Select(g => g.CtrlNbr)
                .ToListAsync();
        }

        var result = new List<ControlNumber> { workArea.CtrlNbr };
        var queue = new Queue<ControlNumber>();
        queue.Enqueue(workArea.CtrlNbr);

        while (queue.Count > 0)
        {
            var parentCtrlNbr = queue.Dequeue();
            var childCtrlNbrs = await DbContext.Set<DynamicGroup>()
                .Where(g => g.ParentGroupCtrlNbr == parentCtrlNbr)
                .Select(g => g.CtrlNbr)
                .ToListAsync();

            foreach (var childCtrlNbr in childCtrlNbrs)
            {
                result.Add(childCtrlNbr);
                queue.Enqueue(childCtrlNbr);
            }
        }

        return result;
    }
}

internal sealed class AssignmentScheduleRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<AssignmentSchedule>(dbContext, currentUserService), IAssignmentScheduleRepository
{
    public async Task<List<AssignmentSchedule>> GetByAssignmentAsync(ControlNumber assignmentCtrlNbr)
    {
        return await DbContext.Set<AssignmentSchedule>()
            .Where(s => s.AssignmentCtrlNbr == assignmentCtrlNbr)
            .ToListAsync();
    }

    public async Task<List<AssignmentSchedule>> GetByShiftDefinitionAsync(ControlNumber shiftDefinitionCtrlNbr)
    {
        return await DbContext.Set<AssignmentSchedule>()
            .Where(s => s.ShiftDefinitionCtrlNbr == shiftDefinitionCtrlNbr)
            .ToListAsync();
    }
}
