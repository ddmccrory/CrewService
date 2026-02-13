using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class EmailAddressTypeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<EmailAddressType>(dbContext, currentUserService), IEmailAddressTypeRepository
{
    public async Task<List<EmailAddressType>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr)
    {
        return await DbContext.Set<EmailAddressType>()
            .Where(et => et.ClientCtrlNbr == clientCtrlNbr)
            .OrderBy(et => et.Number)
            .ToListAsync();
    }

    public async Task<List<EmailAddressType>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr, int pageNumber, int pageSize)
    {
        return await DbContext.Set<EmailAddressType>()
            .Where(et => et.ClientCtrlNbr == clientCtrlNbr)
            .OrderBy(et => et.Number)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}