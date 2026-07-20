using System.Security.Claims;
using CrewService.Application.Authorization;
using CrewService.Application.Bootstrap;
using CrewService.Domain.Constants;
using CrewService.Domain.Models.UserAccess;
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
    BootstrapQueryService bootstrapQueryService,
    IRequestActorContextResolver actorContextResolver,
    IRequestActorContextPolicy actorContextPolicy,
    IHttpContextAccessor httpContextAccessor) : BootstrapSrvc.BootstrapSrvcBase
{
    // ── Full bootstrap ──────────────────────────────────────────────────

    public override async Task<BootstrapResponse> GetBootstrapData(
        BootstrapRequest request, ServerCallContext context)
    {
        var user = GetAuthenticatedUser();
        var response = new BootstrapResponse();

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var employeeNumber = user.FindFirst(CustomClaimTypes.EmployeeNumber)?.Value;

        var parentsTask = bootstrapQueryService.GetAllParentsWithRailroadsAsync(context.CancellationToken);
        var rolesTask = bootstrapQueryService.GetAllRolesAsync(context.CancellationToken);
        var featuresTask = bootstrapQueryService.GetAllFeaturesAsync(context.CancellationToken);
        var employeeTask = bootstrapQueryService.ResolveEmployeeAsync(userId, employeeNumber, context.CancellationToken);

        await Task.WhenAll(parentsTask, rolesTask, featuresTask, employeeTask);

        var parentsWithRailroads = parentsTask.Result;
        var roles = rolesTask.Result;
        var features = featuresTask.Result;
        var employeeInfo = employeeTask.Result;

        // Build context options
        var allContextParents = BuildContextParents(parentsWithRailroads);
        response.Parents.AddRange(FilterContextParents(allContextParents, user));

        response.Employee = new BootstrapEmployee
        {
            CtrlNbr = employeeInfo.CtrlNbr,
            EmployeeNumber = employeeInfo.EmployeeNumber,
            Found = employeeInfo.Found
        };

        response.UseEmployeeProfilePath = false;
        if (employeeInfo.Found)
        {
            var actorContext = await actorContextResolver.ResolveAsync(
                requestedEmployeeCtrlNbr: employeeInfo.CtrlNbr,
                ct: context.CancellationToken);

            response.UseEmployeeProfilePath = actorContextPolicy.ShouldUseEmployeeBehavior(actorContext);
        }

        response.ActiveCraft = employeeInfo.Found
            ? await ResolveActiveCraftResponseAsync(employeeInfo.CtrlNbr, context.CancellationToken)
            : new BootstrapCraft { Found = false };

        foreach (var role in roles)
            response.Roles.Add(new BootstrapRole { CtrlNbr = role.CtrlNbr, Name = role.Name });

        foreach (var feature in features)
            response.Features.Add(new BootstrapFeature { CtrlNbr = feature.CtrlNbr, Key = feature.Key });

        var userRoleCtrlNbrs = roles
            .Where(r => user.IsInRole(r.Name))
            .Select(r => r.CtrlNbr)
            .ToList();
        response.UserRoleCtrlNbrs.AddRange(userRoleCtrlNbrs);

        ControlNumber? craftCtrlNbr = response.ActiveCraft is { Found: true }
            ? ControlNumber.Create(response.ActiveCraft.CtrlNbr) : null;

        foreach (var roleId in userRoleCtrlNbrs)
        {
            var perms = await bootstrapQueryService.GetEffectivePermissionsAsync(
                ControlNumber.Create(roleId), parentCtrlNbr: null, craftCtrlNbr, context.CancellationToken);
            foreach (var p in perms)
            {
                response.Permissions.Add(new BootstrapPermission
                {
                    FeatureCtrlNbr = p.FeatureCtrlNbr,
                    AccessLevel = p.AccessLevel
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
        var parentsWithRailroads = await bootstrapQueryService.GetAllParentsWithRailroadsAsync(context.CancellationToken);
        var allContextParents = BuildContextParents(parentsWithRailroads);
        var response = new GetContextOptionsResponse();
        response.Parents.AddRange(FilterContextParents(allContextParents, user));
        return response;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private ClaimsPrincipal GetAuthenticatedUser()
    {
        return httpContextAccessor.HttpContext?.User
            ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "No authenticated user."));
    }

    private static List<ContextParent> BuildContextParents(
        List<BootstrapQueryService.ParentWithRailroads> parentsWithRailroads)
    {
        return parentsWithRailroads.Select(p =>
        {
            var cp = new ContextParent { CtrlNbr = p.Parent.CtrlNbr.Value, Name = p.Parent.Name.Value };
            foreach (var rr in p.Railroads)
                cp.Railroads.Add(new ContextRailroad
                {
                    CtrlNbr = rr.CtrlNbr.Value,
                    Name = rr.Name,
                    RrMark = rr.Code ?? string.Empty
                });
            return cp;
        }).ToList();
    }

    private static List<ContextParent> FilterContextParents(List<ContextParent> result, ClaimsPrincipal user)
    {
        if (user.IsInRole(Roles.SystemAdmin))
            return result;

        var claimParts = user.Claims
            .Where(c => c.Type == CustomClaimTypes.ParentRole)
            .Select(c => c.Value.Split(':'))
            .Where(parts => parts.Length >= 2 && long.TryParse(parts[0], out _))
            .ToList();

        var allowedParents = claimParts.Select(p => long.Parse(p[0])).Distinct().ToHashSet();
        var allowedRailroads = claimParts
            .Where(p => p.Length >= 3 && long.TryParse(p[2], out _))
            .Select(p => long.Parse(p[2])).Distinct().ToHashSet();

        if (allowedParents.Count == 0) return result;

        return result
            .Where(p => allowedParents.Contains(p.CtrlNbr))
            .Select(p => allowedRailroads.Count > 0 ? FilterRailroads(p, allowedRailroads) : p)
            .ToList();
    }

    private static ContextParent FilterRailroads(ContextParent parent, HashSet<long> allowedRailroads)
    {
        var filtered = new ContextParent { CtrlNbr = parent.CtrlNbr, Name = parent.Name };
        filtered.Railroads.AddRange(parent.Railroads.Where(r => allowedRailroads.Contains(r.CtrlNbr)));
        return filtered;
    }

    private async Task<BootstrapCraft> ResolveActiveCraftResponseAsync(long employeeCtrlNbr, CancellationToken ct)
    {
        try
        {
            var info = await bootstrapQueryService.ResolveActiveCraftAsync(employeeCtrlNbr, ct);
            return info.Found
                ? new BootstrapCraft { CtrlNbr = info.CtrlNbr, Name = info.CraftName, Found = true }
                : new BootstrapCraft { Found = false };
        }
        catch
        {
            return new BootstrapCraft { Found = false };
        }
    }
}
