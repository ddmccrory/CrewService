using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Staffing;

internal sealed class StaffablePositionRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<StaffablePosition>(dbContext, currentUserService), IStaffablePositionRepository
{
    public async Task<List<StaffablePosition>> GetByPositionTypeAsync(string positionType) =>
        await DbContext.Set<StaffablePosition>().Where(s => s.PositionType == positionType).ToListAsync();
}

internal sealed class PositionAssignmentRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<PositionAssignment>(dbContext, currentUserService), IPositionAssignmentRepository
{
    public async Task<PositionAssignment?> GetByStaffablePositionAsync(ControlNumber staffablePositionCtrlNbr) =>
        await DbContext.Set<PositionAssignment>().FirstOrDefaultAsync(a => a.StaffablePositionCtrlNbr == staffablePositionCtrlNbr);

    public async Task<List<PositionAssignment>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr) =>
        await DbContext.Set<PositionAssignment>().Where(a => a.EmployeeCtrlNbr == employeeCtrlNbr).ToListAsync();
}