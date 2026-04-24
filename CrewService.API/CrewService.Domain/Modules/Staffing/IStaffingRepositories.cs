using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Staffing;

public interface IStaffablePositionRepository : IRepository<StaffablePosition>
{
    Task<List<StaffablePosition>> GetByPositionTypeAsync(string positionType);
}

public interface IPositionAssignmentRepository : IRepository<PositionAssignment>
{
    Task<PositionAssignment?> GetByStaffablePositionAsync(ControlNumber staffablePositionCtrlNbr);
    Task<List<PositionAssignment>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr);
    Task<HashSet<long>> GetAssignedEmployeeCtrlNbrsAsync();
}