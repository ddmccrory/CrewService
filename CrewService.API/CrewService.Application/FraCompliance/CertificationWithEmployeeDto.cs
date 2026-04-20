using CrewService.Domain.Modules.FraCompliance;

namespace CrewService.Application.FraCompliance;

public sealed record CertificationWithEmployeeDto(
    EmployeeCertification Certification,
    string EmployeeNumber,
    string UserId);
