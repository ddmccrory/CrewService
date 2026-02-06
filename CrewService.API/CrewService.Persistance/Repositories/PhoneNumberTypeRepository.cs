using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class PhoneNumberTypeRepository(CrewServiceDbContext dbContext)
    : Repository<PhoneNumberType>(dbContext), IPhoneNumberTypeRepository
{
    public async Task<List<PhoneNumberType>> GetAllAsync(long clientCtrlNbr)
    {
        return await GetByClientCtrlNbrAsync(clientCtrlNbr);
    }

    public async Task<List<PhoneNumberType>> GetAllAsync(long clientCtrlNbr, int pageNumber, int pageSize)
    {
        if (clientCtrlNbr <= 0)
            throw new ArgumentException("Client control number must be greater than zero", nameof(clientCtrlNbr));

        return await DbContext.Set<PhoneNumberType>()
            .Where(pt => pt.ClientCtrlNbr == ControlNumber.Create(clientCtrlNbr))
            .OrderBy(pt => pt.Number)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<PhoneNumberType?> GetByIdAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<PhoneNumberType>()
            .SingleOrDefaultAsync(pt => pt.CtrlNbr == ctrlNbr);
    }

    public async Task<List<PhoneNumberType>> GetByClientCtrlNbrAsync(long clientCtrlNbr)
    {
        if (clientCtrlNbr <= 0)
            throw new ArgumentException("Client control number must be greater than zero", nameof(clientCtrlNbr));

        return await DbContext.Set<PhoneNumberType>()
            .Where(pt => pt.ClientCtrlNbr == ControlNumber.Create(clientCtrlNbr))
            .OrderBy(pt => pt.Number)
            .ToListAsync();
    }

    public async Task AddAsync(PhoneNumberType phoneNumberType)
    {
        DbContext.Set<PhoneNumberType>().Add(phoneNumberType);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(PhoneNumberType phoneNumberType)
    {
        DbContext.Set<PhoneNumberType>().Update(phoneNumberType);
        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByIdAsync(ctrlNbr);
        if (entity is not null)
        {
            DbContext.Set<PhoneNumberType>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }
}