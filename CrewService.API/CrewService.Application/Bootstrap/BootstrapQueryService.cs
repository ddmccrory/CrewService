using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Bootstrap;

public sealed class BootstrapQueryService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public sealed record ParentWithRailroads(Parent Parent, List<DynamicGroup> Railroads);
    public sealed record EmployeeInfo(bool Found, long CtrlNbr, string EmployeeNumber);
    public sealed record CraftInfo(bool Found, long CtrlNbr, string CraftName);
    public sealed record RoleInfo(long CtrlNbr, string Name, int Level);
    public sealed record FeatureInfo(long CtrlNbr, string Key);
    public sealed record PermissionInfo(long FeatureCtrlNbr, int AccessLevel);

    public async Task<List<ParentWithRailroads>> GetAllParentsWithRailroadsAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var parents = await uow.Parents.GetAllAsync(ct);
        var result = new List<ParentWithRailroads>();
        foreach (var parent in parents.OrderBy(p => p.Name.Value))
        {
            var railroads = await uow.DynamicGroups.GetByGroupTypeNameAsync(
                "Railroad", parent.CtrlNbr);
            result.Add(new ParentWithRailroads(parent, railroads.OrderBy(r => r.Name).ToList()));
        }
        return result;
    }

    public async Task<EmployeeInfo> ResolveEmployeeAsync(string? employeeNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
            return new EmployeeInfo(false, 0, string.Empty);

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await uow.Employees.GetByEmployeeNumberAsync(employeeNumber);
        if (employee is null) return new EmployeeInfo(false, 0, string.Empty);
        return new EmployeeInfo(true, employee.CtrlNbr.Value, employee.EmployeeNumber);
    }

    public async Task<CraftInfo> ResolveActiveCraftAsync(long employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var seniorityRecords = await uow.Seniority.GetByEmployeeCtrlNbrAsync(ControlNumber.Create(employeeCtrlNbr));
        var activeRecord = seniorityRecords.FirstOrDefault(s => s.LastActiveRoster);
        if (activeRecord is null) return new CraftInfo(false, 0, string.Empty);

        var roster = await uow.Rosters.GetByCtrlNbrAsync(activeRecord.RosterCtrlNbr, ct);
        if (roster is null) return new CraftInfo(false, 0, string.Empty);

        var craft = await uow.Crafts.GetByCtrlNbrAsync(roster.CraftCtrlNbr, ct);
        if (craft is null) return new CraftInfo(false, 0, string.Empty);

        return new CraftInfo(true, craft.CtrlNbr.Value, craft.CraftName);
    }

    public async Task<List<RoleInfo>> GetAllRolesAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var roles = await uow.Roles.GetAllAsync(ct);
        return roles.OrderByDescending(r => r.Level).ThenBy(r => r.Name)
            .Select(r => new RoleInfo(r.CtrlNbr.Value, r.Name, r.Level))
            .ToList();
    }

    public async Task<List<FeatureInfo>> GetAllFeaturesAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var features = await uow.Features.GetAllAsync(ct);
        return features.Select(f => new FeatureInfo(f.CtrlNbr.Value, f.Key)).ToList();
    }

    public async Task<List<PermissionInfo>> GetEffectivePermissionsAsync(
        ControlNumber roleCtrlNbr, ControlNumber? parentCtrlNbr, ControlNumber? craftCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var perms = await uow.Permissions.GetEffectivePermissionsAsync(roleCtrlNbr, parentCtrlNbr, craftCtrlNbr, ct);
        return perms.Select(p => new PermissionInfo(p.FeatureCtrlNbr.Value, (int)p.AccessLevel)).ToList();
    }
}
