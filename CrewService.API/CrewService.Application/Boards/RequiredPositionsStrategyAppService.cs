using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Boards;

public class RequiredPositionsStrategyAppService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<List<RequiredPositionsStrategy>> GetAllAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.RequiredPositionsStrategies.GetAllSystemStrategiesAsync(ct);
    }

    public async Task<RequiredPositionsStrategy> GetAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.RequiredPositionsStrategies.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"RequiredPositionsStrategy {ctrlNbr.Value} not found.");
    }

    public async Task<RequiredPositionsStrategy> CreateAsync(
        string code, string name, string description,
        string formulaType, string parametersJson,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var strategy = RequiredPositionsStrategy.Create(code, name, description, formulaType, parametersJson);
        uow.RequiredPositionsStrategies.Add(strategy);
        await uow.CommitAsync(ct);
        return strategy;
    }

    public async Task<RequiredPositionsStrategy> UpdateAsync(
        ControlNumber ctrlNbr, string name, string description,
        string formulaType, string parametersJson,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var strategy = await uow.RequiredPositionsStrategies.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"RequiredPositionsStrategy {ctrlNbr.Value} not found.");
        strategy.Update(name, description, formulaType, parametersJson);
        uow.RequiredPositionsStrategies.Update(strategy);
        await uow.CommitAsync(ct);
        return strategy;
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var strategy = await uow.RequiredPositionsStrategies.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"RequiredPositionsStrategy {ctrlNbr.Value} not found.");
        uow.RequiredPositionsStrategies.Remove(strategy);
        await uow.CommitAsync(ct);
    }

    public async Task<CraftRequiredPositionsStrategy> AssignToCraftAsync(
        ControlNumber craftCtrlNbr, ControlNumber strategyCtrlNbr, string? parametersJson,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        _ = await uow.RequiredPositionsStrategies.GetByCtrlNbrAsync(strategyCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"RequiredPositionsStrategy {strategyCtrlNbr.Value} not found.");

        var existing = await uow.CraftRequiredPositionsStrategies.GetByCraftAsync(craftCtrlNbr, ct);
        if (existing is not null)
        {
            existing.Reassign(strategyCtrlNbr, parametersJson);
            uow.CraftRequiredPositionsStrategies.Update(existing);
            await uow.CommitAsync(ct);
            return existing;
        }

        var assignment = CraftRequiredPositionsStrategy.Create(craftCtrlNbr, strategyCtrlNbr, parametersJson);
        uow.CraftRequiredPositionsStrategies.Add(assignment);
        await uow.CommitAsync(ct);
        return assignment;
    }

    public async Task<CraftRequiredPositionsStrategy?> GetCraftStrategyAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.CraftRequiredPositionsStrategies.GetByCraftAsync(craftCtrlNbr, ct);
    }

    /// <summary>
    /// Returns all craft assignments for a railroad, with craft names included via dictionary lookup.
    /// </summary>
    public async Task<List<CraftRequiredPositionsStrategy>> GetCraftAssignmentsByRailroadAsync(
        ControlNumber railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var railroadGroups = await uow.DynamicGroups.GetByCtrlNbrsAsync([railroadCtrlNbr]);
        var railroad = railroadGroups.FirstOrDefault();
        var crafts = await uow.Crafts.GetByParentAndRailroadAsync(railroad?.ParentCtrlNbr, railroadCtrlNbr);
        if (crafts.Count == 0) return [];
        return await uow.CraftRequiredPositionsStrategies.GetByCraftsAsync(
            crafts.Select(c => c.CtrlNbr!), ct);
    }

    public async Task<Dictionary<long, string>> GetCraftNamesByCtrlNbrsAsync(
        IEnumerable<ControlNumber> craftCtrlNbrs, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var crafts = await uow.Crafts.GetByCtrlNbrsAsync(craftCtrlNbrs);
        return crafts.ToDictionary(c => c.CtrlNbr.Value, c => c.CraftName);
    }

    /// <summary>
    /// Returns all crafts for the railroad that are NOT already assigned to the given strategy.
    /// Crafts assigned to a different strategy ARE included (allowing reassignment).
    /// </summary>
    public async Task<List<Craft>> GetCraftsForAssignmentAsync(
        ControlNumber railroadCtrlNbr, ControlNumber strategyCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var railroadGroups = await uow.DynamicGroups.GetByCtrlNbrsAsync([railroadCtrlNbr]);
        var railroad = railroadGroups.FirstOrDefault();
        var allCrafts = await uow.Crafts.GetByParentAndRailroadAsync(railroad?.ParentCtrlNbr, railroadCtrlNbr);
        // Get craft ctrl nbrs already assigned to THIS specific strategy
        var assignmentsForThisStrategy = await uow.CraftRequiredPositionsStrategies.GetByStrategyCtrlNbrsAsync(
            [strategyCtrlNbr], ct);
        var alreadyOnThisStrategy = assignmentsForThisStrategy.Select(a => a.CraftCtrlNbr).ToHashSet();
        return allCrafts.Where(c => !alreadyOnThisStrategy.Contains(c.CtrlNbr)).ToList();
    }
}
