using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class AddressTypeRepository(CrewServiceDbContext dbContext)
    : Repository<AddressType>(dbContext), IAddressTypeRepository
{
    public async Task<List<AddressType>> GetAllAsync(long clientCtrlNbr)
    {
        return await GetByClientCtrlNbrAsync(clientCtrlNbr);
    }

    public async Task<List<AddressType>> GetAllAsync(long clientCtrlNbr, int pageNumber, int pageSize)
    {
        if (clientCtrlNbr <= 0)
            throw new ArgumentException("Client control number must be greater than zero", nameof(clientCtrlNbr));

        return await DbContext.Set<AddressType>()
            .Where(at => at.ClientCtrlNbr == ControlNumber.Create(clientCtrlNbr))
            .OrderBy(at => at.Number)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<AddressType?> GetByIdAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<AddressType>()
            .SingleOrDefaultAsync(at => at.CtrlNbr == ctrlNbr);
    }

    public async Task<List<AddressType>> GetByClientCtrlNbrAsync(long clientCtrlNbr)
    {
        if (clientCtrlNbr <= 0)
            throw new ArgumentException("Client control number must be greater than zero", nameof(clientCtrlNbr));

        return await DbContext.Set<AddressType>()
            .Where(at => at.ClientCtrlNbr == ControlNumber.Create(clientCtrlNbr))
            .OrderBy(at => at.Number)
            .ToListAsync();
    }

    public async Task AddAsync(AddressType addressType)
    {
        DbContext.Set<AddressType>().Add(addressType);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(AddressType addressType)
    {
        DbContext.Set<AddressType>().Update(addressType);
        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByIdAsync(ctrlNbr);
        if (entity is not null)
        {
            DbContext.Set<AddressType>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }
}