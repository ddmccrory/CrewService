namespace CrewService.Application.ReportingExports;

public sealed record PayrollExportRow(
    long EmployeeCtrlNbr,
    string EarningsType,
    string? ResolvedEarningCode,
    decimal Amount,
    decimal Hours,
    string PayPeriod);

public interface IPayrollExportFormatter
{
    string FormatCode { get; }
    string FileExtension { get; }
    string FormatHeader();
    string FormatRow(PayrollExportRow row);
}
