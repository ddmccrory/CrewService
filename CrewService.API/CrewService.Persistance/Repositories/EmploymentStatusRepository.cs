using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class EmploymentStatusRepository(CrewServiceDbContext dbContext)
    : Repository<EmploymentStatus>(dbContext), IEmploymentStatusRepository
{
    public async Task<List<EmploymentStatus>> GetAllAsync(long clientCtrlNbr)
    {
        return await GetByClientCtrlNbrAsync(clientCtrlNbr);
    }

    public async Task<List<EmploymentStatus>> GetAllAsync(long clientCtrlNbr, int pageNumber, int pageSize)
    {
        if (clientCtrlNbr <= 0)
            throw new ArgumentException("Client control number must be greater than zero", nameof(clientCtrlNbr));

        return await DbContext.Set<EmploymentStatus>()
            .Where(es => es.ClientCtrlNbr == ControlNumber.Create(clientCtrlNbr))
            .OrderBy(es => es.StatusNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<EmploymentStatus?> GetByIdAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<EmploymentStatus>()
            .SingleOrDefaultAsync(es => es.CtrlNbr == ctrlNbr);
    }

    public async Task<List<EmploymentStatus>> GetByClientCtrlNbrAsync(long clientCtrlNbr)
    {
        if (clientCtrlNbr <= 0)
            throw new ArgumentException("Client control number must be greater than zero", nameof(clientCtrlNbr));

        return await DbContext.Set<EmploymentStatus>()
            .Where(es => es.ClientCtrlNbr == ControlNumber.Create(clientCtrlNbr))
            .OrderBy(es => es.StatusNumber)
            .ToListAsync();
    }

    public async Task AddAsync(EmploymentStatus employmentStatus)
    {
        DbContext.Set<EmploymentStatus>().Add(employmentStatus);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(EmploymentStatus employmentStatus)
    {
        DbContext.Set<EmploymentStatus>().Update(employmentStatus);
        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByIdAsync(ctrlNbr);
        if (entity is not null)
        {
            DbContext.Set<EmploymentStatus>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }
}