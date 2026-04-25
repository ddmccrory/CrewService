using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.FraCompliance;

public sealed class FraComplianceService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    FraCertificationConfigService configService)
{
    public async Task<IReadOnlyList<FraDutyTour>> SearchDutyToursAsync(FraRecordSearchCriteria criteria, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.FraDutyTours.SearchAsync(criteria, ct);
    }

    public async Task<FraDutyTour> GetDutyTourAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.FraDutyTours.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException("Duty tour not found.");
    }

    public async Task<(IReadOnlyList<CertificationWithEmployeeDto> Certs, IReadOnlyList<EmployeeCertification> ByCertNbr)>
        GetCertificationsByEmployeeAsync(ControlNumber clientCtrlNbr, IReadOnlyCollection<string> statuses, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var certs = await uow.EmployeeCertificationReads.GetByClientAndStatusesAsync(clientCtrlNbr, statuses, ct);
        return (certs, []);
    }

    public async Task<EmployeeCertification> CreateEmployeeCertificationAsync(
        ControlNumber employeeCtrlNbr, ControlNumber regulatoryQualificationCtrlNbr,
        string certificationType, DateOnly certificationDate,
        ControlNumber? parentCtrlNbr, string certificationNumber, CancellationToken ct = default)
    {
        var certCycleMonths = await configService.GetCertCycleMonthsAsync(parentCtrlNbr, ct);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var certification = EmployeeCertification.Create(
            employeeCtrlNbr, regulatoryQualificationCtrlNbr, certificationType,
            certificationDate, certCycleMonths, certificationNumber);
        await uow.EmployeeCertifications.AddAsync(certification, ct);
        await uow.CommitAsync(ct);
        return certification;
    }

    public async Task<EmployeeCertification> UpdateEmployeeCertificationAsync(
        ControlNumber ctrlNbr, ControlNumber regulatoryQualificationCtrlNbr,
        string certificationType, DateOnly certificationDate,
        ControlNumber? parentCtrlNbr, string certificationNumber, CancellationToken ct = default)
    {
        var certCycleMonths = await configService.GetCertCycleMonthsAsync(parentCtrlNbr, ct);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var certification = await uow.EmployeeCertifications.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException("Employee certification not found.");
        certification.UpdateCertificationDetails(regulatoryQualificationCtrlNbr, certificationType,
            certificationDate, certCycleMonths, certificationNumber);
        await uow.EmployeeCertifications.UpdateAsync(certification, ct);
        await uow.CommitAsync(ct);
        return certification;
    }

    public async Task DeleteEmployeeCertificationAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        await uow.EmployeeCertifications.DeleteAsync(ctrlNbr, ct);
        await uow.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<CertificationRevocationRecord>> GetRevocationHistoryAsync(
        ControlNumber employeeCertificationCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.CertificationRevocations.GetByCertificationCtrlNbrAsync(employeeCertificationCtrlNbr, ct);
    }

    public async Task<CertificationRevocationRecord> GetRevocationRecordAsync(
        ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.CertificationRevocations.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException("Revocation record not found.");
    }

    public async Task<DrugAlcoholTestRecord> RecordDrugAlcoholTestAsync(
        ControlNumber employeeCtrlNbr, string testType, DateTime testDate,
        decimal? alcoholResult, string drugResult, string substancesDetected,
        bool federalAuthority, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var testRecord = DrugAlcoholTestRecord.Create(employeeCtrlNbr, testType, testDate,
            alcoholResult, drugResult, substancesDetected, federalAuthority);
        await uow.DrugAlcoholTests.AddAsync(testRecord, ct);
        await uow.CommitAsync(ct);
        return testRecord;
    }

    public async Task<IReadOnlyList<DrugAlcoholTestRecord>> GetDrugAlcoholTestsAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.DrugAlcoholTests.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr, ct);
    }

    public async Task<IReadOnlyList<DrugAlcoholAction>> GetDrugAlcoholActionsAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.DrugAlcoholActions.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr, ct);
    }

    public async Task<VoluntaryReferral> CreateVoluntaryReferralAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var referral = VoluntaryReferral.Create(employeeCtrlNbr);
        await uow.VoluntaryReferrals.AddAsync(referral, ct);
        await uow.CommitAsync(ct);
        return referral;
    }

    public async Task<IReadOnlyList<VoluntaryReferral>> GetVoluntaryReferralsAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.VoluntaryReferrals.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr, ct);
    }

    public async Task<VoluntaryReferral> GetVoluntaryReferralAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.VoluntaryReferrals.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException("Voluntary referral not found.");
    }

    public async Task UpdateVoluntaryReferralAsync(VoluntaryReferral referral, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        await uow.VoluntaryReferrals.UpdateAsync(referral, ct);
        await uow.CommitAsync(ct);
    }

    public async Task HandleDrugAlcoholActionsAsync(
        DrugAlcoholTestRecord testRecord,
        DrugAlcoholCertificationImpactHandler impactHandler,
        CancellationToken ct = default)
    {
        if (!testRecord.IsViolation && !testRecord.IsAlcoholRemovalRange)
            return;

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var removed = DrugAlcoholAction.Create(
            testRecord.CtrlNbr,
            testRecord.EmployeeCtrlNbr,
            "RemovedFromService",
            "Auto-generated from FRA Part 219 test result");
        await uow.DrugAlcoholActions.AddAsync(removed, ct);

        if (testRecord.IsViolation)
        {
            var prior = await uow.DrugAlcoholTests.GetByEmployeeCtrlNbrAsync(testRecord.EmployeeCtrlNbr, ct);
            var ineligibility = impactHandler.DetermineIneligibility(testRecord, [.. prior.Where(p => p.CtrlNbr != testRecord.CtrlNbr)]);
            var certifications = await uow.EmployeeCertifications.GetByEmployeeCtrlNbrAsync(testRecord.EmployeeCtrlNbr, ct);
            foreach (var cert in certifications.Where(c => c.Status == CertificationStatuses.Active))
            {
                cert.Suspend($"Drug/alcohol violation ({ineligibility.ViolationCount})");
                await uow.EmployeeCertifications.UpdateAsync(cert, ct);
            }
        }

        await uow.CommitAsync(ct);
    }

    public async Task<EmployeeCertification?> GetCertificationWithChecksAsync(
        ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmployeeCertifications.GetByCtrlNbrWithChecksAsync(ctrlNbr, ct);
    }

    public async Task<EmployeeCertification?> GetCertificationByEligibilityCheckAsync(
        ControlNumber checkCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmployeeCertifications.GetByEligibilityCheckCtrlNbrWithChecksAsync(checkCtrlNbr, ct);
    }

    public async Task<EmployeeCertification> GetCertificationAsync(
        ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmployeeCertifications.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException("Certification not found.");
    }

    public async Task<IReadOnlyList<EmployeeCertification>> GetEmployeeCertificationsAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmployeeCertificationReads.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr, ct);
    }

    public async Task<(CertificationEligibilityCheck Check, EmployeeCertification Certification)>
        AddEligibilityCheckAsync(
            ControlNumber certCtrlNbr, string checkType, DateOnly evaluationDate,
            int stalenessLimitDays, string result, string evaluatorName, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var certification = await uow.EmployeeCertifications.GetByCtrlNbrWithChecksAsync(certCtrlNbr, ct)
            ?? throw new KeyNotFoundException("Employee certification not found.");
        var check = certification.AddEligibilityCheck(checkType, evaluationDate, stalenessLimitDays, result, evaluatorName);
        await uow.EmployeeCertifications.UpdateAsync(certification, ct);
        await uow.CommitAsync(ct);
        return (check, certification);
    }

    public async Task<(CertificationEligibilityCheck Check, EmployeeCertification Certification)>
        UpdateEligibilityCheckAsync(
            ControlNumber checkCtrlNbr, string checkType, DateOnly evaluationDate,
            int stalenessLimitDays, string result, string evaluatorName, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var certification = await uow.EmployeeCertifications.GetByEligibilityCheckCtrlNbrWithChecksAsync(checkCtrlNbr, ct)
            ?? throw new KeyNotFoundException("Eligibility check not found.");
        var check = certification.UpdateEligibilityCheck(checkCtrlNbr, checkType, evaluationDate, stalenessLimitDays, result, evaluatorName);
        await uow.EmployeeCertifications.UpdateAsync(certification, ct);
        await uow.CommitAsync(ct);
        return (check, certification);
    }

    public async Task DeleteEligibilityCheckAsync(ControlNumber checkCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var certification = await uow.EmployeeCertifications.GetByEligibilityCheckCtrlNbrWithChecksAsync(checkCtrlNbr, ct)
            ?? throw new KeyNotFoundException("Eligibility check not found.");
        certification.DeleteEligibilityCheck(checkCtrlNbr);
        await uow.EmployeeCertifications.UpdateAsync(certification, ct);
        await uow.CommitAsync(ct);
    }

    public async Task<AddEmployeeRequirementResult> AddEmployeeRequirementResultAsync(
        ControlNumber employeeCtrlNbr, ControlNumber regulatoryQualificationCtrlNbr,
        string certificationType, DateOnly evaluationDate, ControlNumber? parentCtrlNbr,
        string checkType, string result, string evaluatorName, int stalenessLimitDays,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var existingCertifications = await uow.EmployeeCertifications.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr, ct);
        var certification = existingCertifications
            .OrderByDescending(c => c.CertificationDate)
            .FirstOrDefault(c =>
                c.RegulatoryQualificationCtrlNbr == regulatoryQualificationCtrlNbr
                && string.Equals(c.CertificationType, certificationType, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(c.Status, "Revoked", StringComparison.OrdinalIgnoreCase));

        if (certification is null)
        {
            var certCycleMonths = await configService.GetCertCycleMonthsAsync(parentCtrlNbr, ct);
            certification = EmployeeCertification.Create(employeeCtrlNbr, regulatoryQualificationCtrlNbr,
                certificationType, evaluationDate, certCycleMonths, null);
            await uow.EmployeeCertifications.AddAsync(certification, ct);
            await uow.CommitAsync(ct);
        }
        else
        {
            certification = await uow.EmployeeCertifications.GetByCtrlNbrWithChecksAsync(certification.CtrlNbr, ct)
                ?? throw new KeyNotFoundException("Employee certification not found.");
        }

        var check = certification.AddEligibilityCheck(checkType, evaluationDate, stalenessLimitDays, result, evaluatorName);
        await uow.EmployeeCertifications.UpdateAsync(certification, ct);
        await uow.CommitAsync(ct);
        return new AddEmployeeRequirementResult(check, certification);
    }

    public sealed record AddEmployeeRequirementResult(
        CertificationEligibilityCheck Check,
        EmployeeCertification Certification);
}
