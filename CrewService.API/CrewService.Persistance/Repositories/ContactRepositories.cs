using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class AddressRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Address>(dbContext, currentUserService), IAddressRepository
{
    public async Task<List<Address>> GetAllByEmployeeAsync(ControlNumber employeeCtrlNbr) =>
        await DbContext.Set<Address>()
            .Where(a => a.EmployeeCtrlNbr == employeeCtrlNbr)
            .ToListAsync();

    public async Task<List<Address>> GetAllByEmployeeAsync(ControlNumber employeeCtrlNbr, int pageNumber, int pageSize) =>
        await DbContext.Set<Address>()
            .Where(a => a.EmployeeCtrlNbr == employeeCtrlNbr)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
}

internal sealed class PhoneNumberRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<PhoneNumber>(dbContext, currentUserService), IPhoneNumberRepository
{
    public async Task<PhoneNumber?> GetByIdAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<PhoneNumber>()
            .FirstOrDefaultAsync(p => p.EmployeeCtrlNbr == employeeCtrlNbr, ct);

    public async Task<IReadOnlyList<PhoneNumber>> GetAllByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<PhoneNumber>()
            .Where(p => p.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderBy(p => p.CallingOrder)
            .ToListAsync(ct);
}

internal sealed class EmailAddressRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<EmailAddress>(dbContext, currentUserService), IEmailAddressRepository
{
    public async Task<EmailAddress?> GetByIdAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<EmailAddress>()
            .FirstOrDefaultAsync(e => e.EmployeeCtrlNbr == employeeCtrlNbr, ct);

    public async Task<IReadOnlyList<EmailAddress>> GetAllByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<EmailAddress>()
            .Where(e => e.EmployeeCtrlNbr == employeeCtrlNbr)
            .ToListAsync(ct);
}
