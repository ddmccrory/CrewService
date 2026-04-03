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
    public async Task<List<Assignment>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr) =>
        await DbContext.Set<Assignment>().Where(a => a.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr).OrderBy(a => a.Code).ToListAsync();

    public async Task<List<Assignment>> GetAllByRailroadAsync(ControlNumber railroadCtrlNbr)
    {
        var groupCtrlNbrs = await DbContext.Set<DynamicGroup>()
            .Where(g => g.CtrlNbr == railroadCtrlNbr || g.RailroadCtrlNbr == railroadCtrlNbr)
            .Select(g => g.CtrlNbr)
            .ToListAsync();

        return await DbContext.Set<Assignment>()
            .Where(a => groupCtrlNbrs.Contains(a.WorkAreaGroupCtrlNbr))
            .OrderBy(a => a.Code)
            .ToListAsync();
    }
}

internal sealed class AssignmentScheduleRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<AssignmentSchedule>(dbContext, currentUserService), IAssignmentScheduleRepository
{
    public async Task<List<AssignmentSchedule>> GetByAssignmentAsync(ControlNumber assignmentCtrlNbr) =>
        await DbContext.Set<AssignmentSchedule>().Where(s => s.AssignmentCtrlNbr == assignmentCtrlNbr).ToListAsync();

    public async Task<List<AssignmentSchedule>> GetByShiftDefinitionAsync(ControlNumber shiftDefinitionCtrlNbr) =>
        await DbContext.Set<AssignmentSchedule>().Where(s => s.ShiftDefinitionCtrlNbr == shiftDefinitionCtrlNbr).ToListAsync();
}
