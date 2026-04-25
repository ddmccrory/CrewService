using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.FraCompliance;

internal sealed class FraCertificationConfigRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<FraCertificationConfig>(dbContext, currentUserService), IFraCertificationConfigRepository
{
    public override async Task<List<FraCertificationConfig>> GetAllAsync(CancellationToken ct = default)
        => await DbContext.Set<FraCertificationConfig>().ToListAsync(ct);

    public async Task<FraCertificationConfig?> GetByParentAsync(ControlNumber parentCtrlNbr, CancellationToken ct = default)
        => await DbContext.Set<FraCertificationConfig>()
            .SingleOrDefaultAsync(c => c.ParentCtrlNbr == parentCtrlNbr && c.RailroadCtrlNbr == null, ct);

    public async Task<FraCertificationConfig?> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
        => await DbContext.Set<FraCertificationConfig>()
            .SingleOrDefaultAsync(c => c.RailroadCtrlNbr == railroadCtrlNbr, ct);

    public override async Task AddAsync(FraCertificationConfig config, CancellationToken ct = default)
        => await base.AddAsync(config, ct);

    public override async Task UpdateAsync(FraCertificationConfig config, CancellationToken ct = default)
        => await base.UpdateAsync(config, ct);
}

internal sealed class FraCertificationCheckConfigRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<FraCertificationCheckConfig>(dbContext, currentUserService), IFraCertificationCheckConfigRepository
{
    public override async Task<List<FraCertificationCheckConfig>> GetAllAsync(CancellationToken ct = default)
        => await DbContext.Set<FraCertificationCheckConfig>().ToListAsync(ct);

    public async Task<IReadOnlyList<FraCertificationCheckConfig>> GetByParentAsync(ControlNumber parentCtrlNbr, CancellationToken ct = default)
        => await DbContext.Set<FraCertificationCheckConfig>()
            .Where(c => c.ParentCtrlNbr == parentCtrlNbr && c.RailroadCtrlNbr == null)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FraCertificationCheckConfig>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
        => await DbContext.Set<FraCertificationCheckConfig>()
            .Where(c => c.RailroadCtrlNbr == railroadCtrlNbr)
            .ToListAsync(ct);

    public override async Task AddAsync(FraCertificationCheckConfig config, CancellationToken ct = default)
        => await base.AddAsync(config, ct);

    public override async Task UpdateAsync(FraCertificationCheckConfig config, CancellationToken ct = default)
        => await base.UpdateAsync(config, ct);

    public override async Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
        => await base.DeleteAsync(ctrlNbr, ct);
}

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

    public async Task<IReadOnlyList<CertificationWithEmployeeDto>> GetByClientAndStatusesAsync(ControlNumber clientCtrlNbr, IReadOnlyCollection<string> statuses, CancellationToken ct = default)
    {
        var normalizedStatuses = statuses
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Use an IQueryable subquery so EF translates to SQL IN (SELECT CtrlNbr FROM Employees WHERE ...)
        // rather than materializing ControlNumber value objects into a List<ControlNumber> for Contains.
        // AsSplitQuery is intentionally omitted: split queries cannot correctly populate private backing
        // fields (_eligibilityChecks) when the root set is filtered via a subquery.
        var clientEmployeeCtrlNbrs = dbContext.Set<Employee>()
            .Where(e => e.ClientCtrlNbr == clientCtrlNbr)
            .Select(e => e.CtrlNbr);

        var certificationQuery = dbContext.Set<EmployeeCertification>()
            .Where(c => clientEmployeeCtrlNbrs.Contains(c.EmployeeCtrlNbr));

        if (normalizedStatuses.Count > 0)
            certificationQuery = certificationQuery.Where(c => normalizedStatuses.Contains(c.Status));

        var certs = await certificationQuery
            .OrderByDescending(c => c.ExpirationDate)
            .ToListAsync(ct);

        var employeeMap = await dbContext.Set<Employee>()
            .Where(e => e.ClientCtrlNbr == clientCtrlNbr)
            .Select(e => new { e.CtrlNbr, e.EmployeeNumber, e.UserId })
            .ToDictionaryAsync(e => e.CtrlNbr, ct);

        return [.. certs.Select(c =>
        {
            employeeMap.TryGetValue(c.EmployeeCtrlNbr, out var emp);
            return new CertificationWithEmployeeDto(c, emp?.EmployeeNumber ?? string.Empty, emp?.UserId ?? string.Empty);
        })];
    }
}

internal sealed class EmployeeCertificationRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<EmployeeCertification>(dbContext, currentUserService), IEmployeeCertificationRepository
{
    public override async Task<List<EmployeeCertification>> GetAllAsync(CancellationToken ct = default)
        => await base.GetAllAsync(ct);

    public async Task<List<EmployeeCertification>> GetAllWithChecksAsync(CancellationToken ct = default)
    {
        return await DbContext.Set<EmployeeCertification>()
            .Include(c => c.EligibilityChecks)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EmployeeCertification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<EmployeeCertification>()
            .Where(c => c.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(c => c.ExpirationDate)
            .ToListAsync(ct);
    }

    public async Task<EmployeeCertification?> GetByEmployeeAndRegulatoryQualAsync(ControlNumber employeeCtrlNbr, ControlNumber regulatoryQualCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<EmployeeCertification>()
            .Where(c => c.EmployeeCtrlNbr == employeeCtrlNbr && c.RegulatoryQualificationCtrlNbr == regulatoryQualCtrlNbr)
            .OrderByDescending(c => c.ExpirationDate)
            .FirstOrDefaultAsync(ct);
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
