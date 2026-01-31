using CrewService.Domain.Models.Railroads;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRailroadRepository
{
    Task<List<Railroad>> GetAllAsync();

    Task<Railroad?> GetByCtrlNbrAsync(long railroadCtrlNbr);

    Task<List<Railroad>> GetAllRailroadsByParentCtrlNbrAsync(long parentCtrlNbr);

    void Add(Railroad railroad);

    void Update(Railroad railroad);

    void Remove(Railroad railroad);
}
