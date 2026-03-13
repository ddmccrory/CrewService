using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Payroll;

public sealed class PayrollExportBatch : Entity
{
    public ControlNumber PayrollRunCtrlNbr { get; private set; }
    public string ExportFormat { get; private set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; private set; }
    public int RecordCount { get; private set; }
    public string? FilePath { get; private set; }

    private PayrollExportBatch() { PayrollRunCtrlNbr = null!; }

    public static PayrollExportBatch Create(
        ControlNumber payrollRunCtrlNbr, string exportFormat, int recordCount, string? filePath)
    {
        var batch = new PayrollExportBatch
        {
            PayrollRunCtrlNbr = payrollRunCtrlNbr,
            ExportFormat = exportFormat,
            GeneratedAtUtc = DateTime.UtcNow,
            RecordCount = recordCount,
            FilePath = filePath,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
        batch.Raise(new PayrollExportGeneratedDomainEvent(batch));
        return batch;
    }
}

public sealed record PayrollExportGeneratedDomainEvent : DomainEvent
{
    public PayrollExportGeneratedDomainEvent(PayrollExportBatch batch)
        : base(nameof(PayrollExportBatch), batch.CtrlNbr.Value,
            new { RunCtrlNbr = batch.PayrollRunCtrlNbr.Value, batch.ExportFormat, batch.RecordCount }) { }
}
