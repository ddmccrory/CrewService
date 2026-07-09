using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Safety;

public sealed class SafetyService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<SafetyObservation> CreateObservationAsync(
        long workAreaGroupCtrlNbr, long observerEmployeeCtrlNbr,
        string categoryCode, string areaCode, string description, string? subdivisionCode,
        CancellationToken ct = default)
    {
        var obs = SafetyObservation.Create(
            workAreaGroupCtrlNbr, observerEmployeeCtrlNbr,
            categoryCode, areaCode, description, subdivisionCode);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.SafetyObservations.Add(obs);
        await uow.CommitAsync(ct);
        return obs;
    }

    public async Task<SafetyObservation> GetObservationAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SafetyObservations.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Safety observation {ctrlNbr} not found.");
    }

    public async Task<IReadOnlyList<SafetyObservation>> GetByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, bool openOnly, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return openOnly
            ? await uow.SafetyObservations.GetOpenByWorkAreaAsync(workAreaGroupCtrlNbr, ct)
            : await uow.SafetyObservations.GetByWorkAreaAsync(workAreaGroupCtrlNbr, ct);
    }

    public async Task<SafetyObservationAction> AddActionAsync(
        ControlNumber observationCtrlNbr, ControlNumber takenByCtrlNbr, string actionDescription,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var obs = await uow.SafetyObservations.GetByCtrlNbrAsync(observationCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Safety observation {observationCtrlNbr} not found.");
        var action = obs.AddAction(takenByCtrlNbr, actionDescription);
        uow.SafetyObservations.Update(obs);
        await uow.CommitAsync(ct);
        return action;
    }

    public async Task<SafetyObservationResolution> ResolveObservationAsync(
        ControlNumber observationCtrlNbr, ControlNumber resolvedByCtrlNbr,
        string resolutionDescription, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var obs = await uow.SafetyObservations.GetByCtrlNbrAsync(observationCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Safety observation {observationCtrlNbr} not found.");
        var resolution = obs.Resolve(resolvedByCtrlNbr, resolutionDescription);
        uow.SafetyResolutions.Add(resolution);
        uow.SafetyObservations.Update(obs);
        await uow.CommitAsync(ct);
        return resolution;
    }

    public async Task<IReadOnlyList<SafetyCategory>> GetCategoriesAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SafetyCategories.GetByWorkAreaAsync(workAreaGroupCtrlNbr, ct);
    }

    public async Task<SafetyCategory> CreateCategoryAsync(
        long workAreaGroupCtrlNbr, string code, string displayName, CancellationToken ct = default)
    {
        var cat = SafetyCategory.Create(workAreaGroupCtrlNbr, code, displayName);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.SafetyCategories.Add(cat);
        await uow.CommitAsync(ct);
        return cat;
    }
}
