using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.DailyOperations;

internal sealed class ShiftDefinitionRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<ShiftDefinition>(dbContext, currentUserService), IShiftDefinitionRepository
{
    public async Task<List<ShiftDefinition>> GetByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr) =>
        await DbContext.Set<ShiftDefinition>()
            .Where(s => s.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
}

internal sealed class ShiftInstanceRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<ShiftInstance>(dbContext, currentUserService), IShiftInstanceRepository
{
    public override async Task<ShiftInstance?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<ShiftInstance>()
            .Include(s => s.PositionSlots)
            .Include(s => s.AssignmentNotes)
            .SingleOrDefaultAsync(s => s.CtrlNbr == ctrlNbr, ct);

    public async Task<IReadOnlyList<ShiftInstance>> GetByWorkInstanceAsync(
        ControlNumber workInstanceCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<ShiftInstance>()
            .Include(s => s.PositionSlots)
            .Include(s => s.AssignmentNotes)
            .Where(s => s.WorkInstanceCtrlNbr == workInstanceCtrlNbr)
            .OrderBy(s => s.ShiftCode)
            .ToListAsync(ct);

    public async Task<bool> ExistsByWorkInstanceAndShiftCodeAsync(
        ControlNumber workInstanceCtrlNbr, string shiftCode, ControlNumber? departmentCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<ShiftInstance>()
            .AnyAsync(s => s.WorkInstanceCtrlNbr == workInstanceCtrlNbr && s.ShiftCode == shiftCode && s.DepartmentCtrlNbr == departmentCtrlNbr, ct);
}

internal sealed class OnDutyRecordRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<OnDutyRecord>(dbContext, currentUserService), IOnDutyRecordRepository
{
    public async Task<IReadOnlyList<OnDutyRecord>> GetRecentForEmployeeAsync(
        ControlNumber employeeCtrlNbr, int dayCount, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-dayCount);
        return await DbContext.Set<OnDutyRecord>()
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr && r.OnDutyTimeUtc >= cutoff)
            .OrderByDescending(r => r.OnDutyTimeUtc)
            .ToListAsync(ct);
    }
}

internal sealed class OffDutyRecordRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<OffDutyRecord>(dbContext, currentUserService), IOffDutyRecordRepository
{
    public async Task<OffDutyRecord?> GetLastForEmployeeAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<OffDutyRecord>()
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(r => r.OffDutyTimeUtc)
            .FirstOrDefaultAsync(ct);
}

internal sealed class CraftOperationsPolicyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<CraftOperationsPolicy>(dbContext, currentUserService), ICraftOperationsPolicyRepository
{
    public async Task<CraftOperationsPolicy?> GetByCraftAsync(
        ControlNumber craftCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<CraftOperationsPolicy>()
            .SingleOrDefaultAsync(p => p.CraftCtrlNbr == craftCtrlNbr, ct);
}
