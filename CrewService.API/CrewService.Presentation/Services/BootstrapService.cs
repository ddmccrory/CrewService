using System.Security.Claims;
using CrewService.Domain.Constants;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace CrewService.Presentation.Services;

/// <summary>
/// Server-side gRPC service that combines multiple startup queries into a
/// single round-trip.  The Blazor UI calls <see cref="GetBootstrapData"/>
/// once per circuit instead of making 4-6 independent gRPC calls for
/// employee, craft, catalogs, permissions, and context options.
/// </summary>
public sealed class BootstrapService(
    IParentRepository parentRepository,
    IDynamicGroupRepository dynamicGroupRepository,
    IEmployeeRepository employeeRepository,
    ISeniorityRepository seniorityRepository,
    IRosterRepository rosterRepository,
    ICraftRepository craftRepository,
    IRoleRepository roleRepository,
    IFeatureRepository featureRepository,
    IPermissionRepository permissionRepository,
    IHttpContextAccessor httpContextAccessor) : BootstrapSrvc.BootstrapSrvcBase
{
    // ── Full bootstrap ──────────────────────────────────────────────────

    public override async Task<BootstrapResponse> GetBootstrapData(
        BootstrapRequest request, ServerCallContext context)
    {
        var user = GetAuthenticatedUser();
        var response = new BootstrapResponse();

        // Wave 1 — independent queries run in parallel
        var parentsTask = BuildContextOptionsAsync(user);
        var rolesTask = roleRepository.GetAllAsync();
        var featuresTask = featureRepository.GetAllAsync();
        var employeeNumber = user.FindFirst(CustomClaimTypes.EmployeeNumber)?.Value;
        var employeeTask = ResolveEmployeeAsync(employeeNumber);

        await Task.WhenAll(parentsTask, rolesTask, featuresTask, employeeTask);

        response.Parents.AddRange(parentsTask.Result);

        var roles = rolesTask.Result;
        var features = featuresTask.Result;
        var employee = employeeTask.Result;

        response.Employee = employee;

        // Wave 2 — craft resolution needs the employee record
        response.ActiveCraft = employee.Found
            ? await ResolveActiveCraftAsync(employee.CtrlNbr)
            : new BootstrapCraft { Found = false };

        // Catalogs
        foreach (var role in roles.OrderByDescending(r => r.Level).ThenBy(r => r.Name))
            response.Roles.Add(new BootstrapRole { CtrlNbr = role.CtrlNbr.Value, Name = role.Name });

        foreach (var feature in features)
            response.Features.Add(new BootstrapFeature { CtrlNbr = feature.CtrlNbr.Value, Key = feature.Key });

        // Resolve the authenticated user's role CtrlNbrs from the catalog
        var userRoleCtrlNbrs = roles
            .Where(r => user.IsInRole(r.Name))
            .Select(r => r.CtrlNbr.Value)
            .ToList();
        response.UserRoleCtrlNbrs.AddRange(userRoleCtrlNbrs);

        // Wave 3 — effective permissions (no parent context for initial menu render)
        ControlNumber? craftCtrlNbr = response.ActiveCraft is { Found: true }
            ? ControlNumber.Create(response.ActiveCraft.CtrlNbr)
            : null;

        foreach (var roleId in userRoleCtrlNbrs)
        {
            var perms = await permissionRepository.GetEffectivePermissionsAsync(
                ControlNumber.Create(roleId), parentCtrlNbr: null, craftCtrlNbr);

            foreach (var p in perms)
            {
                response.Permissions.Add(new BootstrapPermission
                {
                    FeatureCtrlNbr = p.FeatureCtrlNbr.Value,
                    AccessLevel = (int)p.AccessLevel
                });
            }
        }

        return response;
    }

    // ── Context options only ────────────────────────────────────────────

    public override async Task<GetContextOptionsResponse> GetContextOptions(
        GetContextOptionsRequest request, ServerCallContext context)
    {
        var user = GetAuthenticatedUser();
        var response = new GetContextOptionsResponse();
        response.Parents.AddRange(await BuildContextOptionsAsync(user));
        return response;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private ClaimsPrincipal GetAuthenticatedUser()
    {
        return httpContextAccessor.HttpContext?.User
            ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "No authenticated user."));
    }

    private async Task<List<ContextParent>> BuildContextOptionsAsync(ClaimsPrincipal user)
    {
        var allParents = await parentRepository.GetAllAsync();
        var result = new List<ContextParent>();

        foreach (var parent in allParents.OrderBy(p => p.Name.Value))
        {
            var railroadGroups = await dynamicGroupRepository
                .GetByGroupTypeNameAsync("Railroad", parent.CtrlNbr.Value);

            var cp = new ContextParent
            {
                CtrlNbr = parent.CtrlNbr.Value,
                Name = parent.Name.Value
            };

            foreach (var rr in railroadGroups.OrderBy(r => r.Name))
            {
                cp.Railroads.Add(new ContextRailroad
                {
                    CtrlNbr = rr.CtrlNbr.Value,
                    Name = rr.Name,
                    RrMark = rr.Code ?? string.Empty
                });
            }

            result.Add(cp);
        }

        // SystemAdmin sees everything
        if (user.IsInRole(Roles.SystemAdmin))
            return result;

        // Non-admin: filter by parent_role claims
        var claimParts = user.Claims
            .Where(c => c.Type == CustomClaimTypes.ParentRole)
            .Select(c => c.Value.Split(':'))
            .Where(parts => parts.Length >= 2 && long.TryParse(parts[0], out _))
            .ToList();

        var allowedParents = claimParts
            .Select(p => long.Parse(p[0]))
            .Distinct()
            .ToHashSet();

        var allowedRailroads = claimParts
            .Where(p => p.Length >= 3 && long.TryParse(p[2], out _))
            .Select(p => long.Parse(p[2]))
            .Distinct()
            .ToHashSet();

        if (allowedParents.Count == 0)
            return result;

        return result
            .Where(p => allowedParents.Contains(p.CtrlNbr))
            .Select(p => allowedRailroads.Count > 0
                ? FilterRailroads(p, allowedRailroads)
                : p)
            .ToList();
    }

    private static ContextParent FilterRailroads(ContextParent parent, HashSet<long> allowedRailroads)
    {
        var filtered = new ContextParent
        {
            CtrlNbr = parent.CtrlNbr,
            Name = parent.Name
        };
        filtered.Railroads.AddRange(
            parent.Railroads.Where(r => allowedRailroads.Contains(r.CtrlNbr)));
        return filtered;
    }

    private async Task<BootstrapEmployee> ResolveEmployeeAsync(string? employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
            return new BootstrapEmployee { Found = false };

        try
        {
            var employee = await employeeRepository.GetByEmployeeNumberAsync(employeeNumber);
            if (employee is null)
                return new BootstrapEmployee { Found = false };

            return new BootstrapEmployee
            {
                CtrlNbr = employee.CtrlNbr.Value,
                EmployeeNumber = employee.EmployeeNumber,
                Found = true
            };
        }
        catch
        {
            return new BootstrapEmployee { Found = false };
        }
    }

    private async Task<BootstrapCraft> ResolveActiveCraftAsync(long employeeCtrlNbr)
    {
        try
        {
            var seniorityRecords = await seniorityRepository
                .GetByEmployeeCtrlNbrAsync(ControlNumber.Create(employeeCtrlNbr));

            var activeRecord = seniorityRecords.FirstOrDefault(s => s.LastActiveRoster);
            if (activeRecord is null)
                return new BootstrapCraft { Found = false };

            var roster = await rosterRepository.GetByCtrlNbrAsync(activeRecord.RosterCtrlNbr);
            if (roster is null)
                return new BootstrapCraft { Found = false };

            var craft = await craftRepository.GetByCtrlNbrAsync(roster.CraftCtrlNbr);
            if (craft is null)
                return new BootstrapCraft { Found = false };

            return new BootstrapCraft
            {
                CtrlNbr = craft.CtrlNbr.Value,
                Name = craft.CraftName,
                Found = true
            };
        }
        catch
        {
            return new BootstrapCraft { Found = false };
        }
    }
}
