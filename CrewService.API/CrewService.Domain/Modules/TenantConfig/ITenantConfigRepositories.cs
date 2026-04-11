using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.TenantConfig;

public interface IGroupTypeRepository : IRepository<GroupType>
{
    Task<GroupType?> GetByNameAsync(string name, ControlNumber? parentCtrlNbr = null);
    Task<GroupType?> GetByNameIncludingDeletedAsync(string name, ControlNumber? parentCtrlNbr = null);
}

public interface IDynamicGroupRepository : IRepository<DynamicGroup>
{
    Task<List<DynamicGroup>> GetByParentCtrlNbrAsync(ControlNumber? parentGroupCtrlNbr);
    Task<List<DynamicGroup>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs);
    Task<DynamicGroup?> GetByGroupTypeAndNameIncludingDeletedAsync(ControlNumber groupTypeCtrlNbr, string name);
    Task<List<DynamicGroup>> GetWorkAreasAsync(ControlNumber? railroadCtrlNbr = null);
    Task<List<DynamicGroup>> GetWorkAreasWithDescendantsAsync();
    Task<List<DynamicGroup>> GetAncestorsAsync(ControlNumber groupCtrlNbr);
    Task<List<DynamicGroup>> GetTreeAsync(ControlNumber? rootCtrlNbr = null);
    Task<List<DynamicGroup>> GetByGroupTypeNameAsync(string typeName, ControlNumber? parentCtrlNbr = null);
    Task BackfillPathsAsync();
}

public interface IGroupAttributeDefinitionRepository : IRepository<GroupAttributeDefinition>
{
    Task<List<GroupAttributeDefinition>> GetByGroupTypeCtrlNbrAsync(ControlNumber groupTypeCtrlNbr);
    Task<GroupAttributeDefinition?> GetByGroupTypeAndAttributeNameIncludingDeletedAsync(ControlNumber groupTypeCtrlNbr, string attributeName);
}

public interface IGroupAttributeValueRepository : IRepository<GroupAttributeValue>
{
    Task<List<GroupAttributeValue>> GetByGroupCtrlNbrAsync(ControlNumber groupCtrlNbr);
}
