using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Queries;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

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
            .Include(s => s.BoardSlots)
            .Include(s => s.AssignmentNotes)
            .SingleOrDefaultAsync(s => s.CtrlNbr == ctrlNbr, ct);

    public async Task<IReadOnlyList<ShiftInstance>> GetByWorkInstanceAsync(
        ControlNumber workInstanceCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<ShiftInstance>()
            .Include(s => s.PositionSlots)
            .Include(s => s.BoardSlots)
            .Include(s => s.AssignmentNotes)
            .Where(s => s.WorkInstanceCtrlNbr == workInstanceCtrlNbr)
            .OrderBy(s => s.ShiftCode)
            .ToListAsync(ct);

    public async Task<bool> ExistsByWorkInstanceAndShiftCodeAsync(
        ControlNumber workInstanceCtrlNbr, string shiftCode, ControlNumber? departmentCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<ShiftInstance>()
            .AnyAsync(s => s.WorkInstanceCtrlNbr == workInstanceCtrlNbr && s.ShiftCode == shiftCode && s.DepartmentCtrlNbr == departmentCtrlNbr, ct);

    public async Task<IReadOnlyList<ShiftInstance>> GetIncompleteByCrewPositionAsync(
        ControlNumber crewPositionCtrlNbr,
        CancellationToken ct = default) =>
        await DbContext.Set<ShiftInstance>()
            .Include(s => s.PositionSlots)
            .Include(s => s.BoardSlots)
            .Include(s => s.AssignmentNotes)
            .Where(s => !s.IsComplete && s.PositionSlots.Any(ps => ps.CrewPositionCtrlNbr == crewPositionCtrlNbr))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ShiftInstance>> GetIncompleteByIncumbentEmployeeAsync(
        ControlNumber employeeCtrlNbr,
        CancellationToken ct = default) =>
        await DbContext.Set<ShiftInstance>()
            .Include(s => s.PositionSlots)
            .Include(s => s.BoardSlots)
            .Include(s => s.AssignmentNotes)
            .Where(s => !s.IsComplete && s.PositionSlots.Any(ps => ps.IncumbentEmployeeCtrlNbr == employeeCtrlNbr))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ShiftInstance>> GetIncompleteByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr,
        CancellationToken ct = default) =>
        await DbContext.Set<ShiftInstance>()
            .Include(s => s.PositionSlots)
            .Include(s => s.BoardSlots)
            .Include(s => s.AssignmentNotes)
            .Where(s => !s.IsComplete
                && DbContext.Set<WorkInstance>()
                    .Any(w => w.CtrlNbr == s.WorkInstanceCtrlNbr && w.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr))
            .ToListAsync(ct);
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

    public async Task<IReadOnlyList<OnDutyCompletionStatus>> GetCompletionStatusesForShiftAsync(
        ControlNumber shiftInstanceCtrlNbr,
        CancellationToken ct = default)
    {
        var completionStatusValues = await (
            from onDuty in DbContext.Set<OnDutyRecord>()
            join slot in DbContext.Set<PositionSlotInstance>() on onDuty.PositionSlotCtrlNbr equals slot.CtrlNbr
            where slot.ShiftInstanceCtrlNbr == shiftInstanceCtrlNbr
            select onDuty.CompletionStatus.Value)
            .ToListAsync(ct);

        return completionStatusValues
            .Select(OnDutyCompletionStatus.FromValue)
            .ToList();
    }

    public async Task<OnDutyTieUpContext?> GetTieUpContextAsync(
        ControlNumber onDutyRecordCtrlNbr,
        CancellationToken ct = default)
    {
        var row = await (
            from onDuty in DbContext.Set<OnDutyRecord>()
            join slot in DbContext.Set<PositionSlotInstance>() on onDuty.PositionSlotCtrlNbr equals slot.CtrlNbr
            join shift in DbContext.Set<ShiftInstance>() on slot.ShiftInstanceCtrlNbr equals shift.CtrlNbr
            join work in DbContext.Set<WorkInstance>() on shift.WorkInstanceCtrlNbr equals work.CtrlNbr
            where onDuty.CtrlNbr == onDutyRecordCtrlNbr
            select new
            {
                onDuty.CtrlNbr,
                slot.AssignmentCode,
                slot.ShiftInstanceCtrlNbr,
                work.WorkAreaGroupCtrlNbr
            })
            .SingleOrDefaultAsync(ct);

        return row is null
            ? null
            : new OnDutyTieUpContext(
                row.CtrlNbr,
                row.AssignmentCode,
                row.ShiftInstanceCtrlNbr,
                row.WorkAreaGroupCtrlNbr);
    }

    public async Task<IReadOnlyList<OnDutyRecord>> GetByPositionSlotsAsync(
        IReadOnlyList<ControlNumber> positionSlotCtrlNbrs, CancellationToken ct = default)
    {
        if (positionSlotCtrlNbrs.Count == 0) return [];
        var slotList = positionSlotCtrlNbrs.ToList();
        return await DbContext.Set<OnDutyRecord>()
            .Where(r => slotList.Contains(r.PositionSlotCtrlNbr))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OnDutyRecord>> GetOpenForEmployeeAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<OnDutyRecord>()
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr && r.Status != OnDutyStatus.TiedUp)
            .OrderByDescending(r => r.OnDutyTimeUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<OnDutyRecord>> GetIncompleteForEmployeeAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        var records = await DbContext.Set<OnDutyRecord>()
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr && r.CompletionStatus != OnDutyCompletionStatus.Completed)
            .OrderByDescending(r => r.OnDutyTimeUtc)
            .ToListAsync(ct);

        return records
            .Where(r => r.CompletionStatus != OnDutyCompletionStatus.Completed)
            .ToList();
    }

    public async Task<IReadOnlyList<OnDutyRecord>> GetNotStartedForRailroadAsync(
        ControlNumber railroadCtrlNbr, CancellationToken ct = default)
    {
        var railroadWorkAreaIds = await DbContext.Set<DynamicGroup>()
            .Where(g => g.IsWorkArea)
            .WhereOwnedByRailroad(railroadCtrlNbr)
            .Select(g => g.CtrlNbr)
            .ToListAsync(ct);

        if (railroadWorkAreaIds.Count == 0)
            return [];

        var query = from onDuty in DbContext.Set<OnDutyRecord>()
                    join slot in DbContext.Set<PositionSlotInstance>() on onDuty.PositionSlotCtrlNbr equals slot.CtrlNbr
                    join shift in DbContext.Set<ShiftInstance>() on slot.ShiftInstanceCtrlNbr equals shift.CtrlNbr
                    join work in DbContext.Set<WorkInstance>() on shift.WorkInstanceCtrlNbr equals work.CtrlNbr
                    where railroadWorkAreaIds.Contains(work.WorkAreaGroupCtrlNbr)
                    select onDuty;

        var records = await query
            .Distinct()
            .OrderBy(r => r.OnDutyTimeUtc)
            .ToListAsync(ct);

        return records
            .Where(r => r.CompletionStatus != OnDutyCompletionStatus.Completed)
            .ToList();
    }

    public async Task<IReadOnlyList<OnDutyRecord>> GetForEmployeeInRangeAsync(
        ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc, CancellationToken ct = default) =>
        // History surfaces completed tours only (parity with the legacy pay-period slices, which
        // required an off-duty/tie-up record). Open/scheduled records belong on the Work &amp; Staffing tab.
        await DbContext.Set<OnDutyRecord>()
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr
                        && r.Status == OnDutyStatus.TiedUp
                        && r.OnDutyTimeUtc >= startUtc
                        && r.OnDutyTimeUtc < endUtc)
            .OrderByDescending(r => r.OnDutyTimeUtc)
            .ToListAsync(ct);
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

    public async Task<IReadOnlyList<OffDutyRecord>> GetByOnDutyRecordsAsync(
        IReadOnlyList<ControlNumber> onDutyRecordCtrlNbrs, CancellationToken ct = default)
    {
        if (onDutyRecordCtrlNbrs.Count == 0) return [];
        var idList = onDutyRecordCtrlNbrs.ToList();
        return await DbContext.Set<OffDutyRecord>()
            .Where(r => idList.Contains(r.OnDutyRecordCtrlNbr))
            .ToListAsync(ct);
    }
}

internal sealed class CraftOperationsPolicyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<CraftOperationsPolicy>(dbContext, currentUserService), ICraftOperationsPolicyRepository
{
    public async Task<CraftOperationsPolicy?> GetByCraftAsync(
        ControlNumber craftCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<CraftOperationsPolicy>()
            .SingleOrDefaultAsync(p => p.CraftCtrlNbr == craftCtrlNbr, ct);
}
