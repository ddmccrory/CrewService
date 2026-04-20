using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.FraCompliance;

public interface IEmployeeCertificationReadRepository
{
    Task<IReadOnlyList<EmployeeCertification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);
    Task<IReadOnlyList<CertificationWithEmployeeDto>> GetByClientAndStatusesAsync(ControlNumber clientCtrlNbr, IReadOnlyCollection<string> statuses, CancellationToken ct = default);
}
