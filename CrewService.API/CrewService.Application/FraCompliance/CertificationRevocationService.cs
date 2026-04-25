using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.FraCompliance;

public sealed class CertificationRevocationService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    DrugAlcoholCertificationImpactHandler impactHandler)
{
    public async Task<CertificationRevocationRecord> StartRevocationAsync(
        ControlNumber employeeCertificationCtrlNbr, string violationType, DateTime violationDateUtc, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var certification = await uow.EmployeeCertifications.GetByCtrlNbrAsync(employeeCertificationCtrlNbr, ct)
            ?? throw new InvalidOperationException("Employee certification not found");
        certification.Suspend($"Violation {violationType}");
        await uow.EmployeeCertifications.UpdateAsync(certification, ct);
        var record = CertificationRevocationRecord.Create(employeeCertificationCtrlNbr, violationType, violationDateUtc);
        await uow.CertificationRevocations.AddAsync(record, ct);
        await ApplyCrossRevocationIfRequiredAsync(uow, certification, violationType, ct);
        await uow.CommitAsync(ct);
        return record;
    }

    public async Task RecordWrittenNoticeAsync(ControlNumber revocationRecordCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var record = await uow.CertificationRevocations.GetByCtrlNbrAsync(revocationRecordCtrlNbr, ct)
            ?? throw new InvalidOperationException("Revocation record not found");
        if (DateTime.UtcNow > record.SuspendedAtUtc.AddHours(96))
            throw new InvalidOperationException("Written notice exceeds 96-hour FRA window");
        record.RecordWrittenNotice();
        await uow.CertificationRevocations.UpdateAsync(record, ct);
        await uow.CommitAsync(ct);
    }

    public async Task ScheduleHearingAsync(ControlNumber revocationRecordCtrlNbr, DateTime hearingDateUtc, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var record = await uow.CertificationRevocations.GetByCtrlNbrAsync(revocationRecordCtrlNbr, ct)
            ?? throw new InvalidOperationException("Revocation record not found");
        if (hearingDateUtc > record.SuspendedAtUtc.AddDays(10))
            throw new InvalidOperationException("Hearing exceeds 10-day FRA window");
        record.ScheduleHearing(hearingDateUtc);
        await uow.CertificationRevocations.UpdateAsync(record, ct);
        await uow.CommitAsync(ct);
    }

    public async Task DecideAsync(
        ControlNumber revocationRecordCtrlNbr, string decision, int? revocationPeriodMonths, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var record = await uow.CertificationRevocations.GetByCtrlNbrAsync(revocationRecordCtrlNbr, ct)
            ?? throw new InvalidOperationException("Revocation record not found");
        var certification = await uow.EmployeeCertifications.GetByCtrlNbrWithChecksAsync(record.EmployeeCertificationCtrlNbr, ct)
            ?? throw new InvalidOperationException("Employee certification not found");
        record.Decide(decision, revocationPeriodMonths);
        await uow.CertificationRevocations.UpdateAsync(record, ct);
        if (string.Equals(decision, "Revoked", StringComparison.OrdinalIgnoreCase))
            certification.Revoke(record.RevocationEndsUtc ?? DateTime.UtcNow);
        else
            certification.Reinstate(DateOnly.FromDateTime(DateTime.UtcNow));
        await uow.EmployeeCertifications.UpdateAsync(certification, ct);
        await uow.CommitAsync(ct);
    }

    private async Task ApplyCrossRevocationIfRequiredAsync(IOrchestrationUnitOfWork uow, EmployeeCertification certification, string violationType, CancellationToken ct)
    {
        if (!impactHandler.ShouldCrossRevoke(violationType)) return;
        var conductorQual = await uow.RegulatoryQualifications.GetByCodeAsync("CFR-242-CONDUCTOR", ct);
        var engineerQual = await uow.RegulatoryQualifications.GetByCodeAsync("CFR-240-ENGINEER", ct);
        if (conductorQual is null || engineerQual is null) return;
        if (certification.RegulatoryQualificationCtrlNbr != conductorQual.CtrlNbr) return;
        var certifications = await uow.EmployeeCertifications.GetByEmployeeCtrlNbrAsync(certification.EmployeeCtrlNbr, ct);
        var engineerCert = certifications.FirstOrDefault(c => c.RegulatoryQualificationCtrlNbr == engineerQual.CtrlNbr && c.Status != "Revoked");
        if (engineerCert is null) return;
        engineerCert.Revoke(DateTime.UtcNow);
        await uow.EmployeeCertifications.UpdateAsync(engineerCert, ct);
    }
}

