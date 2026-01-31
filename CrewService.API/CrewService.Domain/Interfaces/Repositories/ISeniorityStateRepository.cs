using CrewService.Domain.Models.Seniority;

namespace CrewService.Domain.Interfaces.Repositories;

public interface ISeniorityStateRepository
{
    Task<List<SeniorityState>> GetAllAsync();
    Task<List<SeniorityState>> GetAllAsync(int pageNumber, int pageSize);
    Task<SeniorityState?> GetByCtrlNbrAsync(long ctrlNbr);
    void Add(SeniorityState state);
    void Update(SeniorityState state);
    void Remove(SeniorityState state);
}