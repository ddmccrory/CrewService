using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.FraCompliance;

public class VoluntaryReferralTests
{
    [Fact]
    public void Create_DefaultsToReferred()
    {
        var referral = VoluntaryReferral.Create(ControlNumber.Create(100));

        Assert.Equal("Referred", referral.Status);
        Assert.Equal(6, referral.FollowUpTestsRequired);
    }

    [Fact]
    public void RecordSapEvaluation_TransitionsToInTreatment()
    {
        var referral = VoluntaryReferral.Create(ControlNumber.Create(100));

        referral.RecordSapEvaluation(DateTime.UtcNow);

        Assert.Equal("InTreatment", referral.Status);
        Assert.NotNull(referral.SapEvaluationDate);
    }

    [Fact]
    public void RecordReturnToDutyTest_Negative_TransitionsToFollowUp()
    {
        var referral = VoluntaryReferral.Create(ControlNumber.Create(100));
        referral.RecordSapEvaluation(DateTime.UtcNow);
        referral.CompleteTreatment(DateTime.UtcNow);
        var testDate = DateTime.UtcNow;

        referral.RecordReturnToDutyTest(testDate, "Negative");

        Assert.Equal("FollowUp", referral.Status);
        Assert.NotNull(referral.FollowUpEndDate);
    }

    [Fact]
    public void Complete_SetsCompleted()
    {
        var referral = VoluntaryReferral.Create(ControlNumber.Create(100));

        referral.Complete();

        Assert.Equal("Completed", referral.Status);
    }
}

public class CertificationRevocationRecordTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var record = CertificationRevocationRecord.Create(
            ControlNumber.Create(1), "OperatingViolation", DateTime.UtcNow);

        Assert.Equal("OperatingViolation", record.ViolationType);
        Assert.Null(record.Decision);
    }

    [Fact]
    public void FullLifecycle_RecordNotice_ScheduleHearing_RecordHearing_Decide()
    {
        var record = CertificationRevocationRecord.Create(
            ControlNumber.Create(1), "OperatingViolation", DateTime.UtcNow);

        record.RecordWrittenNotice();
        Assert.NotNull(record.WrittenNoticeAtUtc);

        var hearingDate = DateTime.UtcNow.AddDays(30);
        record.ScheduleHearing(hearingDate);
        Assert.Equal(hearingDate, record.HearingScheduledUtc);

        record.RecordHearing(DateTime.UtcNow, ControlNumber.Create(200));
        Assert.NotNull(record.HearingHeldUtc);

        record.Decide("Revoked", 12);
        Assert.Equal("Revoked", record.Decision);
        Assert.Equal(12, record.RevocationPeriodMonths);
        Assert.NotNull(record.RevocationEndsUtc);
        Assert.NotNull(record.HearingRecordRetainUntil);
    }
}
