using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.SeniorityOps;

public sealed class SeniorityStateAppService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<List<SeniorityState>> GetAllAsync(
        ControlNumber? parentCtrlNbr = null, int pageNumber = 0, int pageSize = 0, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        if (parentCtrlNbr is not null)
            return await uow.SeniorityStates.GetByParentCtrlNbrAsync(parentCtrlNbr);
        if (pageSize > 0)
            return await uow.SeniorityStates.GetAllAsync(pageNumber, pageSize);
        return await uow.SeniorityStates.GetAllAsync();
    }

    public async Task<SeniorityState> GetAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SeniorityStates.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Seniority state {ctrlNbr.Value} not found.");
    }

    public async Task<SeniorityState> CreateAsync(
        string stateDescription, StateType stateType, long parentCtrlNbr, CancellationToken ct = default)
    {
        var state = SeniorityState.Create(stateDescription, stateType, parentCtrlNbr);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.SeniorityStates.Add(state);
        await uow.CommitAsync(ct);
        return state;
    }

    public async Task<SeniorityState> UpdateAsync(
        ControlNumber ctrlNbr, string stateDescription, StateType stateType, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var state = await uow.SeniorityStates.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Seniority state {ctrlNbr.Value} not found.");
        state.Update(stateDescription, stateType);
        uow.SeniorityStates.Update(state);
        await uow.CommitAsync(ct);
        return state;
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var state = await uow.SeniorityStates.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Seniority state {ctrlNbr.Value} not found.");
        uow.SeniorityStates.Remove(state);
        await uow.CommitAsync(ct);
    }
}
