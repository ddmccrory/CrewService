using CrewService.Application.Qualifications;
using CrewService.Application.Qualifications.Evaluators;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Qualifications;

internal sealed class OnDutyRecordCounter(CrewServiceDbContext dbContext) : IOnDutyRecordCounter
{
    public async Task<int> CountCompletedAsync(ControlNumber employeeCtrlNbr, string? activityFilter = null, CancellationToken ct = default)
    {
        return await dbContext.Set<OnDutyRecord>()
            .CountAsync(r => r.EmployeeCtrlNbr == employeeCtrlNbr && r.Status == "TiedUp", ct);
    }
}

internal sealed class CraftMembershipDateProvider(CrewServiceDbContext dbContext) : ICraftMembershipDateProvider
{
    public async Task<DateTime?> GetEarliestActiveMembershipDateAsync(ControlNumber employeeCtrlNbr, ControlNumber? craftCtrlNbr = null, CancellationToken ct = default)
    {
        var query = dbContext.Set<PositionAssignment>()
            .Where(a => a.EmployeeCtrlNbr == employeeCtrlNbr);

        var earliestAssignedDate = await query
            .OrderBy(a => a.AssignedDateUtc)
            .Select(a => (DateTime?)a.AssignedDateUtc)
            .FirstOrDefaultAsync(ct);

        return earliestAssignedDate;
    }
}

internal sealed class FraCertificationChecker(CrewServiceDbContext dbContext) : IFraCertificationChecker
{
    public async Task<bool> HasActiveCertificationAsync(ControlNumber employeeCtrlNbr, ControlNumber regulatoryQualificationCtrlNbr, CancellationToken ct = default)
    {
        return await dbContext.Set<EmployeeCertification>()
            .AnyAsync(c => c.EmployeeCtrlNbr == employeeCtrlNbr
                        && c.RegulatoryQualificationCtrlNbr == regulatoryQualificationCtrlNbr
                        && c.Status == "Active", ct);
    }
}

internal sealed class RegulatoryQualificationCatalog(CrewServiceDbContext dbContext) : IRegulatoryQualificationCatalog
{
    public async Task<IReadOnlyList<RegulatoryQualificationCatalogEntry>> GetAllAsync(CancellationToken ct = default)
    {
        return await dbContext.Set<RegulatoryQualification>()
            .OrderBy(r => r.Code)
            .Select(r => new RegulatoryQualificationCatalogEntry(
                r.CtrlNbr,
                r.Code,
                r.CfrPart,
                r.Description))
            .ToListAsync(ct);
    }
}
