using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class ParentRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Parent>(dbContext, currentUserService), IParentRepository
{
    public override async Task<List<Parent>> GetAllAsync()
    {
        return await DbContext.Set<Parent>()
            .Include(p => p.Railroads)
            .ToListAsync();
    }

    public override async Task<Parent?> GetByCtrlNbrAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<Parent>()
            .Include(p => p.Railroads)
            .SingleOrDefaultAsync(p => p.CtrlNbr == ctrlNbr);
    }
}