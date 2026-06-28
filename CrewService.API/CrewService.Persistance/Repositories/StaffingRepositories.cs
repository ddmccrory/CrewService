using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

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

    public async Task<List<PositionAssignment>> GetByStaffablePositionsAsync(IEnumerable<ControlNumber> staffablePositionCtrlNbrs)
    {
        var ctrlNbrs = staffablePositionCtrlNbrs.ToList();
        if (ctrlNbrs.Count == 0) return [];
        return await DbContext.Set<PositionAssignment>()
            .Where(a => ctrlNbrs.Contains(a.StaffablePositionCtrlNbr))
            .ToListAsync();
    }

    public async Task<List<PositionAssignment>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr) =>
        await DbContext.Set<PositionAssignment>().Where(a => a.EmployeeCtrlNbr == employeeCtrlNbr).ToListAsync();

    public async Task<HashSet<long>> GetAssignedEmployeeCtrlNbrsAsync()
    {
        var values = await DbContext.Set<PositionAssignment>()
            .Select(a => a.EmployeeCtrlNbr.Value)
            .Distinct()
            .ToListAsync();
        return [.. values];
    }

    public async Task<HashSet<long>> GetAssignedEmployeeCtrlNbrsByTypeAsync(string assignmentType)
    {
        var values = await DbContext.Set<PositionAssignment>()
            .Where(a => a.AssignmentType == assignmentType)
            .Select(a => a.EmployeeCtrlNbr.Value)
            .Distinct()
            .ToListAsync();
        return [.. values];
    }
}