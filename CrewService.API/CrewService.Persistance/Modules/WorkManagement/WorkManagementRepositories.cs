using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.WorkManagement;

internal sealed class AssignmentTemplateRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<AssignmentTemplate>(dbContext, currentUserService), IAssignmentTemplateRepository
{
    public async Task<List<AssignmentTemplate>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr)
    {
        return await DbContext.Set<AssignmentTemplate>()
            .Where(t => t.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr)
            .OrderBy(t => t.Code)
            .ToListAsync();
    }
}

internal sealed class WorkInstanceRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<WorkInstance>(dbContext, currentUserService), IWorkInstanceRepository
{
    public async Task<List<WorkInstance>> GetByWorkAreaAndDateRangeAsync(ControlNumber workAreaGroupCtrlNbr, DateTime startUtc, DateTime endUtc)
    {
        return await DbContext.Set<WorkInstance>()
            .Where(w => w.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && w.StartUtc >= startUtc && w.EndUtc <= endUtc)
            .OrderBy(w => w.StartUtc)
            .ToListAsync();
    }
}

internal sealed class PositionRoleRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<PositionRole>(dbContext, currentUserService), IPositionRoleRepository
{
    public async Task<List<PositionRole>> GetByCraftAsync(ControlNumber craftCtrlNbr)
    {
        return await DbContext.Set<PositionRole>()
            .Where(p => p.CraftCtrlNbr == craftCtrlNbr)
            .OrderBy(p => p.Code)
            .ToListAsync();
    }
}

internal sealed class PositionSlotRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<PositionSlot>(dbContext, currentUserService), IPositionSlotRepository
{
    public async Task<List<PositionSlot>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr)
    {
        return await DbContext.Set<PositionSlot>()
            .Where(s => s.WorkInstanceCtrlNbr == workInstanceCtrlNbr)
            .ToListAsync();
    }

    public async Task<List<PositionSlot>> GetOpenByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr)
    {
        return await DbContext.Set<PositionSlot>()
            .Where(s => s.WorkInstanceCtrlNbr == workInstanceCtrlNbr && s.BoundEmployeeCtrlNbr == null)
            .ToListAsync();
    }
}

internal sealed class SlotRequirementRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<SlotRequirement>(dbContext, currentUserService), ISlotRequirementRepository
{
    public async Task<List<SlotRequirement>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr)
    {
        return await DbContext.Set<SlotRequirement>()
            .Where(r => r.PositionSlotCtrlNbr == positionSlotCtrlNbr)
            .OrderBy(r => r.Priority)
            .ToListAsync();
    }
}
