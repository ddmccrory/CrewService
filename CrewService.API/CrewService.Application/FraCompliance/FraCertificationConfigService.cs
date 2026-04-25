using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.FraCompliance;

/// <summary>
/// Application service for FRA certification config CRUD and config-aware lookups.
/// </summary>
public sealed class FraCertificationConfigService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<FraCertificationConfig> GetOrDefaultAsync(
        ControlNumber parentCtrlNbr, ControlNumber? railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        FraCertificationConfig? config = null;
        if (railroadCtrlNbr is not null)
            config = await uow.FraCertificationConfigs.GetByRailroadAsync(railroadCtrlNbr, ct);
        config ??= await uow.FraCertificationConfigs.GetByParentAsync(parentCtrlNbr, ct);
        config ??= FraCertificationConfig.Create(parentCtrlNbr, null);
        return config;
    }

    public async Task<FraCertificationConfig> UpsertAsync(
        ControlNumber parentCtrlNbr, ControlNumber? railroadCtrlNbr,
        int certCycleMonths, int recertWindowDays, int renewWindowDays, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        FraCertificationConfig? existing = railroadCtrlNbr is not null
            ? await uow.FraCertificationConfigs.GetByRailroadAsync(railroadCtrlNbr, ct)
            : await uow.FraCertificationConfigs.GetByParentAsync(parentCtrlNbr, ct);

        if (existing is null)
        {
            existing = FraCertificationConfig.Create(parentCtrlNbr, railroadCtrlNbr, certCycleMonths, recertWindowDays, renewWindowDays);
            await uow.FraCertificationConfigs.AddAsync(existing, ct);
        }
        else
        {
            existing.Update(certCycleMonths, recertWindowDays, renewWindowDays);
            await uow.FraCertificationConfigs.UpdateAsync(existing, ct);
        }
        await uow.CommitAsync(ct);
        return existing;
    }

    public async Task<int> GetCertCycleMonthsAsync(ControlNumber? parentCtrlNbr, CancellationToken ct = default)
    {
        if (parentCtrlNbr is null) return 36;
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var config = await uow.FraCertificationConfigs.GetByParentAsync(parentCtrlNbr, ct);
        return config?.CertCycleMonths ?? 36;
    }

    public async Task<IReadOnlyList<FraCertificationCheckConfig>> GetCheckConfigsOrDefaultAsync(
        ControlNumber parentCtrlNbr, ControlNumber? railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        IReadOnlyList<FraCertificationCheckConfig> configs = railroadCtrlNbr is not null
            ? await uow.FraCertificationCheckConfigs.GetByRailroadAsync(railroadCtrlNbr, ct)
            : await uow.FraCertificationCheckConfigs.GetByParentAsync(parentCtrlNbr, ct);

        if (configs.Count == 0)
            configs = await uow.FraCertificationCheckConfigs.GetByParentAsync(parentCtrlNbr, ct);

        if (configs.Count == 0)
            configs = CertificationCheckDefaults.Checks
                .Select(d => FraCertificationCheckConfig.Create(
                    parentCtrlNbr, null, d.CheckType, d.StalenessLimitDays, d.IsEnforced, d.IsEnforcementLocked))
                .ToList();

        return configs;
    }

    public async Task<FraCertificationCheckConfig> UpsertCheckConfigAsync(
        ControlNumber parentCtrlNbr, ControlNumber? railroadCtrlNbr, string checkType,
        int stalenessLimitDays, bool isEnforced, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        IReadOnlyList<FraCertificationCheckConfig> existing = railroadCtrlNbr is not null
            ? await uow.FraCertificationCheckConfigs.GetByRailroadAsync(railroadCtrlNbr, ct)
            : await uow.FraCertificationCheckConfigs.GetByParentAsync(parentCtrlNbr, ct);

        var row = existing.FirstOrDefault(c => string.Equals(c.CheckType, checkType, StringComparison.OrdinalIgnoreCase));

        if (row is null)
        {
            var defaultEntry = CertificationCheckDefaults.Checks
                .FirstOrDefault(d => string.Equals(d.CheckType, checkType, StringComparison.OrdinalIgnoreCase));
            row = FraCertificationCheckConfig.Create(parentCtrlNbr, railroadCtrlNbr, checkType, stalenessLimitDays, isEnforced,
                defaultEntry.CheckType is not null && defaultEntry.IsEnforcementLocked);
            await uow.FraCertificationCheckConfigs.AddAsync(row, ct);
        }
        else
        {
            row.Update(stalenessLimitDays, isEnforced);
            await uow.FraCertificationCheckConfigs.UpdateAsync(row, ct);
        }
        await uow.CommitAsync(ct);
        return row;
    }

    public async Task<int> GetStalenessLimitDaysAsync(string checkType, ControlNumber? parentCtrlNbr, CancellationToken ct = default)
    {
        if (parentCtrlNbr is not null)
        {
            await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
            var configs = await uow.FraCertificationCheckConfigs.GetByParentAsync(parentCtrlNbr, ct);
            var match = configs.FirstOrDefault(c => string.Equals(c.CheckType, checkType, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match.StalenessLimitDays;
        }
        var defaultEntry = CertificationCheckDefaults.Checks
            .FirstOrDefault(d => string.Equals(d.CheckType, checkType, StringComparison.OrdinalIgnoreCase));
        return defaultEntry.CheckType is not null ? defaultEntry.StalenessLimitDays : 365;
    }
}
