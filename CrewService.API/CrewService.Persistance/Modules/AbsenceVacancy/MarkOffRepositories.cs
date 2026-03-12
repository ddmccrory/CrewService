using CrewService.Application.MarkOff;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.AbsenceVacancy;

internal sealed class AbsenceCodeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<AbsenceCode>(dbContext, currentUserService), IAbsenceCodeRepository
{
    public async Task<AbsenceCodeCraftOverride?> GetOverrideAsync(
        ControlNumber absenceCodeCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<AbsenceCodeCraftOverride>()
            .SingleOrDefaultAsync(o => o.AbsenceCodeCtrlNbr == absenceCodeCtrlNbr && o.CraftCtrlNbr == craftCtrlNbr, ct);
}

internal sealed class CompensationBalanceRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<CompensationBalance>(dbContext, currentUserService), ICompensationBalanceRepository
{
    public async Task<CompensationBalance?> GetAsync(
        ControlNumber employeeCtrlNbr, string compensationType, CancellationToken ct = default) =>
        await DbContext.Set<CompensationBalance>()
            .SingleOrDefaultAsync(b => b.EmployeeCtrlNbr == employeeCtrlNbr && b.CompensationType == compensationType, ct);
}
