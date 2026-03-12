using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public interface ICraftOperationsPolicyRepository
{
    Task<CraftOperationsPolicy?> GetByCraftAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default);
}

public sealed class TieUpService(
    IOnDutyRecordRepository onDutyRepo,
    IOffDutyRecordRepository offDutyRepo,
    ICraftOperationsPolicyRepository policyRepo)
{
    public async Task<OffDutyRecord> ExecuteAsync(
        ControlNumber onDutyRecordCtrlNbr,
        DateTime offDutyTimeUtc,
        string releaseReason,
        ControlNumber craftCtrlNbr,
        CancellationToken ct = default)
    {
        var onDutyRecord = await onDutyRepo.GetByCtrlNbrAsync(onDutyRecordCtrlNbr, ct)
            ?? throw new InvalidOperationException("On-duty record not found");

        var policy = await policyRepo.GetByCraftAsync(craftCtrlNbr, ct);

        var totalMinutes = (int)(offDutyTimeUtc - onDutyRecord.OnDutyTimeUtc).TotalMinutes;
        var restHours = CalculateRestHours(policy, totalMinutes);
        var consecutiveDayResetHours = policy?.ConsecutiveDayResetHours ?? 24m;

        var offDutyRecord = OffDutyRecord.Create(
            onDutyRecordCtrlNbr,
            onDutyRecord.EmployeeCtrlNbr,
            offDutyTimeUtc,
            totalMinutes,
            restHours,
            consecutiveDayResetHours,
            releaseReason);

        onDutyRecord.TieUp();
        await offDutyRepo.AddAsync(offDutyRecord, ct);
        return offDutyRecord;
    }

    private static decimal CalculateRestHours(CraftOperationsPolicy? policy, int totalMinutes)
    {
        if (policy is null) return 10m;

        return policy.RestCalculationStrategy switch
        {
            "FixedHours" => policy.FixedRestHours ?? 10m,
            "CraftConfigured" => CalculateCraftConfiguredRest(totalMinutes),
            _ => 10m // "FRA" — actual FRA rest calc handled by FraCompliance module
        };
    }

    private static decimal CalculateCraftConfiguredRest(int totalMinutes)
    {
        var baseRest = 10m;
        var excessMinutes = Math.Max(0, totalMinutes - 720);
        var penalty = excessMinutes > 0 ? Math.Ceiling(excessMinutes / 60m) : 0;
        return baseRest + penalty;
    }
}
