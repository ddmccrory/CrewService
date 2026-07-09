using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.SeniorityOps;

public sealed class RosterAppService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    // ── Query ─────────────────────────────────────────────────────────────────

    public async Task<(List<Roster> Rosters, Dictionary<ControlNumber, string> WorkAreaNames, Dictionary<ControlNumber, string> CraftNames)>
        GetRostersAsync(
            long craftCtrlNbr, long parentCtrlNbr, long dynamicGroupCtrlNbr,
            CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        List<Roster> rosters;
        if (craftCtrlNbr > 0)
        {
            rosters = await uow.Rosters.GetByCraftCtrlNbrAsync(ControlNumber.Create(craftCtrlNbr));
        }
        else if (parentCtrlNbr > 0)
        {
            var crafts = await uow.Crafts.GetByParentAndRailroadAsync(
                ControlNumber.Create(parentCtrlNbr),
                dynamicGroupCtrlNbr > 0 ? ControlNumber.Create(dynamicGroupCtrlNbr) : null);
            var craftCtrlNbrs = crafts.Select(c => c.CtrlNbr).ToList();
            rosters = craftCtrlNbrs.Count > 0
                ? await uow.Rosters.GetByCraftCtrlNbrsAsync(craftCtrlNbrs)
                : [];
        }
        else
        {
            rosters = await uow.Rosters.GetAllAsync(ct);
        }

        var workAreaCtrlNbrs = rosters.Select(r => r.WorkAreaGroupCtrlNbr).Distinct().ToList();
        var workAreas = await uow.DynamicGroups.GetByCtrlNbrsAsync(workAreaCtrlNbrs);
        var workAreaNames = workAreas.ToDictionary(g => g.CtrlNbr, g => g.Name);

        var craftNames = new Dictionary<ControlNumber, string>();
        foreach (var ctrlNbr in rosters.Select(r => r.CraftCtrlNbr).Distinct())
        {
            var craft = await uow.Crafts.GetByCtrlNbrAsync(ctrlNbr, ct);
            if (craft is not null) craftNames[ctrlNbr] = craft.CraftName;
        }

        return (rosters, workAreaNames, craftNames);
    }

    public async Task<(Roster? Roster, string WorkAreaName, string CraftName)>
        GetRosterAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var roster = await uow.Rosters.GetByCtrlNbrAsync(ctrlNbr, ct);
        if (roster is null) return (null, string.Empty, string.Empty);

        var group = await uow.DynamicGroups.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr, ct);
        var craft = await uow.Crafts.GetByCtrlNbrAsync(roster.CraftCtrlNbr, ct);
        return (roster, group?.Name ?? string.Empty, craft?.CraftName ?? string.Empty);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<Roster> CreateRosterAsync(
        ControlNumber craftCtrlNbr,
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber? railroadPayrollDepartmentCtrlNbr,
        string rosterName,
        string rosterPluralName,
        int rosterNumber,
        RosterType rosterType = RosterType.Active,
        CancellationToken ct = default)
    {
        var roster = Roster.Create(
            craftCtrlNbr, workAreaGroupCtrlNbr, railroadPayrollDepartmentCtrlNbr,
            rosterName, rosterPluralName, rosterNumber, rosterType);

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.Rosters.Add(roster);
        await uow.CommitAsync(ct);
        return roster;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public async Task<Roster> UpdateRosterAsync(
        ControlNumber ctrlNbr,
        string rosterName,
        string rosterPluralName,
        int rosterNumber,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var roster = await uow.Rosters.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Roster {ctrlNbr.Value} not found.");
        roster.Update(rosterName, rosterPluralName, rosterNumber);
        uow.Rosters.Update(roster);
        await uow.CommitAsync(ct);
        return roster;
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public async Task<ControlNumber> DeleteRosterAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var roster = await uow.Rosters.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Roster {ctrlNbr.Value} not found.");
        uow.Rosters.Remove(roster);
        await uow.CommitAsync(ct);
        return roster.CtrlNbr;
    }
}
