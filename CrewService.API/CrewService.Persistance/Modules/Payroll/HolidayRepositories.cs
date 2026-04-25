using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.HolidayManagement;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Payroll;

internal sealed class HolidayRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Holiday>(dbContext, currentUserService), IHolidayRepository
{
    public async Task<IReadOnlyList<Holiday>> GetActiveByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<Holiday>()
            .Where(h => h.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && h.IsActive)
            .OrderBy(h => h.ObservedDate)
            .ToListAsync(ct);
}

internal sealed class HolidayQualificationRuleRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<HolidayQualificationRule>(dbContext, currentUserService), IHolidayQualificationRuleRepository
{
    public async Task<IReadOnlyList<HolidayQualificationRule>> GetByHolidayAsync(
        ControlNumber holidayCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<HolidayQualificationRule>()
            .Where(r => r.HolidayCtrlNbr == holidayCtrlNbr)
            .ToListAsync(ct);
}

internal sealed class HolidayPayrollRecordRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<HolidayPayrollRecord>(dbContext, currentUserService), IHolidayPayrollRecordRepository
{
}

internal sealed class RailroadHolidaySelectionRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<RailroadHolidaySelection>(dbContext, currentUserService), IRailroadHolidaySelectionRepository
{
    public async Task<IReadOnlyList<RailroadHolidaySelection>> GetActiveByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<RailroadHolidaySelection>()
            .Where(s => s.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && s.IsActive)
            .ToListAsync(ct);

    public async Task<bool> HasOwnSelectionsAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<RailroadHolidaySelection>()
            .AnyAsync(s => s.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr, ct);
}
