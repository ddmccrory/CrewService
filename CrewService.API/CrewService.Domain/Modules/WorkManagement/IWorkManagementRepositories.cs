using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public interface IWorkInstanceRepository : IRepository<WorkInstance>
{
    Task<List<WorkInstance>> GetByWorkAreaAndDateRangeAsync(ControlNumber workAreaGroupCtrlNbr, DateTime startUtc, DateTime endUtc);
}

public interface ICraftRoleRepository : IRepository<CraftRole>
{
    Task<List<CraftRole>> GetByCraftAsync(ControlNumber craftCtrlNbr);
    Task<List<CraftRole>> GetByDepartmentAsync(ControlNumber departmentCtrlNbr);
    Task<List<CraftRole>> GetByRailroadAsync(ControlNumber railroadCtrlNbr);
    Task<CraftRole?> GetByCtrlNbrWithQualificationsAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
}

public interface ICraftRoleQualificationRepository : IRepository<CraftRoleQualification>
{
    Task<List<CraftRoleQualification>> GetByCraftRoleAsync(ControlNumber craftRoleCtrlNbr);
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

public interface IDepartmentRepository : IRepository<Department>
{
    Task<List<Department>> GetByParentAndRailroadAsync(ControlNumber? parentCtrlNbr, ControlNumber? railroadCtrlNbr);
}

public interface IShiftDefinitionRepository : IRepository<ShiftDefinition>
{
    Task<List<ShiftDefinition>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr);
}

public interface IShiftInstanceRepository : IRepository<ShiftInstance>
{
    Task<IReadOnlyList<ShiftInstance>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr, CancellationToken ct = default);
    Task<bool> ExistsByWorkInstanceAndShiftCodeAsync(ControlNumber workInstanceCtrlNbr, string shiftCode, ControlNumber? departmentCtrlNbr, CancellationToken ct = default);
}
