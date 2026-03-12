using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRepository<TEntity> where TEntity : Entity
{
    // Read
    Task<List<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<List<TEntity>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default);
    Task<TEntity?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task<TEntity?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default);

    // Write (async - immediate save)
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default);

    // Write (sync - for Unit of Work)
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}