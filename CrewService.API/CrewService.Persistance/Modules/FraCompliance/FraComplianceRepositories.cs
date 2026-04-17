using CrewService.Application.FraCompliance;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
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

internal sealed class RegulatoryStandardRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<RegulatoryStandard>(dbContext, currentUserService), IRegulatoryStandardRepository
{
}

internal sealed class RegulatoryQualificationRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<RegulatoryQualification>(dbContext, currentUserService), IRegulatoryQualificationRepository
{
    public async Task<RegulatoryQualification?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await DbContext.Set<RegulatoryQualification>()
            .SingleOrDefaultAsync(r => r.Code == code, ct);
    }
}

internal sealed class EmployeeCertificationReadRepository(CrewServiceDbContext dbContext)
    : IEmployeeCertificationReadRepository
{
    public async Task<IReadOnlyList<EmployeeCertification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        return await dbContext.Set<EmployeeCertification>()
            .Where(c => c.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(c => c.ExpirationDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EmployeeCertification>> GetByClientAndStatusesAsync(ControlNumber clientCtrlNbr, IReadOnlyCollection<string> statuses, CancellationToken ct = default)
    {
        var normalizedStatuses = statuses
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var query = from certification in dbContext.Set<EmployeeCertification>()
                    join employee in dbContext.Set<Employee>() on certification.EmployeeCtrlNbr equals employee.CtrlNbr
                    where employee.ClientCtrlNbr == clientCtrlNbr
                    select certification;

        if (normalizedStatuses.Count > 0)
        {
            query = query.Where(c => normalizedStatuses.Contains(c.Status));
        }

        return await query
            .OrderByDescending(c => c.ExpirationDate)
            .ToListAsync(ct);
    }
}

internal sealed class EmployeeCertificationRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<EmployeeCertification>(dbContext, currentUserService), IEmployeeCertificationRepository
{
    public override async Task<List<EmployeeCertification>> GetAllAsync(CancellationToken ct = default)
        => await base.GetAllAsync(ct);

    public async Task<IReadOnlyList<EmployeeCertification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<EmployeeCertification>()
            .Where(c => c.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(c => c.ExpirationDate)
            .ToListAsync(ct);
    }

    public async Task<EmployeeCertification?> GetByCtrlNbrWithChecksAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<EmployeeCertification>()
            .Include(c => c.EligibilityChecks)
            .SingleOrDefaultAsync(c => c.CtrlNbr == ctrlNbr, ct);
    }

    public async Task<EmployeeCertification?> GetByEligibilityCheckCtrlNbrWithChecksAsync(ControlNumber eligibilityCheckCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<EmployeeCertification>()
            .Include(c => c.EligibilityChecks)
            .SingleOrDefaultAsync(c => c.EligibilityChecks.Any(ch => ch.CtrlNbr == eligibilityCheckCtrlNbr), ct);
    }
}

internal sealed class CertificationRevocationRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<CertificationRevocationRecord>(dbContext, currentUserService), ICertificationRevocationRepository
{
    public override async Task<CertificationRevocationRecord?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
        => await base.GetByCtrlNbrAsync(ctrlNbr, ct);

    public async Task<IReadOnlyList<CertificationRevocationRecord>> GetByCertificationCtrlNbrAsync(ControlNumber employeeCertificationCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<CertificationRevocationRecord>()
            .Where(r => r.EmployeeCertificationCtrlNbr == employeeCertificationCtrlNbr)
            .OrderByDescending(r => r.ViolationDate)
            .ToListAsync(ct);
    }
}

internal sealed class DrugAlcoholTestRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<DrugAlcoholTestRecord>(dbContext, currentUserService), IDrugAlcoholTestRepository
{
    public async Task<IReadOnlyList<DrugAlcoholTestRecord>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<DrugAlcoholTestRecord>()
            .Where(t => t.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(t => t.TestDate)
            .ToListAsync(ct);
    }
}

internal sealed class VoluntaryReferralRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<VoluntaryReferral>(dbContext, currentUserService), IVoluntaryReferralRepository
{
    public async Task<IReadOnlyList<VoluntaryReferral>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<VoluntaryReferral>()
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(r => r.ReferralDate)
            .ToListAsync(ct);
    }

    public override async Task<VoluntaryReferral?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
        => await base.GetByCtrlNbrAsync(ctrlNbr, ct);
}

internal sealed class DrugAlcoholActionRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<DrugAlcoholAction>(dbContext, currentUserService), IDrugAlcoholActionRepository
{
    public async Task<IReadOnlyList<DrugAlcoholAction>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<DrugAlcoholAction>()
            .Where(a => a.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(a => a.ActionDate)
            .ToListAsync(ct);
    }
}
