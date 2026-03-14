using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class CertificationRevocationRecord : Entity
{
    public ControlNumber EmployeeCertificationCtrlNbr { get; private set; }
    public string ViolationType { get; private set; } = string.Empty;
    public DateTime ViolationDate { get; private set; }
    public DateTime SuspendedAtUtc { get; private set; }
    public DateTime? WrittenNoticeAtUtc { get; private set; }
    public DateTime? HearingScheduledUtc { get; private set; }
    public DateTime? HearingHeldUtc { get; private set; }
    public ControlNumber? PresidingOfficerCtrlNbr { get; private set; }
    public string? Decision { get; private set; }
    public DateTime? DecisionDate { get; private set; }
    public int? RevocationPeriodMonths { get; private set; }
    public DateTime? RevocationEndsUtc { get; private set; }
    public DateOnly? HearingRecordRetainUntil { get; private set; }

    private CertificationRevocationRecord()
    {
        EmployeeCertificationCtrlNbr = null!;
    }

    public static CertificationRevocationRecord Create(
        ControlNumber employeeCertificationCtrlNbr,
        string violationType,
        DateTime violationDate)
    {
        return new CertificationRevocationRecord
        {
            EmployeeCertificationCtrlNbr = employeeCertificationCtrlNbr,
            ViolationType = violationType,
            ViolationDate = violationDate,
            SuspendedAtUtc = DateTime.UtcNow
        };
    }

    public void RecordWrittenNotice()
    {
        WrittenNoticeAtUtc = DateTime.UtcNow;
    }

    public void ScheduleHearing(DateTime hearingDateUtc)
    {
        HearingScheduledUtc = hearingDateUtc;
    }

    public void RecordHearing(DateTime heldUtc, ControlNumber presidingOfficerCtrlNbr)
    {
        HearingHeldUtc = heldUtc;
        PresidingOfficerCtrlNbr = presidingOfficerCtrlNbr;
    }

    public void Decide(string decision, int? revocationPeriodMonths)
    {
        Decision = decision;
        DecisionDate = DateTime.UtcNow;
        RevocationPeriodMonths = revocationPeriodMonths;

        if (decision == "Revoked" && revocationPeriodMonths.HasValue)
            RevocationEndsUtc = DateTime.UtcNow.AddMonths(revocationPeriodMonths.Value);

        HearingRecordRetainUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(3));
    }
}
