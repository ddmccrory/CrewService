using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class QualificationTypeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<QualificationType>(dbContext, currentUserService), IQualificationTypeRepository
{
    public override async Task<QualificationType?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<QualificationType>()
            .Include(q => q.Requirements)
            .SingleOrDefaultAsync(q => q.CtrlNbr == ctrlNbr, ct);
    }

    public async Task<List<QualificationType>> GetByParentCtrlNbrAsync(ControlNumber parentCtrlNbr)
    {
        return await DbContext.Set<QualificationType>()
            .Where(q => q.ParentCtrlNbr == parentCtrlNbr)
            .OrderBy(q => q.Code)
            .ToListAsync();
    }

    public async Task<QualificationType?> GetByCodeAsync(ControlNumber parentCtrlNbr, string code)
    {
        var normalizedCode = code.ToUpperInvariant();

        return await DbContext.Set<QualificationType>()
            .Include(q => q.Requirements)
            .SingleOrDefaultAsync(q => q.ParentCtrlNbr == parentCtrlNbr && q.Code == normalizedCode);
    }

    public async Task<List<QualificationType>> GetActiveByParentCtrlNbrAsync(ControlNumber parentCtrlNbr)
    {
        return await DbContext.Set<QualificationType>()
            .Where(q => q.ParentCtrlNbr == parentCtrlNbr && q.IsActive)
            .OrderBy(q => q.Code)
            .ToListAsync();
    }

    public async Task<List<QualificationType>> GetActiveByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr)
    {
        return await DbContext.Set<QualificationType>()
            .Include(q => q.Requirements)
            .Where(q => q.CraftCtrlNbr == craftCtrlNbr && q.IsActive)
            .ToListAsync();
    }
}

internal sealed class QualificationRequirementRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<QualificationRequirement>(dbContext, currentUserService), IQualificationRequirementRepository
{
    public async Task<List<QualificationRequirement>> GetByQualificationTypeCtrlNbrAsync(ControlNumber qualificationTypeCtrlNbr)
    {
        return await DbContext.Set<QualificationRequirement>()
            .Where(p => p.QualificationTypeCtrlNbr == qualificationTypeCtrlNbr)
            .OrderBy(p => p.RequirementKind)
            .ThenBy(p => p.CtrlNbr)
            .ToListAsync();
    }
}

internal sealed class EmployeeQualificationRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<EmployeeQualification>(dbContext, currentUserService), IEmployeeQualificationRepository
{
    public override async Task<EmployeeQualification?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<EmployeeQualification>()
            .Include(e => e.Evidence)
            .SingleOrDefaultAsync(e => e.CtrlNbr == ctrlNbr, ct);
    }

    public async Task<List<EmployeeQualification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
    {
        return await DbContext.Set<EmployeeQualification>()
            .Where(eq => eq.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(eq => eq.AchievedAtUtc)
            .ToListAsync();
    }

    public async Task<EmployeeQualification?> GetByEmployeeAndTypeAsync(ControlNumber employeeCtrlNbr, ControlNumber qualificationTypeCtrlNbr)
    {
        return await DbContext.Set<EmployeeQualification>()
            .SingleOrDefaultAsync(eq => eq.EmployeeCtrlNbr == employeeCtrlNbr && eq.QualificationTypeCtrlNbr == qualificationTypeCtrlNbr);
    }

    public async Task<List<EmployeeQualification>> GetActiveByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
    {
        var now = DateTime.UtcNow;
        return await DbContext.Set<EmployeeQualification>()
            .Where(eq => eq.EmployeeCtrlNbr == employeeCtrlNbr
                && eq.RevokedAtUtc == null
                && eq.AchievedAtUtc != null && eq.AchievedAtUtc <= now
                && (eq.ExpiresAtUtc == null || eq.ExpiresAtUtc > now))
            .OrderBy(eq => eq.ExpiresAtUtc)
            .ToListAsync();
    }

    public async Task<List<EmployeeQualification>> GetActiveByEmployeeCtrlNbrsAsync(IEnumerable<ControlNumber> employeeCtrlNbrs)
    {
        var ctrlNbrList = employeeCtrlNbrs.ToList();
        if (ctrlNbrList.Count == 0) return [];
        var now = DateTime.UtcNow;
        return await DbContext.Set<EmployeeQualification>()
            .Where(eq => ctrlNbrList.Contains(eq.EmployeeCtrlNbr)
                && eq.RevokedAtUtc == null
                && eq.AchievedAtUtc != null && eq.AchievedAtUtc <= now
                && (eq.ExpiresAtUtc == null || eq.ExpiresAtUtc > now))
            .ToListAsync();
    }

    public async Task<List<EmployeeQualification>> GetExpiringBeforeAsync(DateTime cutoffUtc)
    {
        var now = DateTime.UtcNow;
        return await DbContext.Set<EmployeeQualification>()
            .Where(eq => eq.ExpiresAtUtc.HasValue
                && eq.ExpiresAtUtc.Value <= cutoffUtc
                && eq.RevokedAtUtc == null
                && eq.AchievedAtUtc != null && eq.AchievedAtUtc <= now
                && eq.ExpiresAtUtc.Value > now)
            .OrderBy(eq => eq.ExpiresAtUtc)
            .ToListAsync();
    }
}
