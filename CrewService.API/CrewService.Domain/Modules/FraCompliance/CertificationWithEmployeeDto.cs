namespace CrewService.Domain.Modules.FraCompliance;

public sealed record CertificationWithEmployeeDto(
    EmployeeCertification Certification,
    string EmployeeNumber,
    string UserId);
