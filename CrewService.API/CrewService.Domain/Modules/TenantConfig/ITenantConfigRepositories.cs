using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.TenantConfig;

public interface IGroupTypeRepository : IRepository<GroupType>
{
    Task<GroupType?> GetByNameAsync(string name, long parentCtrlNbr = 0);
    Task<GroupType?> GetByNameIncludingDeletedAsync(string name, long parentCtrlNbr = 0);
}

public interface IDynamicGroupRepository : IRepository<DynamicGroup>
{
    Task<List<DynamicGroup>> GetByParentCtrlNbrAsync(ControlNumber? parentGroupCtrlNbr);
    Task<DynamicGroup?> GetByGroupTypeAndNameIncludingDeletedAsync(ControlNumber groupTypeCtrlNbr, string name);
    Task<List<DynamicGroup>> GetWorkAreasAsync();
    Task<List<DynamicGroup>> GetAncestorsAsync(ControlNumber groupCtrlNbr);
    Task<List<DynamicGroup>> GetTreeAsync(ControlNumber? rootCtrlNbr = null);
    Task<List<DynamicGroup>> GetByGroupTypeNameAsync(string typeName, long parentCtrlNbr = 0);
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
