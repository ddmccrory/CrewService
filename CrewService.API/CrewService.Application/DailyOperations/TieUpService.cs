using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed class TieUpService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<OffDutyRecord> ExecuteAsync(
        ControlNumber onDutyRecordCtrlNbr,
        DateTime offDutyTimeUtc,
        string releaseReason,
        ControlNumber craftCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var onDutyRecord = await uow.OnDutyRecords.GetByCtrlNbrAsync(onDutyRecordCtrlNbr, ct)
            ?? throw new InvalidOperationException("On-duty record not found");

        var policy = await uow.CraftOperationsPolicies.GetByCraftAsync(craftCtrlNbr, ct);

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
        await uow.OffDutyRecords.AddAsync(offDutyRecord, ct);
        await uow.CommitAsync(ct);
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

