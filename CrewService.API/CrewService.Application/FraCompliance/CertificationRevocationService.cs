using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.FraCompliance;

public sealed class CertificationRevocationService(
    IEmployeeCertificationRepository employeeCertificationRepository,
    ICertificationRevocationRepository certificationRevocationRepository,
    IRegulatoryQualificationRepository regulatoryQualificationRepository,
    DrugAlcoholCertificationImpactHandler impactHandler)
{
    public async Task<CertificationRevocationRecord> StartRevocationAsync(
        ControlNumber employeeCertificationCtrlNbr,
        string violationType,
        DateTime violationDateUtc,
        CancellationToken ct = default)
    {
        var certification = await employeeCertificationRepository.GetByCtrlNbrAsync(employeeCertificationCtrlNbr, ct)
            ?? throw new InvalidOperationException("Employee certification not found");

        certification.Suspend($"Violation {violationType}");
        await employeeCertificationRepository.UpdateAsync(certification, ct);

        var record = CertificationRevocationRecord.Create(employeeCertificationCtrlNbr, violationType, violationDateUtc);
        await certificationRevocationRepository.AddAsync(record, ct);

        await ApplyCrossRevocationIfRequiredAsync(certification, violationType, ct);

        return record;
    }

    public async Task RecordWrittenNoticeAsync(ControlNumber revocationRecordCtrlNbr, CancellationToken ct = default)
    {
        var record = await certificationRevocationRepository.GetByCtrlNbrAsync(revocationRecordCtrlNbr, ct)
            ?? throw new InvalidOperationException("Revocation record not found");

        if (DateTime.UtcNow > record.SuspendedAtUtc.AddHours(96))
            throw new InvalidOperationException("Written notice exceeds 96-hour FRA window");

        record.RecordWrittenNotice();
        await certificationRevocationRepository.UpdateAsync(record, ct);
    }

    public async Task ScheduleHearingAsync(ControlNumber revocationRecordCtrlNbr, DateTime hearingDateUtc, CancellationToken ct = default)
    {
        var record = await certificationRevocationRepository.GetByCtrlNbrAsync(revocationRecordCtrlNbr, ct)
            ?? throw new InvalidOperationException("Revocation record not found");

        if (hearingDateUtc > record.SuspendedAtUtc.AddDays(10))
            throw new InvalidOperationException("Hearing exceeds 10-day FRA window");

        record.ScheduleHearing(hearingDateUtc);
        await certificationRevocationRepository.UpdateAsync(record, ct);
    }

    public async Task DecideAsync(
        ControlNumber revocationRecordCtrlNbr,
        string decision,
        int? revocationPeriodMonths,
        CancellationToken ct = default)
    {
        var record = await certificationRevocationRepository.GetByCtrlNbrAsync(revocationRecordCtrlNbr, ct)
            ?? throw new InvalidOperationException("Revocation record not found");

        var certification = await employeeCertificationRepository.GetByCtrlNbrAsync(record.EmployeeCertificationCtrlNbr, ct)
            ?? throw new InvalidOperationException("Employee certification not found");

        record.Decide(decision, revocationPeriodMonths);
        await certificationRevocationRepository.UpdateAsync(record, ct);

        if (string.Equals(decision, "Revoked", StringComparison.OrdinalIgnoreCase))
            certification.Revoke(record.RevocationEndsUtc ?? DateTime.UtcNow);
        else
            certification.Reinstate();

        await employeeCertificationRepository.UpdateAsync(certification, ct);
    }

    private async Task ApplyCrossRevocationIfRequiredAsync(EmployeeCertification certification, string violationType, CancellationToken ct)
    {
        if (!impactHandler.ShouldCrossRevoke(violationType))
            return;

        var conductorQual = await regulatoryQualificationRepository.GetByCodeAsync("CFR-242-CONDUCTOR", ct);
        var engineerQual = await regulatoryQualificationRepository.GetByCodeAsync("CFR-240-ENGINEER", ct);

        if (conductorQual is null || engineerQual is null)
            return;

        if (certification.RegulatoryQualificationCtrlNbr != conductorQual.CtrlNbr)
            return;

        var certifications = await employeeCertificationRepository.GetByEmployeeCtrlNbrAsync(certification.EmployeeCtrlNbr, ct);
        var engineerCert = certifications.FirstOrDefault(c => c.RegulatoryQualificationCtrlNbr == engineerQual.CtrlNbr && c.Status != "Revoked");

        if (engineerCert is null)
            return;

        engineerCert.Revoke(DateTime.UtcNow);
        await employeeCertificationRepository.UpdateAsync(engineerCert, ct);
    }
}
