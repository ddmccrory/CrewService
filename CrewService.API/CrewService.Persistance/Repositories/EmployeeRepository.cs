using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class EmployeeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Employee>(dbContext, currentUserService), IEmployeeRepository
{
    public override async Task<List<Employee>> GetAllAsync(CancellationToken ct = default)
    {
        return await DbContext.Set<Employee>()
            .Include(e => e.Addresses)
            .Include(e => e.PhoneNumbers)
            .Include(e => e.EmailAddresses)
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    public override async Task<List<Employee>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        return await DbContext.Set<Employee>()
            .Include(e => e.Addresses)
            .Include(e => e.PhoneNumbers)
            .Include(e => e.EmailAddresses)
            .AsSplitQuery()
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public override async Task<Employee?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<Employee>()
            .Include(e => e.Addresses)
            .Include(e => e.PhoneNumbers)
            .Include(e => e.EmailAddresses)
            .AsSplitQuery()
            .SingleOrDefaultAsync(e => e.CtrlNbr == ctrlNbr, ct);
    }

    public async Task<Employee?> GetByEmployeeNumberAsync(string employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new ArgumentException("The Employee number cannot be null or empty", nameof(employeeNumber));

        return await DbContext.Set<Employee>()
            .Include(e => e.Addresses)
            .Include(e => e.PhoneNumbers)
            .Include(e => e.EmailAddresses)
            .AsSplitQuery()
            .SingleOrDefaultAsync(e => e.EmployeeNumber == employeeNumber);
    }

    public async Task<Employee?> GetBySocialSecurityNumberAsync(string socialSecurityNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(socialSecurityNumber))
            throw new ArgumentException("The social security number cannot be null or empty", nameof(socialSecurityNumber));

        return await DbContext.Set<Employee>()
            .Include(e => e.Addresses)
            .Include(e => e.PhoneNumbers)
            .Include(e => e.EmailAddresses)
            .AsSplitQuery()
            .SingleOrDefaultAsync(e => e.SocialSecurityNumber == socialSecurityNumber, ct);
    }

    public async Task<Employee?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("The user id cannot be null or empty", nameof(userId));

        return await DbContext.Set<Employee>()
            .SingleOrDefaultAsync(e => e.UserId == userId, ct);
    }

    public async Task<List<Employee>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr)
    {
        return await DbContext.Set<Employee>()
            .Where(e => e.ClientCtrlNbr == clientCtrlNbr)
            .Include(e => e.Addresses)
            .Include(e => e.PhoneNumbers)
            .Include(e => e.EmailAddresses)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task<List<Employee>> GetListByClientCtrlNbrAsync(ControlNumber clientCtrlNbr)
    {
        return await DbContext.Set<Employee>()
            .Where(e => e.ClientCtrlNbr == clientCtrlNbr)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Employee>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs, CancellationToken ct = default)
    {
        var ctrlNbrList = ctrlNbrs.ToList();
        if (ctrlNbrList.Count == 0) return [];
        return await DbContext.Set<Employee>()
            .Where(e => ctrlNbrList.Contains(e.CtrlNbr))
            .ToListAsync(ct);
    }
}