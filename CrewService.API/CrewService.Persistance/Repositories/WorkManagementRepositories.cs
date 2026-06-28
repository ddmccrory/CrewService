using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;


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

internal sealed class CraftRoleRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<CraftRole>(dbContext, currentUserService), ICraftRoleRepository
{
    public async Task<List<CraftRole>> GetByCraftAsync(ControlNumber craftCtrlNbr)
    {
        return await DbContext.Set<CraftRole>()
            .Where(p => p.CraftCtrlNbr == craftCtrlNbr)
            .OrderBy(p => p.Code)
            .ToListAsync();
    }

    public async Task<List<CraftRole>> GetByDepartmentAsync(ControlNumber departmentCtrlNbr)
    {
        var craftCtrlNbrs = await DbContext.Set<Craft>()
            .Where(c => c.DepartmentCtrlNbr == departmentCtrlNbr)
            .Select(c => c.CtrlNbr)
            .ToListAsync();

        return await DbContext.Set<CraftRole>()
            .Where(r => craftCtrlNbrs.Contains(r.CraftCtrlNbr))
            .OrderBy(r => r.Code)
            .ToListAsync();
    }

    public async Task<List<CraftRole>> GetByRailroadAsync(ControlNumber railroadCtrlNbr)
    {
        var departmentCtrlNbrs = await DbContext.Set<Department>()
            .Where(d => d.DynamicGroupCtrlNbr == railroadCtrlNbr)
            .Select(d => d.CtrlNbr)
            .ToListAsync();

        var craftCtrlNbrs = await DbContext.Set<Craft>()
            .Where(c => c.DepartmentCtrlNbr != null && departmentCtrlNbrs.Contains(c.DepartmentCtrlNbr))
            .Select(c => c.CtrlNbr)
            .ToListAsync();

        return await DbContext.Set<CraftRole>()
            .Where(r => craftCtrlNbrs.Contains(r.CraftCtrlNbr))
            .OrderBy(r => r.Code)
            .ToListAsync();
    }

    public async Task<CraftRole?> GetByCtrlNbrWithQualificationsAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<CraftRole>()
            .Include("_requiredQualifications")
            .FirstOrDefaultAsync(r => r.CtrlNbr == ctrlNbr, ct);
    }
}


internal sealed class CraftRoleQualificationRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<CraftRoleQualification>(dbContext, currentUserService), ICraftRoleQualificationRepository
{
    public async Task<List<CraftRoleQualification>> GetByCraftRoleAsync(ControlNumber craftRoleCtrlNbr)
    {
        return await DbContext.Set<CraftRoleQualification>()
            .Where(q => q.CraftRoleCtrlNbr == craftRoleCtrlNbr)
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

internal sealed class DepartmentRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Department>(dbContext, currentUserService), IDepartmentRepository
{
    public async Task<List<Department>> GetByParentAndRailroadAsync(ControlNumber? parentCtrlNbr, ControlNumber? railroadCtrlNbr)
    {
        return await DbContext.Set<Department>()
            .Where(d => d.ParentCtrlNbr == parentCtrlNbr
                && (d.DynamicGroupCtrlNbr == null || d.DynamicGroupCtrlNbr == railroadCtrlNbr))
            .OrderBy(d => d.Name)
            .ToListAsync();
    }
}
