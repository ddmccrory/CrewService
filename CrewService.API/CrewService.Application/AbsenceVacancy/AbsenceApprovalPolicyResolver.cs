using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Policies;

namespace CrewService.Application.AbsenceVacancy;

public enum AbsenceApprovalLevel
{
    Automatic = 1,
    CallerManager = 2,
    ManagerOnly = 3
}

public sealed class StaticAbsenceApprovalPolicyResolver : IAbsenceApprovalPolicyResolver
{
    public Task<AbsenceApprovalPolicy> ResolveAsync(AbsenceCode absenceCode, CancellationToken ct = default)
    {
        var level = absenceCode.RequiresApproval
            ? AbsenceApprovalLevel.CallerManager
            : AbsenceApprovalLevel.Automatic;

        return Task.FromResult(AbsenceApprovalPolicy.ForLevel(level));
    }
}

public sealed record AbsenceApprovalPolicy(
    AbsenceApprovalLevel Level,
    string Description,
    bool AutoMarkOffIfWithinHoursEnabled,
    int AutoMarkOffIfWithinHours)
{
    public static AbsenceApprovalPolicy ForLevel(
        AbsenceApprovalLevel level,
        bool autoMarkOffIfWithinHoursEnabled = false,
        int autoMarkOffIfWithinHours = 0)
    {
        return level switch
        {
            AbsenceApprovalLevel.Automatic => new AbsenceApprovalPolicy(level, "Automatic approval (System)", autoMarkOffIfWithinHoursEnabled, autoMarkOffIfWithinHours),
            AbsenceApprovalLevel.CallerManager => new AbsenceApprovalPolicy(level, "Caller or Manager approval required", autoMarkOffIfWithinHoursEnabled, autoMarkOffIfWithinHours),
            AbsenceApprovalLevel.ManagerOnly => new AbsenceApprovalPolicy(level, "Manager approval required", autoMarkOffIfWithinHoursEnabled, autoMarkOffIfWithinHours),
            _ => new AbsenceApprovalPolicy(AbsenceApprovalLevel.CallerManager, "Caller or Manager approval required", autoMarkOffIfWithinHoursEnabled, autoMarkOffIfWithinHours)
        };
    }
}

public interface IAbsenceApprovalPolicyResolver
{
    Task<AbsenceApprovalPolicy> ResolveAsync(AbsenceCode absenceCode, CancellationToken ct = default);
}

public sealed class DbAbsenceApprovalPolicyResolver(IAbsenceApprovalPolicyRepository absenceApprovalPolicyRepository) : IAbsenceApprovalPolicyResolver
{
    public async Task<AbsenceApprovalPolicy> ResolveAsync(AbsenceCode absenceCode, CancellationToken ct = default)
    {
        if (!absenceCode.RequiresApproval)
            return AbsenceApprovalPolicy.ForLevel(AbsenceApprovalLevel.Automatic);

        var policy = await absenceApprovalPolicyRepository.GetByRailroadAsync(absenceCode.RailroadCtrlNbr);

        if (policy is null || !policy.IsEnabled)
            return GetFallbackPolicy(absenceCode);

        var level = ParseLevel(policy.ApprovalLevel);
        return AbsenceApprovalPolicy.ForLevel(
            level,
            policy.AutoMarkOffIfWithinHoursEnabled,
            policy.AutoMarkOffIfWithinHours);
    }

    private static AbsenceApprovalPolicy GetFallbackPolicy(AbsenceCode absenceCode)
    {
        var level = absenceCode.RequiresApproval
            ? AbsenceApprovalLevel.CallerManager
            : AbsenceApprovalLevel.Automatic;

        return AbsenceApprovalPolicy.ForLevel(level);
    }

    private static AbsenceApprovalLevel ParseLevel(string? approvalLevel)
    {
        if (string.Equals(approvalLevel, AbsenceApprovalPolicyLevel.Automatic, StringComparison.OrdinalIgnoreCase))
            return AbsenceApprovalLevel.Automatic;

        if (string.Equals(approvalLevel, AbsenceApprovalPolicyLevel.ManagerOnly, StringComparison.OrdinalIgnoreCase))
            return AbsenceApprovalLevel.ManagerOnly;

        return AbsenceApprovalLevel.CallerManager;
    }
}