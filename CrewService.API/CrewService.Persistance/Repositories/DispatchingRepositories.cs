using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class DispatchProjectionRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<DispatchProjection>(dbContext, currentUserService), IDispatchProjectionRepository
{
    public async Task<List<DispatchProjection>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr) =>
        await DbContext.Set<DispatchProjection>().Where(p => p.PositionSlotCtrlNbr == positionSlotCtrlNbr).OrderByDescending(p => p.ComputedUtc).ToListAsync();
}

internal sealed class DispatchDecisionLogRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<DispatchDecisionLog>(dbContext, currentUserService), IDispatchDecisionLogRepository
{
    public async Task<List<DispatchDecisionLog>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr) =>
        await DbContext.Set<DispatchDecisionLog>().Where(l => l.PositionSlotCtrlNbr == positionSlotCtrlNbr).OrderByDescending(l => l.AsOfUtc).ToListAsync();
}

internal sealed class DispatchOverrideRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<DispatchOverride>(dbContext, currentUserService), IDispatchOverrideRepository
{
    public async Task<List<DispatchOverride>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr) =>
        await DbContext.Set<DispatchOverride>().Where(o => o.PositionSlotCtrlNbr == positionSlotCtrlNbr).ToListAsync();

    public async Task<List<DispatchOverride>> GetPendingAsync() =>
        await DbContext.Set<DispatchOverride>().Where(o => o.Status == "PENDING").ToListAsync();
}

internal sealed class EmployeeBookingRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<EmployeeBooking>(dbContext, currentUserService), IEmployeeBookingRepository
{
    public async Task<List<EmployeeBooking>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc) =>
        await DbContext.Set<EmployeeBooking>()
            .Where(b => b.EmployeeCtrlNbr == employeeCtrlNbr && b.StartUtc < endUtc && b.EndUtc > startUtc)
            .ToListAsync();

    public async Task<bool> HasOverlapAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc) =>
        await DbContext.Set<EmployeeBooking>()
            .AnyAsync(b => b.EmployeeCtrlNbr == employeeCtrlNbr && b.StartUtc < endUtc && b.EndUtc > startUtc);
}
