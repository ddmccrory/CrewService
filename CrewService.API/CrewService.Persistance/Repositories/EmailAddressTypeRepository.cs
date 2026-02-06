using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class EmailAddressTypeRepository(CrewServiceDbContext dbContext)
    : Repository<EmailAddressType>(dbContext), IEmailAddressTypeRepository
{
    public async Task<List<EmailAddressType>> GetAllAsync(long clientCtrlNbr)
    {
        return await GetByClientCtrlNbrAsync(clientCtrlNbr);
    }

    public async Task<List<EmailAddressType>> GetAllAsync(long clientCtrlNbr, int pageNumber, int pageSize)
    {
        if (clientCtrlNbr <= 0)
            throw new ArgumentException("Client control number must be greater than zero", nameof(clientCtrlNbr));

        return await DbContext.Set<EmailAddressType>()
            .Where(et => et.ClientCtrlNbr == ControlNumber.Create(clientCtrlNbr))
            .OrderBy(et => et.Number)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<EmailAddressType?> GetByIdAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<EmailAddressType>()
            .SingleOrDefaultAsync(et => et.CtrlNbr == ctrlNbr);
    }

    public async Task<List<EmailAddressType>> GetByClientCtrlNbrAsync(long clientCtrlNbr)
    {
        if (clientCtrlNbr <= 0)
            throw new ArgumentException("Client control number must be greater than zero", nameof(clientCtrlNbr));

        return await DbContext.Set<EmailAddressType>()
            .Where(et => et.ClientCtrlNbr == ControlNumber.Create(clientCtrlNbr))
            .OrderBy(et => et.Number)
            .ToListAsync();
    }

    public async Task AddAsync(EmailAddressType emailAddressType)
    {
        DbContext.Set<EmailAddressType>().Add(emailAddressType);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(EmailAddressType emailAddressType)
    {
        DbContext.Set<EmailAddressType>().Update(emailAddressType);
        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByIdAsync(ctrlNbr);
        if (entity is not null)
        {
            DbContext.Set<EmailAddressType>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }
}