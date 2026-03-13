using System.Globalization;

namespace CrewService.Application.ReportingExports.Formatters;

public sealed class AdpExportFormatter : IPayrollExportFormatter
{
    public string FormatCode => "ADP";
    public string FileExtension => ".csv";

    public string FormatHeader() =>
        "CO_CODE,BATCH_ID,FILE_NUM,EARN_CODE,HOURS,AMOUNT,PAY_PERIOD";

    public string FormatRow(PayrollExportRow row) =>
        string.Join(",",
            "001",
            "CREW",
            row.EmployeeCtrlNbr.ToString(CultureInfo.InvariantCulture),
            row.ResolvedEarningCode ?? row.EarningsType,
            row.Hours.ToString("F2", CultureInfo.InvariantCulture),
            row.Amount.ToString("F2", CultureInfo.InvariantCulture),
            row.PayPeriod);
}

public sealed class UkgExportFormatter : IPayrollExportFormatter
{
    public string FormatCode => "UKG";
    public string FileExtension => ".csv";

    public string FormatHeader() =>
        "EmployeeID,EarningCode,Hours,Rate,Amount,PeriodEnd";

    public string FormatRow(PayrollExportRow row) =>
        string.Join(",",
            row.EmployeeCtrlNbr.ToString(CultureInfo.InvariantCulture),
            row.ResolvedEarningCode ?? row.EarningsType,
            row.Hours.ToString("F2", CultureInfo.InvariantCulture),
            row.Hours > 0 ? (row.Amount / row.Hours).ToString("F4", CultureInfo.InvariantCulture) : "0.0000",
            row.Amount.ToString("F2", CultureInfo.InvariantCulture),
            row.PayPeriod);
}
