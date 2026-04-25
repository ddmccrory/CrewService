using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Payroll;

public sealed record HolidayQualificationContext(
    ControlNumber EmployeeCtrlNbr,
    bool WorkedDayBefore,
    bool WorkedDayAfter,
    string? AbsenceCodeDayBefore,
    string? AbsenceCodeDayAfter);

public sealed record HolidayQualificationResult(
    bool IsQualified,
    string? DisqualificationReason = null);

public sealed class HolidayQualificationService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<IReadOnlyList<Holiday>> GetActiveByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Holidays.GetActiveByWorkAreaAsync(workAreaGroupCtrlNbr, ct);
    }

    public async Task<HolidayQualificationResult> EvaluateAsync(
        ControlNumber holidayCtrlNbr, HolidayQualificationContext ctx, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var rules = await uow.HolidayQualificationRules.GetByHolidayAsync(holidayCtrlNbr, ct);
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
