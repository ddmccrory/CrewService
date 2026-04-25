using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed record FraRecordSearchCriteria
{
    public ControlNumber? EmployeeCtrlNbr { get; init; }
    public DateTime? StartDateUtc { get; init; }
    public DateTime? EndDateUtc { get; init; }
    public string? LocationCode { get; init; }
    public string? RegulatoryStandardCode { get; init; }
    public bool? HasExcessService { get; init; }
    public bool? IsCertified { get; init; }
}

public interface IFraDutyTourRepository
{
    Task<IReadOnlyList<FraDutyTour>> SearchAsync(FraRecordSearchCriteria criteria, CancellationToken ct = default);
    Task<FraDutyTour?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task AddAsync(FraDutyTour tour, CancellationToken ct = default);
    Task<FraDutyTour?> GetActiveTourForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);
}

public interface IRegulatoryStandardRepository
{
    Task<List<RegulatoryStandard>> GetAllAsync(CancellationToken ct = default);
    Task<RegulatoryStandard?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task AddAsync(RegulatoryStandard standard, CancellationToken ct = default);
}

public interface IRegulatoryQualificationRepository
{
    Task<List<RegulatoryQualification>> GetAllAsync(CancellationToken ct = default);
    Task<RegulatoryQualification?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task AddAsync(RegulatoryQualification qualification, CancellationToken ct = default);
}

public interface ICertificationRevocationRepository
{
    Task<CertificationRevocationRecord?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task<IReadOnlyList<CertificationRevocationRecord>> GetByCertificationCtrlNbrAsync(ControlNumber employeeCertificationCtrlNbr, CancellationToken ct = default);
    Task AddAsync(CertificationRevocationRecord revocationRecord, CancellationToken ct = default);
    Task UpdateAsync(CertificationRevocationRecord revocationRecord, CancellationToken ct = default);
}

public interface IDrugAlcoholTestRepository
{
    Task<IReadOnlyList<DrugAlcoholTestRecord>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);
    Task AddAsync(DrugAlcoholTestRecord testRecord, CancellationToken ct = default);
}

public interface IDrugAlcoholActionRepository
{
    Task<IReadOnlyList<DrugAlcoholAction>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);
    Task AddAsync(DrugAlcoholAction action, CancellationToken ct = default);
}

public interface IVoluntaryReferralRepository
{
    Task<IReadOnlyList<VoluntaryReferral>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);
    Task<VoluntaryReferral?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task AddAsync(VoluntaryReferral referral, CancellationToken ct = default);
    Task UpdateAsync(VoluntaryReferral referral, CancellationToken ct = default);
}

public interface IEmployeeCertificationReadRepository
{
    Task<IReadOnlyList<EmployeeCertification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);
    Task<IReadOnlyList<CertificationWithEmployeeDto>> GetByClientAndStatusesAsync(ControlNumber clientCtrlNbr, IReadOnlyCollection<string> statuses, CancellationToken ct = default);
}

public interface IFraCertificationConfigRepository
{
    Task<List<FraCertificationConfig>> GetAllAsync(CancellationToken ct = default);
    Task<FraCertificationConfig?> GetByParentAsync(ControlNumber parentCtrlNbr, CancellationToken ct = default);
    Task<FraCertificationConfig?> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default);
    Task AddAsync(FraCertificationConfig config, CancellationToken ct = default);
    Task UpdateAsync(FraCertificationConfig config, CancellationToken ct = default);
}

public interface IFraCertificationCheckConfigRepository
{
    Task<List<FraCertificationCheckConfig>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FraCertificationCheckConfig>> GetByParentAsync(ControlNumber parentCtrlNbr, CancellationToken ct = default);
    Task<IReadOnlyList<FraCertificationCheckConfig>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default);
    Task AddAsync(FraCertificationCheckConfig config, CancellationToken ct = default);
    Task UpdateAsync(FraCertificationCheckConfig config, CancellationToken ct = default);
    Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
}

public interface IEmployeeCertificationRepository
{
    Task<List<EmployeeCertification>> GetAllAsync(CancellationToken ct = default);
    Task<List<EmployeeCertification>> GetAllWithChecksAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeCertification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);
    Task<EmployeeCertification?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task<EmployeeCertification?> GetByCtrlNbrWithChecksAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task<EmployeeCertification?> GetByEligibilityCheckCtrlNbrWithChecksAsync(ControlNumber eligibilityCheckCtrlNbr, CancellationToken ct = default);
    Task<EmployeeCertification?> GetByEmployeeAndRegulatoryQualAsync(ControlNumber employeeCtrlNbr, ControlNumber regulatoryQualCtrlNbr, CancellationToken ct = default);
    Task AddAsync(EmployeeCertification certification, CancellationToken ct = default);
    void Add(EmployeeCertification certification);
    Task UpdateAsync(EmployeeCertification certification, CancellationToken ct = default);
    Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
}
