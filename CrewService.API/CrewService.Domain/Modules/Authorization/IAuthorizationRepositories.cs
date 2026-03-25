using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Authorization;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Role?> GetByNameIncludingDeletedAsync(string name, CancellationToken ct = default);
}

public interface IFeatureRepository : IRepository<Feature>
{
    Task<Feature?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<List<Feature>> GetByCategoryAsync(string category, CancellationToken ct = default);
}

public interface IPermissionRepository : IRepository<Permission>
{
    Task<Permission?> GetByRoleFeatureParentAsync(ControlNumber roleCtrlNbr, ControlNumber featureCtrlNbr, long? parentCtrlNbr, CancellationToken ct = default);
    Task<List<Permission>> GetByRoleCtrlNbrAsync(ControlNumber roleCtrlNbr, CancellationToken ct = default);
    Task<List<Permission>> GetEffectivePermissionsAsync(ControlNumber roleCtrlNbr, long? parentCtrlNbr, CancellationToken ct = default);
}
