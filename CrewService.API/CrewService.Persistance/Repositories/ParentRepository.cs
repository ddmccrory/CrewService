using CrewService.Domain.Models.Parents;
using CrewService.Domain.Repositories;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class ParentRepository(CrewAssignmentDbContext dbContext)
    : Repository<Parent>(dbContext), IParentRepository
{

    public override async Task<List<Parent>> GetAllAsync()
    {
        return await DbContext.Set<Parent>().Include(c => c.Railroads).ToListAsync();
    }

    public override async Task<Parent?> GetByCtrlNbrAsync(long ctrlNbr)
    {
        if (ctrlNbr <= 0)
            throw new ArgumentNullException(nameof(ctrlNbr), "(The Parent control number cannot be zero or less");

        return await DbContext.Set<Parent>()
            .Include(c => c.Railroads)
            .SingleOrDefaultAsync(c => c.CtrlNbr == ControlNumber.Create(ctrlNbr));
    }
}