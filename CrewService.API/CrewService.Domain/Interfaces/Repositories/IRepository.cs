using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRepository<TEntity> where TEntity : Entity
{
    // Read
    Task<List<TEntity>> GetAllAsync();
    Task<List<TEntity>> GetAllAsync(int pageNumber, int pageSize);
    Task<TEntity?> GetByCtrlNbrAsync(ControlNumber ctrlNbr);
    Task<TEntity?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr);

    // Write (async - immediate save)
    Task AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(ControlNumber ctrlNbr);
    Task RestoreAsync(ControlNumber ctrlNbr);

    // Write (sync - for Unit of Work)
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}