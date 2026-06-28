using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.AbsenceVacancy;
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
