using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Payroll;

public sealed record EarningContext(
    bool IsOffDay,
    bool IsHoliday,
    bool IsOvertime,
    string? AbsenceCode,
    string? CraftRoleCode);

public sealed record EarningCodeResult(string ResultCode, bool RequiresApproval);

public sealed class EarningCodeResolver(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<EarningCodeResult?> ResolveAsync(
        ControlNumber workAreaGroupCtrlNbr, EarningContext ctx, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var rules = await uow.EarningCodeRules.GetActiveByWorkAreaAsync(workAreaGroupCtrlNbr, ct);

        foreach (var rule in rules.OrderBy(r => r.Priority))
        {
            if (Matches(rule, ctx))
                return new EarningCodeResult(rule.ResultCode, rule.RequiresApproval);
        }

        return null;
    }

    private static bool Matches(EarningCodeRule rule, EarningContext ctx)
    {
        if (string.IsNullOrEmpty(rule.ConditionsJson)) return true;

        var conditions = rule.ConditionsJson;
        if (conditions.Contains("IsOffDay=true", StringComparison.OrdinalIgnoreCase) && !ctx.IsOffDay) return false;
        if (conditions.Contains("IsOffDay=false", StringComparison.OrdinalIgnoreCase) && ctx.IsOffDay) return false;
        if (conditions.Contains("IsHoliday=true", StringComparison.OrdinalIgnoreCase) && !ctx.IsHoliday) return false;
        if (conditions.Contains("IsHoliday=false", StringComparison.OrdinalIgnoreCase) && ctx.IsHoliday) return false;
        if (conditions.Contains("IsOvertime=true", StringComparison.OrdinalIgnoreCase) && !ctx.IsOvertime) return false;
        if (conditions.Contains("IsOvertime=false", StringComparison.OrdinalIgnoreCase) && ctx.IsOvertime) return false;

        return true;
    }
}
