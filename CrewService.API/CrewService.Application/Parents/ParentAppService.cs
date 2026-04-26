using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Parents;

public sealed class ParentAppService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<List<Parent>> GetAllAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Parents.GetAllAsync();
    }

    public async Task<Parent> GetAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Parents.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Parent {ctrlNbr.Value} not found.");
    }

    public async Task<List<DynamicGroup>> GetRailroadsAsync(long parentCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.DynamicGroups.GetByGroupTypeNameAsync("Railroad", ControlNumber.Create(parentCtrlNbr));
    }

    public async Task<Parent> CreateAsync(string name, CancellationToken ct = default)
    {
        var parent = Parent.Create(name);

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.Parents.Add(parent);

        foreach (var systemTypeName in GroupType.SystemTypeNames)
        {
            var description = systemTypeName switch
            {
                "Railroad" => "Railroad operational boundaries",
                _ => $"{systemTypeName} (auto-created)"
            };
            var systemType = GroupType.Create(
                systemTypeName, description, isWorkArea: false,
                parentCtrlNbr: parent.CtrlNbr);
            uow.GroupTypes.Add(systemType);
        }

        var defaultStates = new (string Description, StateType Type)[]
        {
            ("Active", StateType.Active),
            ("Cut Back", StateType.CutBack),
            ("Inactive", StateType.Inactive),
            ("Terminated", StateType.Inactive),
            ("Dismissed", StateType.Inactive),
            ("Leave of Absence", StateType.Inactive),
            ("Medical Leave", StateType.Inactive),
            ("Retired", StateType.Inactive)
        };

        foreach (var (desc, type) in defaultStates)
        {
            var seniorityState = SeniorityState.Create(desc, type, parent.CtrlNbr.Value);
            uow.SeniorityStates.Add(seniorityState);
        }

        await uow.CommitAsync(ct);
        return parent;
    }

    public async Task<Parent> UpdateAsync(ControlNumber ctrlNbr, string name, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var parent = await uow.Parents.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Parent {ctrlNbr.Value} not found.");
        parent.Update(name);
        uow.Parents.Update(parent);
        await uow.CommitAsync(ct);
        return parent;
    }

    public async Task<Parent> DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var parent = await uow.Parents.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Parent {ctrlNbr.Value} not found.");
        uow.Parents.Remove(parent);
        await uow.CommitAsync(ct);
        return parent;
    }
}
