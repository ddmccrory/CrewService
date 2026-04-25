using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.ReportingExports;

public sealed record PayrollImportRow(
    long EmployeeCtrlNbr,
    decimal PaidAmount,
    string PayPeriod);

public sealed class PayrollImportService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<IReadOnlyList<PayrollImportRecord>> ImportAsync(
        string sourceFile,
        IReadOnlyList<PayrollImportRow> rows,
        string payPeriod,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var run = await uow.PayrollRuns.GetByPayPeriodAsync(payPeriod);
        List<PayrollRecord>? runRecords = null;
        if (run is not null)
            runRecords = await uow.PayrollRecords.GetByRunAsync(run.CtrlNbr);

        var importRecords = new List<PayrollImportRecord>();

        foreach (var row in rows)
        {
            var empCtrl = ControlNumber.Create(row.EmployeeCtrlNbr);
            var record = PayrollImportRecord.Create(sourceFile, empCtrl, row.PaidAmount);

            if (runRecords is not null)
            {
                var match = runRecords.FirstOrDefault(r =>
                    r.EmployeeCtrlNbr == empCtrl
                    && Math.Abs(r.Amount - row.PaidAmount) < 0.01m);

                if (match is not null)
                    record.MatchToRecord(match.CtrlNbr);
            }

            await uow.PayrollImportRecords.AddAsync(record, ct);
            importRecords.Add(record);
        }

        await uow.CommitAsync(ct);
        return importRecords;
    }
}

