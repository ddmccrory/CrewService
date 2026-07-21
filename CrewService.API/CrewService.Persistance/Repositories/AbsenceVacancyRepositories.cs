using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class AbsenceRequestRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<AbsenceRequest>(dbContext, currentUserService), IAbsenceRequestRepository
{
    public async Task<List<AbsenceRequest>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr) =>
        await DbContext.Set<AbsenceRequest>().Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr).OrderByDescending(r => r.StartUtc).ToListAsync();

    public async Task<List<AbsenceRequest>> GetPendingAsync() =>
        await DbContext.Set<AbsenceRequest>().Where(r => r.Status == "PENDING").ToListAsync();

    public async Task<List<AbsenceRequest>> GetByDateAsync(
        ControlNumber railroadCtrlNbr,
        DateTime requestDateUtc,
        bool includeAllStatuses,
        CancellationToken ct = default)
    {
        var day = requestDateUtc.Date;
        var nextDay = day.AddDays(1);

        return await GetByDateRangeAsync(railroadCtrlNbr, day, nextDay, includeAllStatuses, ct);
    }

    public async Task<List<AbsenceRequest>> GetByDateRangeAsync(
        ControlNumber railroadCtrlNbr,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        bool includeAllStatuses,
        CancellationToken ct = default)
    {
        var rangeStart = DateTime.SpecifyKind(rangeStartUtc, DateTimeKind.Utc);
        var rangeEnd = DateTime.SpecifyKind(rangeEndUtc, DateTimeKind.Utc);
        if (rangeEnd <= rangeStart)
            return [];

        var railroadGroup = await DbContext.Set<DynamicGroup>()
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.CtrlNbr == railroadCtrlNbr, ct);
        var parentCtrlNbr = railroadGroup?.ParentCtrlNbr;

        var query = DbContext.Set<AbsenceRequest>()
            .Join(
                DbContext.Set<Employee>(),
                r => r.EmployeeCtrlNbr,
                e => e.CtrlNbr,
                (r, e) => new { Request = r, Employee = e })
            .Where(x => x.Employee.ClientCtrlNbr == railroadCtrlNbr
                || (parentCtrlNbr != null && x.Employee.ClientCtrlNbr == parentCtrlNbr))
            .Select(x => x.Request)
            .Where(r => r.StartUtc >= rangeStart && r.StartUtc < rangeEnd);

        if (!includeAllStatuses)
            query = query.Where(r => r.Status != "CANCELLED" && r.Status != "DENIED");

        return await query
            .OrderBy(r => r.StartUtc)
            .ThenBy(r => r.CtrlNbr)
            .ToListAsync(ct);
    }

    public async Task<List<AbsenceRequest>> GetOpenAbsencesByRangeAsync(
        ControlNumber railroadCtrlNbr,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        CancellationToken ct = default)
    {
        var rangeStart = DateTime.SpecifyKind(rangeStartUtc, DateTimeKind.Utc);
        var rangeEnd = DateTime.SpecifyKind(rangeEndUtc, DateTimeKind.Utc);
        if (rangeEnd <= rangeStart)
            return [];

        var nowUtc = DateTime.UtcNow;
        var effectiveRangeEnd = rangeEnd < nowUtc ? rangeEnd : nowUtc;
        if (effectiveRangeEnd <= rangeStart)
            return [];

        var railroadGroup = await DbContext.Set<DynamicGroup>()
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.CtrlNbr == railroadCtrlNbr, ct);
        var parentCtrlNbr = railroadGroup?.ParentCtrlNbr;

        return await DbContext.Set<AbsenceRequest>()
            .Join(
                DbContext.Set<Employee>(),
                r => r.EmployeeCtrlNbr,
                e => e.CtrlNbr,
                (r, e) => new { Request = r, Employee = e })
            .Where(x => x.Employee.ClientCtrlNbr == railroadCtrlNbr
                || (parentCtrlNbr != null && x.Employee.ClientCtrlNbr == parentCtrlNbr))
            .Select(x => x.Request)
            .Where(r => r.StartUtc <= nowUtc)
            .Where(r => !r.EndUtc.HasValue || r.EndUtc.Value >= nowUtc)
            .Where(r => r.StartUtc < effectiveRangeEnd)
            .Where(r => !r.EndUtc.HasValue || r.EndUtc.Value >= rangeStart)
            .OrderBy(r => r.StartUtc)
            .ThenBy(r => r.CtrlNbr)
            .ToListAsync(ct);
    }

    public async Task<List<AbsenceRequest>> GetActiveMarkupBoundAsync(ControlNumber employeeCtrlNbr) =>
        await DbContext.Set<AbsenceRequest>()
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr && r.Status == "APPROVED" && r.EndUtc == null)
            .ToListAsync();
}

internal sealed class VacancyImpactRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<VacancyImpact>(dbContext, currentUserService), IVacancyImpactRepository
{
    public async Task<List<VacancyImpact>> GetByAbsenceRequestAsync(ControlNumber absenceRequestCtrlNbr) =>
        await DbContext.Set<VacancyImpact>().Where(v => v.AbsenceRequestCtrlNbr == absenceRequestCtrlNbr).ToListAsync();

    public async Task<List<VacancyImpact>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr) =>
        await DbContext.Set<VacancyImpact>().Where(v => v.PositionSlotCtrlNbr == positionSlotCtrlNbr).ToListAsync();
}
