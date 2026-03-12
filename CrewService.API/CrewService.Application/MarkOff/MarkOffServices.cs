using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.MarkOff;

public interface IAbsenceCodeRepository
{
    Task<AbsenceCode?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task<AbsenceCodeCraftOverride?> GetOverrideAsync(ControlNumber absenceCodeCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default);
}

public interface ICompensationBalanceRepository
{
    Task<CompensationBalance?> GetAsync(ControlNumber employeeCtrlNbr, string compensationType, CancellationToken ct = default);
    Task AddAsync(CompensationBalance balance, CancellationToken ct = default);
}

public sealed class AutoMarkUpService(IAbsenceCodeRepository absenceCodeRepo)
{
    public async Task<decimal?> ResolveMarkUpHoursAsync(
        ControlNumber absenceCodeCtrlNbr,
        ControlNumber? craftCtrlNbr,
        CancellationToken ct = default)
    {
        var code = await absenceCodeRepo.GetByCtrlNbrAsync(absenceCodeCtrlNbr, ct);
        if (code is null || code.DefaultAutoMarkUpHours is null)
            return null;

        if (craftCtrlNbr is not null)
        {
            var craftOverride = await absenceCodeRepo.GetOverrideAsync(absenceCodeCtrlNbr, craftCtrlNbr, ct);
            if (craftOverride is not null)
                return craftOverride.OverrideAutoMarkUpHours;
        }

        return code.DefaultAutoMarkUpHours;
    }
}

public sealed class CompensationBalanceService(ICompensationBalanceRepository balanceRepo)
{
    public async Task<bool> DebitAsync(
        ControlNumber employeeCtrlNbr, string compensationType,
        decimal hours, CancellationToken ct = default)
    {
        var balance = await balanceRepo.GetAsync(employeeCtrlNbr, compensationType, ct);
        if (balance is null) return false;
        return balance.Debit(hours);
    }

    public async Task CreditAsync(
        ControlNumber employeeCtrlNbr, string compensationType,
        decimal hours, CancellationToken ct = default)
    {
        var balance = await balanceRepo.GetAsync(employeeCtrlNbr, compensationType, ct);
        if (balance is null) return;
        balance.Credit(hours);
    }
}
