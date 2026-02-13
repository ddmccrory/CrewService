using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class PhoneNumberTypeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<PhoneNumberType>(dbContext, currentUserService), IPhoneNumberTypeRepository
{
    public async Task<List<PhoneNumberType>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr)
    {
        return await DbContext.Set<PhoneNumberType>()
            .Where(pt => pt.ClientCtrlNbr == clientCtrlNbr)
            .OrderBy(pt => pt.Number)
            .ToListAsync();
    }

    public async Task<List<PhoneNumberType>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr, int pageNumber, int pageSize)
    {
        return await DbContext.Set<PhoneNumberType>()
            .Where(pt => pt.ClientCtrlNbr == clientCtrlNbr)
            .OrderBy(pt => pt.Number)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}