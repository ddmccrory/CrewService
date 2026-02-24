using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Dispatching;

public interface IDispatchProjectionRepository : IRepository<DispatchProjection>
{
    Task<List<DispatchProjection>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr);
}

public interface IDispatchDecisionLogRepository : IRepository<DispatchDecisionLog>
{
    Task<List<DispatchDecisionLog>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr);
}

public interface IDispatchOverrideRepository : IRepository<DispatchOverride>
{
    Task<List<DispatchOverride>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr);
    Task<List<DispatchOverride>> GetPendingAsync();
}

public interface IEmployeeBookingRepository : IRepository<EmployeeBooking>
{
    Task<List<EmployeeBooking>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc);
    Task<bool> HasOverlapAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc);
}
