using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Payroll;

public interface IHolidayRepository
{
    Task<IReadOnlyList<Holiday>> GetActiveByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
}

public interface IHolidayQualificationRuleRepository
{
    Task<IReadOnlyList<HolidayQualificationRule>> GetByHolidayAsync(ControlNumber holidayCtrlNbr, CancellationToken ct = default);
}

public sealed record HolidayQualificationContext(
    ControlNumber EmployeeCtrlNbr,
    bool WorkedDayBefore,
    bool WorkedDayAfter,
    string? AbsenceCodeDayBefore,
    string? AbsenceCodeDayAfter);

public sealed record HolidayQualificationResult(
    bool IsQualified,
    string? DisqualificationReason = null);

public sealed class HolidayQualificationService(IHolidayQualificationRuleRepository ruleRepo)
{
    public async Task<HolidayQualificationResult> EvaluateAsync(
        ControlNumber holidayCtrlNbr, HolidayQualificationContext ctx, CancellationToken ct = default)
    {
        var rules = await ruleRepo.GetByHolidayAsync(holidayCtrlNbr, ct);
        if (rules.Count == 0)
            return new HolidayQualificationResult(true);

        foreach (var rule in rules)
        {
            if (rule.RequireWorkDayBefore && !ctx.WorkedDayBefore)
            {
                if (!IsExempt(rule.ExemptAbsenceCodes, ctx.AbsenceCodeDayBefore))
                    return new HolidayQualificationResult(false, "Did not work day before");
            }

            if (rule.RequireWorkDayAfter && !ctx.WorkedDayAfter)
            {
                if (!IsExempt(rule.ExemptAbsenceCodes, ctx.AbsenceCodeDayAfter))
                    return new HolidayQualificationResult(false, "Did not work day after");
            }
        }

        return new HolidayQualificationResult(true);
    }

    private static bool IsExempt(string? exemptCodes, string? absenceCode)
    {
        if (string.IsNullOrEmpty(exemptCodes) || string.IsNullOrEmpty(absenceCode))
            return false;

        return exemptCodes.Contains(absenceCode, StringComparison.OrdinalIgnoreCase);
    }
}
