using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public interface IAssignmentTemplateRepository : IRepository<AssignmentTemplate>
{
    Task<List<AssignmentTemplate>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr);
}

public interface IWorkInstanceRepository : IRepository<WorkInstance>
{
    Task<List<WorkInstance>> GetByWorkAreaAndDateRangeAsync(ControlNumber workAreaGroupCtrlNbr, DateTime startUtc, DateTime endUtc);
}

public interface IPositionRoleRepository : IRepository<PositionRole>
{
    Task<List<PositionRole>> GetByCraftAsync(ControlNumber craftCtrlNbr);
}

public interface IPositionSlotRepository : IRepository<PositionSlot>
{
    Task<List<PositionSlot>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr);
    Task<List<PositionSlot>> GetOpenByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr);
}

public interface ISlotRequirementRepository : IRepository<SlotRequirement>
{
    Task<List<SlotRequirement>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr);
}
