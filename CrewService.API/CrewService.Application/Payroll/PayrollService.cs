using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Payroll;

public sealed class PayrollService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<IReadOnlyList<TimeEntry>> GetTimeEntriesAsync(
        ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.TimeEntries.GetByEmployeeAndPeriodAsync(employeeCtrlNbr, startUtc, endUtc);
    }

    public async Task<TimeEntry> CreateTimeEntryAsync(
        long employeeCtrlNbr, DateTime dateUtc, string entryType,
        decimal hours, string? reasonCode, string? notes, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var entry = TimeEntry.Create(employeeCtrlNbr, dateUtc, entryType, hours, reasonCode, notes);
        await uow.TimeEntries.AddAsync(entry, ct);
        await uow.CommitAsync(ct);
        return entry;
    }

    public async Task<PayrollRun> GetPayrollRunAsync(string payPeriod, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PayrollRuns.GetByPayPeriodAsync(payPeriod)
            ?? throw new KeyNotFoundException($"Payroll run for period {payPeriod} not found.");
    }

    public async Task<PayrollRun> CreatePayrollRunAsync(string payPeriod, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var run = PayrollRun.Create(payPeriod);
        await uow.PayrollRuns.AddAsync(run, ct);
        await uow.CommitAsync(ct);
        return run;
    }

    public async Task<PayrollRun> LockPayrollRunAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var run = await uow.PayrollRuns.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Payroll run {ctrlNbr} not found.");
        run.Lock();
        await uow.PayrollRuns.UpdateAsync(run, ct);
        await uow.CommitAsync(ct);
        return run;
    }

    public async Task<IReadOnlyList<PayrollRecord>> GetPayrollRecordsAsync(
        ControlNumber payrollRunCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PayrollRecords.GetByRunAsync(payrollRunCtrlNbr);
    }
}
