using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IPhoneNumberRepository
{
    Task<PhoneNumber?> GetByIdAsync(ControlNumber employeeCtrlNbr, CancellationToken cancellationToken = default);
    Task AddAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default);
    Task UpdateAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default);
    Task DeleteAsync(ControlNumber employeeCtrlNbr, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhoneNumber>> GetAllByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken cancellationToken = default);
}