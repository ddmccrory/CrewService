using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Payroll;

public interface IPayRateRepository
{
    Task<PayRate?> GetEffectiveAsync(
        ControlNumber craftCtrlNbr, DateTime asOfDate,
        ControlNumber? craftRoleCtrlNbr = null, CancellationToken ct = default);
}

public sealed class PayrollPeriodService(IPayrollRunRepository runRepo)
{
    public async Task<PayrollRun> CreateOrGetDraftAsync(
        string payPeriod, ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
    {
        var existing = await runRepo.GetByPayPeriodAsync(payPeriod, workAreaGroupCtrlNbr, ct);
        if (existing is not null && existing.Status == "DRAFT")
            return existing;

        var run = PayrollRun.Create(payPeriod);
        await runRepo.AddAsync(run, ct);
        return run;
    }

    public async Task<PayrollRun> CalculateTrialAsync(ControlNumber runCtrlNbr, CancellationToken ct = default)
    {
        var run = await runRepo.GetByCtrlNbrAsync(runCtrlNbr, ct)
            ?? throw new InvalidOperationException("Payroll run not found");

        run.MarkCalculated();
        return run;
    }

    public async Task<PayrollRun> LockFinalAsync(ControlNumber runCtrlNbr, CancellationToken ct = default)
    {
        var run = await runRepo.GetByCtrlNbrAsync(runCtrlNbr, ct)
            ?? throw new InvalidOperationException("Payroll run not found");

        run.Lock();
        return run;
    }
}
