using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal abstract class Repository<TEntity>(CrewServiceDbContext dbContext)
    where TEntity : Entity
{
    protected readonly CrewServiceDbContext DbContext = dbContext;

    public virtual async Task<List<TEntity>> GetAllAsync()
    {
        return await DbContext.Set<TEntity>().ToListAsync();
    }

    public virtual async Task<TEntity?> GetByCtrlNbrAsync(long ctrlNbr)
    {
        if (ctrlNbr <= 0)
            throw new ArgumentNullException(nameof(ctrlNbr), $"The {typeof(TEntity).Name} control number cannot be zero or less");

        return await DbContext.Set<TEntity>().SingleOrDefaultAsync(c => c.CtrlNbr == ControlNumber.Create(ctrlNbr));
    }

    public void Add(TEntity entity)
    {
        DbContext.Set<TEntity>().Add(entity);
        DbContext.SaveChangesAsync();
    }

    public void Update(TEntity entity)
    {
        DbContext.Set<TEntity>().Update(entity);
        DbContext.SaveChangesAsync();
    }

    public void Remove(TEntity entity)
    {
        DbContext.Set<TEntity>().Remove(entity);
        DbContext.SaveChangesAsync();
    }
}
