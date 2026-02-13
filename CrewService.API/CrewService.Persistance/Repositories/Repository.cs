using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal abstract class Repository<TEntity>(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : IRepository<TEntity> where TEntity : Entity
{
    protected readonly CrewServiceDbContext DbContext = dbContext;
    protected readonly ICurrentUserService CurrentUserService = currentUserService;

    #region Read Operations

    public virtual async Task<List<TEntity>> GetAllAsync()
    {
        return await DbContext.Set<TEntity>().ToListAsync();
    }

    public virtual async Task<List<TEntity>> GetAllAsync(int pageNumber, int pageSize)
    {
        return await DbContext.Set<TEntity>()
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public virtual async Task<TEntity?> GetByCtrlNbrAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<TEntity>().SingleOrDefaultAsync(c => c.CtrlNbr == ctrlNbr);
    }

    public virtual async Task<TEntity?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<TEntity>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(c => c.CtrlNbr == ctrlNbr);
    }

    #endregion

    #region Write Operations (sync - for Unit of Work)

    public void Add(TEntity entity)
    {
        DbContext.Set<TEntity>().Add(entity);
    }

    public void Update(TEntity entity)
    {
        DbContext.Set<TEntity>().Update(entity);
    }

    public void Remove(TEntity entity)
    {
        entity.SoftDelete(CurrentUserService.GetUserName());
        DbContext.Set<TEntity>().Update(entity);
    }

    #endregion

    #region Write Operations (async - immediate save)

    public virtual async Task AddAsync(TEntity entity)
    {
        DbContext.Set<TEntity>().Add(entity);
        await DbContext.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(TEntity entity)
    {
        DbContext.Set<TEntity>().Update(entity);
        await DbContext.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByCtrlNbrAsync(ctrlNbr);
        if (entity is not null)
        {
            entity.SoftDelete(CurrentUserService.GetUserName());
            DbContext.Set<TEntity>().Update(entity);
            await DbContext.SaveChangesAsync();
        }
    }

    public virtual async Task RestoreAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByCtrlNbrIncludingDeletedAsync(ctrlNbr);
        if (entity is not null)
        {
            entity.Restore();
            DbContext.Set<TEntity>().Update(entity);
            await DbContext.SaveChangesAsync();
        }
    }

    #endregion
}
