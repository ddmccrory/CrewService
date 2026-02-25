using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.TenantConfig;

public interface IGroupTypeRepository : IRepository<GroupType>
{
    Task<GroupType?> GetByNameAsync(string name);
}

public interface IDynamicGroupRepository : IRepository<DynamicGroup>
{
    Task<List<DynamicGroup>> GetByParentCtrlNbrAsync(ControlNumber? parentGroupCtrlNbr);
    Task<List<DynamicGroup>> GetWorkAreasAsync();
    Task<List<DynamicGroup>> GetAncestorsAsync(ControlNumber groupCtrlNbr);
    Task<List<DynamicGroup>> GetTreeAsync(ControlNumber? rootCtrlNbr = null);
}

public interface IGroupAttributeDefinitionRepository : IRepository<GroupAttributeDefinition>
{
    Task<List<GroupAttributeDefinition>> GetByGroupTypeCtrlNbrAsync(ControlNumber groupTypeCtrlNbr);
}

public interface IGroupAttributeValueRepository : IRepository<GroupAttributeValue>
{
    Task<List<GroupAttributeValue>> GetByGroupCtrlNbrAsync(ControlNumber groupCtrlNbr);
}

public interface IRailroadGroupPlacementRepository : IRepository<RailroadGroupPlacement>
{
    Task<List<RailroadGroupPlacement>> GetByRailroadCtrlNbrAsync(ControlNumber railroadCtrlNbr);
    Task<List<RailroadGroupPlacement>> GetByGroupCtrlNbrAsync(ControlNumber groupCtrlNbr);
    Task<List<RailroadGroupPlacement>> GetByGroupSubtreeAsync(string pathPrefix);
    Task<RailroadGroupPlacement?> GetByRailroadAndGroupAsync(ControlNumber railroadCtrlNbr, ControlNumber groupCtrlNbr);
}
