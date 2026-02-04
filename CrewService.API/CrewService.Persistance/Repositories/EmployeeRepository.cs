using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class EmployeeRepository(CrewServiceDbContext dbContext)
    : Repository<Employee>(dbContext), IEmployeeRepository
{
    public override async Task<List<Employee>> GetAllAsync()
    {
        return await DbContext.Set<Employee>()
            .Include(e => e.Addresses)
            .Include(e => e.PhoneNumbers)
            .Include(e => e.EmailAddresses)
            .ToListAsync();
    }

    public async Task<List<Employee>> GetAllAsync(int pageNumber, int pageSize)
    {
        return await DbContext.Set<Employee>()
            .Include(e => e.Addresses)
            .Include(e => e.PhoneNumbers)
            .Include(e => e.EmailAddresses)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public override async Task<Employee?> GetByCtrlNbrAsync(long ctrlNbr)
    {
        if (ctrlNbr <= 0)
            throw new ArgumentNullException(nameof(ctrlNbr), "The Employee control number cannot be zero or less");

        return await DbContext.Set<Employee>()
            .Include(e => e.Addresses)
            .Include(e => e.PhoneNumbers)
            .Include(e => e.EmailAddresses)
            .SingleOrDefaultAsync(e => e.CtrlNbr == ControlNumber.Create(ctrlNbr));
    }

    public async Task<Employee?> GetByEmployeeNumberAsync(string employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new ArgumentNullException(nameof(employeeNumber), "The Employee number cannot be null or empty");

        return await DbContext.Set<Employee>()
            .Include(e => e.Addresses)
            .Include(e => e.PhoneNumbers)
            .Include(e => e.EmailAddresses)
            .SingleOrDefaultAsync(e => e.EmployeeNumber == employeeNumber);
    }
}