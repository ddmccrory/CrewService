using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Absence;

public interface IAbsenceCodeRepository : IRepository<AbsenceCode>
{
    Task<List<AbsenceCode>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default);
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

public sealed class AbsenceCodeService(IAbsenceCodeRepository absenceCodeRepo)
{
    public async Task<List<AbsenceCode>> GetCodesAsync(ControlNumber railroadCtrlNbr, bool activeOnly, CancellationToken ct = default)
    {
        var codes = await absenceCodeRepo.GetByRailroadAsync(railroadCtrlNbr, ct);

        if (activeOnly)
            codes = codes.Where(c => c.IsActive).ToList();

        return codes
            .OrderBy(c => c.Code)
            .ThenBy(c => c.Description)
            .ToList();
    }

    public async Task<AbsenceCode> CreateCodeAsync(
        ControlNumber railroadCtrlNbr,
        string code,
        string description,
        bool isExcused,
        bool isCompensated,
        bool requiresApproval,
        bool isSystemOnly,
        bool isHolidayExempt,
        decimal? defaultAutoMarkUpHours,
        bool isActive,
        CancellationToken ct = default)
    {
        var normalizedCode = NormalizeCode(code);
        var normalizedDescription = NormalizeDescription(description);

        var existingCodes = await absenceCodeRepo.GetByRailroadAsync(railroadCtrlNbr, ct);
        if (existingCodes.Any(c => string.Equals(c.Code, normalizedCode, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A mark-off code with code '{normalizedCode}' already exists.");

        var entity = AbsenceCode.Create(
            railroadCtrlNbr.Value,
            normalizedCode,
            normalizedDescription,
            isExcused,
            isCompensated,
            requiresApproval,
            isSystemOnly,
            isHolidayExempt,
            defaultAutoMarkUpHours,
            isActive);

        await absenceCodeRepo.AddAsync(entity, ct);

        return entity;
    }

    public async Task<AbsenceCode> UpdateCodeAsync(
        ControlNumber railroadCtrlNbr,
        ControlNumber ctrlNbr,
        string code,
        string description,
        bool isExcused,
        bool isCompensated,
        bool requiresApproval,
        bool isSystemOnly,
        bool isHolidayExempt,
        decimal? defaultAutoMarkUpHours,
        bool isActive,
        CancellationToken ct = default)
    {
        var entity = await absenceCodeRepo.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Mark-off code {ctrlNbr.Value} not found.");

        if (entity.RailroadCtrlNbr != railroadCtrlNbr)
            throw new KeyNotFoundException($"Mark-off code {ctrlNbr.Value} not found.");

        var normalizedCode = NormalizeCode(code);
        var normalizedDescription = NormalizeDescription(description);

        var existingCodes = await absenceCodeRepo.GetByRailroadAsync(railroadCtrlNbr, ct);
        if (existingCodes.Any(c => c.CtrlNbr != ctrlNbr
            && string.Equals(c.Code, normalizedCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A mark-off code with code '{normalizedCode}' already exists.");
        }

        entity.Update(
            code: normalizedCode,
            description: normalizedDescription,
            isExcused: isExcused,
            isCompensated: isCompensated,
            requiresApproval: requiresApproval,
            isSystemOnly: isSystemOnly,
            isHolidayExempt: isHolidayExempt,
            defaultAutoMarkUpHours: defaultAutoMarkUpHours,
            isActive: isActive);

        await absenceCodeRepo.UpdateAsync(entity, ct);

        return entity;
    }

    public async Task DeleteCodeAsync(ControlNumber railroadCtrlNbr, ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        var entity = await absenceCodeRepo.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Mark-off code {ctrlNbr.Value} not found.");

        if (entity.RailroadCtrlNbr != railroadCtrlNbr)
            throw new KeyNotFoundException($"Mark-off code {ctrlNbr.Value} not found.");

        await absenceCodeRepo.DeleteAsync(entity.CtrlNbr, ct);
    }

    private static string NormalizeCode(string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("Code is required.");

        return normalized;
    }

    private static string NormalizeDescription(string description)
    {
        var normalized = (description ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("Description is required.");

        return normalized;
    }
}
