using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Boards;

public sealed class BoardCascadePolicyService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<BoardCascadePolicy?> GetByWorkAreaAndCraftAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.BoardCascadePolicies.GetByWorkAreaAndCraftAsync(workAreaGroupCtrlNbr, craftCtrlNbr);
    }

    public async Task<BoardCascadePolicy> UpsertAsync(
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber craftCtrlNbr,
        string cascadeMode,
        int? maxLevels,
        bool auxEnabled,
        int? auxMaxLevels,
        string? selectionStrategy)
    {
        await using var uow = await uowFactory.CreateAsync();
        var existing = await uow.BoardCascadePolicies.GetByWorkAreaAndCraftAsync(workAreaGroupCtrlNbr, craftCtrlNbr);
        if (existing is not null)
            uow.BoardCascadePolicies.Remove(existing);

        var policy = BoardCascadePolicy.Create(workAreaGroupCtrlNbr, craftCtrlNbr, cascadeMode, maxLevels, auxEnabled, auxMaxLevels, selectionStrategy);
        uow.BoardCascadePolicies.Add(policy);
        await uow.CommitAsync();
        return policy;
    }
}
