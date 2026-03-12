using CrewService.Application.FraCompliance;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.FraCompliance;

internal sealed class FraDutyTourRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<FraDutyTour>(dbContext, currentUserService), IFraDutyTourRepository
{
    public async Task<IReadOnlyList<FraDutyTour>> SearchAsync(FraRecordSearchCriteria criteria, CancellationToken ct = default)
    {
        var query = DbContext.Set<FraDutyTour>().AsQueryable();

        if (criteria.EmployeeCtrlNbr is not null)
            query = query.Where(t => t.EmployeeCtrlNbr == criteria.EmployeeCtrlNbr);
        if (criteria.StartDateUtc.HasValue)
            query = query.Where(t => t.DutyTourStartUtc >= criteria.StartDateUtc.Value);
        if (criteria.EndDateUtc.HasValue)
            query = query.Where(t => t.DutyTourStartUtc <= criteria.EndDateUtc.Value);
        if (criteria.HasExcessService == true)
            query = query.Where(t => t.ExcessMinutes != null && t.ExcessMinutes > 0);
        if (criteria.IsCertified.HasValue)
            query = query.Where(t => t.IsCertified == criteria.IsCertified.Value);

        return await query.OrderByDescending(t => t.DutyTourStartUtc).ToListAsync(ct);
    }

    public override async Task<FraDutyTour?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<FraDutyTour>()
            .Include(t => t.Segments)
            .Include(t => t.TransportationSegments)
            .Include(t => t.OtherServiceSegments)
            .SingleOrDefaultAsync(t => t.CtrlNbr == ctrlNbr, ct);

    public async Task<FraDutyTour?> GetActiveTourForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<FraDutyTour>()
            .Include(t => t.Segments)
            .Where(t => t.EmployeeCtrlNbr == employeeCtrlNbr && t.DutyTourEndUtc == null)
            .OrderByDescending(t => t.DutyTourStartUtc)
            .FirstOrDefaultAsync(ct);
}
