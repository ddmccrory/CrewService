using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Payroll;

public sealed class PayrollImportRecord : Entity
{
    public string SourceFile { get; private set; } = string.Empty;
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber? PayrollRecordCtrlNbr { get; private set; }
    public decimal PaidAmount { get; private set; }
    public DateTime ImportedAtUtc { get; private set; }
    public string MatchStatus { get; private set; } = "Unmatched";

    private PayrollImportRecord() { EmployeeCtrlNbr = null!; }

    public static PayrollImportRecord Create(
        string sourceFile, ControlNumber employeeCtrlNbr, decimal paidAmount)
    {
        return new PayrollImportRecord
        {
            SourceFile = sourceFile,
            EmployeeCtrlNbr = employeeCtrlNbr,
            PaidAmount = paidAmount,
            ImportedAtUtc = DateTime.UtcNow,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }

    public void MatchToRecord(ControlNumber payrollRecordCtrlNbr)
    {
        PayrollRecordCtrlNbr = payrollRecordCtrlNbr;
        MatchStatus = "Matched";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void MarkError(string reason)
    {
        MatchStatus = "Error";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }
}

public sealed record PayrollImportCompletedDomainEvent : DomainEvent
{
    public PayrollImportCompletedDomainEvent(string sourceFile, int totalRecords, int matchedRecords)
        : base("PayrollImportRecord", 0,
            new { SourceFile = sourceFile, TotalRecords = totalRecords, MatchedRecords = matchedRecords }) { }
}
