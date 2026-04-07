using CrewService.BlazorUI.Clients;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Scoped service that caches commonly-needed railroad reference data
/// (groups, work areas, departments, craft roles) for the current circuit.
/// Automatically invalidates its cache when the selected railroad changes.
/// Pages inject this instead of fetching the same reference data independently.
/// </summary>
public sealed class RailroadReferenceDataService(
    TenantConfigClient tenantConfigClient,
    DepartmentClient departmentClient,
    WorkManagementClient workManagementClient,
    AppContextService appContext)
{
    private long _loadedRailroad;
    private long _loadedParent;

    private List<GroupResponse>? _groups;
    private List<GroupResponse>? _workAreas;
    private IReadOnlyList<DepartmentResponse>? _departments;
    private IReadOnlyList<CraftRoleResponse>? _craftRoles;

    /// <summary>All groups for the current railroad (full tree).</summary>
    public async Task<IReadOnlyList<GroupResponse>> GetGroupsAsync()
    {
        EnsureContextCurrent();
        if (_groups is null)
        {
            try
            {
                var response = await tenantConfigClient.GetGroupTreeAsync(appContext.SelectedRailroadCtrlNbr ?? 0);
                _groups = response.Groups.ToList();
            }
            catch { _groups = []; }
        }
        return _groups;
    }

    /// <summary>Only groups flagged as work areas for the current railroad.</summary>
    public async Task<IReadOnlyList<GroupResponse>> GetWorkAreasAsync()
    {
        EnsureContextCurrent();
        if (_workAreas is null)
        {
            try
            {
                var response = await tenantConfigClient.GetWorkAreasAsync(appContext.SelectedRailroadCtrlNbr ?? 0);
                _workAreas = response.Groups.ToList();
            }
            catch { _workAreas = []; }
        }
        return _workAreas;
    }

    /// <summary>Departments for the current parent + railroad.</summary>
    public async Task<IReadOnlyList<DepartmentResponse>> GetDepartmentsAsync()
    {
        EnsureContextCurrent();
        if (_departments is null)
        {
            try
            {
                var response = await departmentClient.GetAllAsync(
                    appContext.SelectedParentCtrlNbr ?? 0,
                    appContext.SelectedRailroadCtrlNbr ?? 0);
                _departments = response.Departments;
            }
            catch { _departments = []; }
        }
        return _departments;
    }

    /// <summary>Craft roles for the current railroad.</summary>
    public async Task<IReadOnlyList<CraftRoleResponse>> GetCraftRolesAsync()
    {
        EnsureContextCurrent();
        if (_craftRoles is null)
        {
            try
            {
                var response = await workManagementClient.GetCraftRolesAsync(
                    railroadCtrlNbr: appContext.SelectedRailroadCtrlNbr ?? 0);
                _craftRoles = response.Roles;
            }
            catch { _craftRoles = []; }
        }
        return _craftRoles;
    }

    /// <summary>Clears all cached data so the next access re-fetches.</summary>
    public void Invalidate()
    {
        _loadedRailroad = 0;
        _loadedParent = 0;
        _groups = null;
        _workAreas = null;
        _departments = null;
        _craftRoles = null;
    }

    private void EnsureContextCurrent()
    {
        var rr = appContext.SelectedRailroadCtrlNbr ?? 0;
        var parent = appContext.SelectedParentCtrlNbr ?? 0;
        if (rr != _loadedRailroad || parent != _loadedParent)
        {
            _groups = null;
            _workAreas = null;
            _departments = null;
            _craftRoles = null;
            _loadedRailroad = rr;
            _loadedParent = parent;
        }
    }
}
