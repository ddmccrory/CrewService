using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class VoluntaryReferral : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public DateTime ReferralDate { get; private set; }
    public DateTime? SapEvaluationDate { get; private set; }
    public DateTime? TreatmentCompletedDate { get; private set; }
    public DateTime? ReturnToDutyTestDate { get; private set; }
    public string? ReturnToDutyResult { get; private set; }
    public int FollowUpTestsRequired { get; private set; }
    public DateTime? FollowUpEndDate { get; private set; }
    public string Status { get; private set; } = "Referred";

    private VoluntaryReferral()
    {
        EmployeeCtrlNbr = null!;
    }

    public static VoluntaryReferral Create(ControlNumber employeeCtrlNbr)
    {
        return new VoluntaryReferral
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            ReferralDate = DateTime.UtcNow,
            FollowUpTestsRequired = 6,
            Status = "Referred",
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }

    public void RecordSapEvaluation(DateTime date)
    {
        SapEvaluationDate = date;
        Status = "InTreatment";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void CompleteTreatment(DateTime date)
    {
        TreatmentCompletedDate = date;
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void RecordReturnToDutyTest(DateTime date, string result)
    {
        ReturnToDutyTestDate = date;
        ReturnToDutyResult = result;

        if (result == "Negative")
        {
            Status = "FollowUp";
            FollowUpEndDate = date.AddMonths(60);
        }

        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void Complete()
    {
        Status = "Completed";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }
}
