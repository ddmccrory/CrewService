using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using System.Text;

namespace CrewService.Application.ReportingExports;

public sealed class PayrollExportService(
    IPayrollRunRepository runRepo,
    IPayrollRecordRepository recordRepo,
    IPayrollExportBatchRepository batchRepo,
    IEnumerable<IPayrollExportFormatter> formatters)
{
    public async Task<PayrollExportBatch> ExportAsync(
        ControlNumber payrollRunCtrlNbr, string formatCode, CancellationToken ct = default)
    {
        var formatter = formatters.FirstOrDefault(f =>
            f.FormatCode.Equals(formatCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Unknown export format: {formatCode}");

        var run = await runRepo.GetByCtrlNbrAsync(payrollRunCtrlNbr, ct)
            ?? throw new InvalidOperationException($"PayrollRun {payrollRunCtrlNbr.Value} not found.");

        if (run.Status != "LOCKED")
            throw new InvalidOperationException("PayrollRun must be locked before exporting.");

        var records = await recordRepo.GetByRunAsync(payrollRunCtrlNbr);

        var sb = new StringBuilder();
        sb.AppendLine(formatter.FormatHeader());

        foreach (var record in records)
        {
            var row = new PayrollExportRow(
                record.EmployeeCtrlNbr.Value,
                record.EarningsType,
                record.ResolvedEarningCode,
                record.Amount,
                record.Hours,
                run.PayPeriod);

            sb.AppendLine(formatter.FormatRow(row));
        }

        var filePath = $"exports/payroll_{run.PayPeriod}_{formatCode.ToLowerInvariant()}{formatter.FileExtension}";

        var batch = PayrollExportBatch.Create(payrollRunCtrlNbr, formatCode, records.Count, filePath);
        await batchRepo.AddAsync(batch, ct);

        return batch;
    }
}
