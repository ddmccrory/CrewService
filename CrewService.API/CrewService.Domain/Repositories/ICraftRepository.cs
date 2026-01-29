using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Repositories;

public interface ICraftRepository
{
    Task<List<Craft>> GetAllAsync();
    Task<Craft?> GetByIdAsync(ControlNumber ctrlNbr);
    Task AddAsync(Craft craft);
    Task UpdateAsync(Craft craft);
    Task DeleteAsync(ControlNumber ctrlNbr);
}