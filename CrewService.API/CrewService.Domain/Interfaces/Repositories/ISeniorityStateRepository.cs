using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface ISeniorityStateRepository
{
    Task<List<SeniorityState>> GetAllAsync();
    Task<List<SeniorityState>> GetAllAsync(int pageNumber, int pageSize);
    Task<SeniorityState?> GetByIdAsync(ControlNumber ctrlNbr);
    Task<SeniorityState?> GetByCtrlNbrAsync(long ctrlNbr);

    Task AddAsync(SeniorityState seniorityState);
    Task UpdateAsync(SeniorityState seniorityState);
    Task DeleteAsync(ControlNumber ctrlNbr);

    void Add(SeniorityState seniorityState);
    void Update(SeniorityState seniorityState);
    void Remove(SeniorityState seniorityState);
}