using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Payroll;

public sealed class PayrollTierAppService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<List<PayrollTier>> GetAllByGroupAsync(ControlNumber dynamicGroupCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PayrollTiers.GetByDynamicGroupCtrlNbrAsync(dynamicGroupCtrlNbr);
    }

    public async Task<PayrollTier> GetAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PayrollTiers.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"PayrollTier {ctrlNbr.Value} not found.");
    }

    public async Task<PayrollTier> CreateAsync(
        ControlNumber dynamicGroupCtrlNbr, int numberOfDays, int typeOfDay, int ratePercentage,
        CancellationToken ct = default)
    {
        var tier = PayrollTier.Create(dynamicGroupCtrlNbr, numberOfDays, typeOfDay, ratePercentage);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.PayrollTiers.Add(tier);
        await uow.CommitAsync(ct);
        return tier;
    }

    public async Task<PayrollTier> UpdateAsync(
        ControlNumber ctrlNbr, int numberOfDays, int typeOfDay, int ratePercentage,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var tier = await uow.PayrollTiers.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"PayrollTier {ctrlNbr.Value} not found.");
        tier.Update(numberOfDays, typeOfDay, ratePercentage);
        uow.PayrollTiers.Update(tier);
        await uow.CommitAsync(ct);
        return tier;
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var tier = await uow.PayrollTiers.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"PayrollTier {ctrlNbr.Value} not found.");
        uow.PayrollTiers.Remove(tier);
        await uow.CommitAsync(ct);
    }
}
