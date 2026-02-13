using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class EmploymentStatusRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<EmploymentStatus>(dbContext, currentUserService), IEmploymentStatusRepository
{
    public async Task<List<EmploymentStatus>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr)
    {
        return await DbContext.Set<EmploymentStatus>()
            .Where(es => es.ClientCtrlNbr == clientCtrlNbr)
            .OrderBy(es => es.StatusNumber)
            .ToListAsync();
    }

    public async Task<List<EmploymentStatus>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr, int pageNumber, int pageSize)
    {
        return await DbContext.Set<EmploymentStatus>()
            .Where(es => es.ClientCtrlNbr == clientCtrlNbr)
            .OrderBy(es => es.StatusNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}