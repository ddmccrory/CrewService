using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmailAddressRepository
{
    Task<EmailAddress?> GetByIdAsync(ControlNumber employeeCtrlNbr, CancellationToken cancellationToken = default);
    Task AddAsync(EmailAddress emailAddress, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmailAddress emailAddress, CancellationToken cancellationToken = default);
    Task DeleteAsync(ControlNumber employeeCtrlNbr, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailAddress>> GetAllByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken cancellationToken = default);
}