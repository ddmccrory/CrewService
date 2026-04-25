using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Payroll;

public sealed class PayrollPeriodService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<PayrollRun> CreateOrGetDraftAsync(
        string payPeriod, ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.PayrollRuns.GetByPayPeriodAsync(payPeriod, workAreaGroupCtrlNbr, ct);
        if (existing is not null && existing.Status == "DRAFT")
            return existing;

        var run = PayrollRun.Create(payPeriod);
        await uow.PayrollRuns.AddAsync(run, ct);
        await uow.CommitAsync(ct);
        return run;
    }

    public async Task<PayrollRun> CalculateTrialAsync(ControlNumber runCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var run = await uow.PayrollRuns.GetByCtrlNbrAsync(runCtrlNbr, ct)
            ?? throw new InvalidOperationException("Payroll run not found");
        run.MarkCalculated();
        await uow.PayrollRuns.UpdateAsync(run, ct);
        await uow.CommitAsync(ct);
        return run;
    }

    public async Task<PayrollRun> LockFinalAsync(ControlNumber runCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var run = await uow.PayrollRuns.GetByCtrlNbrAsync(runCtrlNbr, ct)
            ?? throw new InvalidOperationException("Payroll run not found");
        run.Lock();
        await uow.PayrollRuns.UpdateAsync(run, ct);
        await uow.CommitAsync(ct);
        return run;
    }
}
