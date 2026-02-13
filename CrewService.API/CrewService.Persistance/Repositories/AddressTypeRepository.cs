using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class AddressTypeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<AddressType>(dbContext, currentUserService), IAddressTypeRepository
{
    public async Task<List<AddressType>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr)
    {
        return await DbContext.Set<AddressType>()
            .Where(at => at.ClientCtrlNbr == clientCtrlNbr)
            .OrderBy(at => at.Number)
            .ToListAsync();
    }

    public async Task<List<AddressType>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr, int pageNumber, int pageSize)
    {
        return await DbContext.Set<AddressType>()
            .Where(at => at.ClientCtrlNbr == clientCtrlNbr)
            .OrderBy(at => at.Number)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}