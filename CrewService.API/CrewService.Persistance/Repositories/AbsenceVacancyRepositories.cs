using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Seniority;
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
        await DbContext.Set<AbsenceRequest>()
            .Include(r => r.MarkUps)
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(r => r.ScheduledStartUtc)
            .ToListAsync();

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

        return await GetByDateRangeAsync(railroadCtrlNbr, day, nextDay, includeAllStatuses, ct: ct);
    }

    public async Task<List<AbsenceRequest>> GetByDateRangeAsync(
        ControlNumber railroadCtrlNbr,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        bool includeAllStatuses,
        ControlNumber? craftCtrlNbr = null,
        ControlNumber? departmentCtrlNbr = null,
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
            .Include(r => r.MarkUps)
            .Join(
                DbContext.Set<Employee>(),
                r => r.EmployeeCtrlNbr,
                e => e.CtrlNbr,
                (r, e) => new { Request = r, Employee = e })
            .Where(x => x.Employee.ClientCtrlNbr == railroadCtrlNbr
                || (parentCtrlNbr != null && x.Employee.ClientCtrlNbr == parentCtrlNbr))
            .Select(x => x.Request)
            .Where(r => r.ScheduledStartUtc >= rangeStart && r.ScheduledStartUtc < rangeEnd);

        if (!includeAllStatuses)
            query = query.Where(r => r.Status != "CANCELLED" && r.Status != "DENIED");

        query = ApplyCraftAndDepartmentFilters(query, railroadCtrlNbr, craftCtrlNbr, departmentCtrlNbr);

        return await query
            .Distinct()
            .OrderBy(r => r.ScheduledStartUtc)
            .ThenBy(r => r.CtrlNbr)
            .ToListAsync(ct);
    }

    public async Task<List<AbsenceRequest>> GetOpenAbsencesByRangeAsync(
        ControlNumber railroadCtrlNbr,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        ControlNumber? craftCtrlNbr = null,
        ControlNumber? departmentCtrlNbr = null,
        CancellationToken ct = default)
    {
        var rangeStart = DateTime.SpecifyKind(rangeStartUtc, DateTimeKind.Utc);
        var rangeEnd = DateTime.SpecifyKind(rangeEndUtc, DateTimeKind.Utc);
        if (rangeEnd <= rangeStart)
            return [];

        var nowUtc = DateTime.UtcNow;

        var railroadGroup = await DbContext.Set<DynamicGroup>()
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.CtrlNbr == railroadCtrlNbr, ct);
        var parentCtrlNbr = railroadGroup?.ParentCtrlNbr;

        var query = DbContext.Set<AbsenceRequest>()
            .Include(r => r.MarkUps)
            .Join(
                DbContext.Set<Employee>(),
                r => r.EmployeeCtrlNbr,
                e => e.CtrlNbr,
                (r, e) => new { Request = r, Employee = e })
            .Where(x => x.Employee.ClientCtrlNbr == railroadCtrlNbr
                || (parentCtrlNbr != null && x.Employee.ClientCtrlNbr == parentCtrlNbr))
            .Select(x => x.Request)
            .Where(r => r.Status == "EXERCISED" || r.Status == "COMPLETED")
            .Where(r =>
                !DbContext.Set<AbsenceMarkUp>().Any(m => m.AbsenceRequestCtrlNbr == r.CtrlNbr)
                || DbContext.Set<AbsenceMarkUp>().Any(m =>
                    m.AbsenceRequestCtrlNbr == r.CtrlNbr
                    && m.ActualMarkUpUtc == null)
                || DbContext.Set<AbsenceMarkUp>().Any(m =>
                    m.AbsenceRequestCtrlNbr == r.CtrlNbr
                    && m.ActualMarkUpUtc != null
                    && m.ActualMarkUpUtc > nowUtc));

        query = ApplyCraftAndDepartmentFilters(query, railroadCtrlNbr, craftCtrlNbr, departmentCtrlNbr);

        return await query
            .Distinct()
            .OrderBy(r => r.ScheduledStartUtc)
            .ThenBy(r => r.CtrlNbr)
            .ToListAsync(ct);
    }

    private IQueryable<AbsenceRequest> ApplyCraftAndDepartmentFilters(
        IQueryable<AbsenceRequest> query,
        ControlNumber railroadCtrlNbr,
        ControlNumber? craftCtrlNbr,
        ControlNumber? departmentCtrlNbr)
    {
        var filterByCraft = craftCtrlNbr is not null;
        var filterByDepartment = departmentCtrlNbr is not null;

        if (!filterByCraft && !filterByDepartment)
            return query;

        var activeEmployeeCrafts =
            from seniority in DbContext.Set<Seniority>()
            join roster in DbContext.Set<Roster>()
                on seniority.RosterCtrlNbr equals roster.CtrlNbr
            join craft in DbContext.Set<Craft>()
                on roster.CraftCtrlNbr equals craft.CtrlNbr
            where craft.DynamicGroupCtrlNbr == railroadCtrlNbr
                && seniority.SeniorityEndDate == null
                && seniority.LastActiveRoster
            select new
            {
                seniority.EmployeeCtrlNbr,
                CraftCtrlNbr = craft.CtrlNbr,
                craft.DepartmentCtrlNbr
            };

        return query.Where(request =>
            activeEmployeeCrafts.Any(x => x.EmployeeCtrlNbr == request.EmployeeCtrlNbr
                && (!filterByCraft || x.CraftCtrlNbr == craftCtrlNbr)
                && (!filterByDepartment || x.DepartmentCtrlNbr == departmentCtrlNbr)));
    }

    public async Task<List<AbsenceRequest>> GetActiveMarkupBoundAsync(ControlNumber employeeCtrlNbr) =>
        await DbContext.Set<AbsenceRequest>()
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr
                && r.Status == "APPROVED"
                && !DbContext.Set<AbsenceMarkUp>().Any(m => m.AbsenceRequestCtrlNbr == r.CtrlNbr && m.ActualMarkUpUtc.HasValue))
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
