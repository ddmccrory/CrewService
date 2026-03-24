using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.TenantConfig;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CrewService.GrpcService;

/// <summary>
/// Seeds baseline data required in all environments (dev, staging, production).
/// Ensures every Parent has its own Railroad and WorkArea system GroupTypes.
/// Idempotent — safe to call on every startup.
/// </summary>
internal static class BaselineSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        // Provide a synthetic SYSTEM user so auditing works outside an HTTP request
        var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "SYSTEM")], "Seed"))
        };

        var groupTypeRepo = sp.GetRequiredService<IGroupTypeRepository>();
        var parentRepo = sp.GetRequiredService<IParentRepository>();

        // Backfill system GroupTypes for any parent that is missing them
        var allParents = await parentRepo.GetAllAsync();
        var allGroupTypes = await groupTypeRepo.GetAllAsync();

        foreach (var parent in allParents)
        {
            foreach (var systemTypeName in GroupType.SystemTypeNames)
            {
                if (!allGroupTypes.Any(gt => gt.Name == systemTypeName && gt.ParentCtrlNbr == parent.CtrlNbr.Value))
                {
                    var isWorkArea = string.Equals(systemTypeName, "WorkArea", StringComparison.OrdinalIgnoreCase);
                    await groupTypeRepo.AddAsync(
                        GroupType.Create(systemTypeName, $"{systemTypeName} (auto-created)", isWorkArea: isWorkArea, parentCtrlNbr: parent.CtrlNbr.Value));
                }
            }
        }

        // Backfill materialized paths for any DynamicGroup missing them
        var dynamicGroupRepo = sp.GetRequiredService<IDynamicGroupRepository>();
        await dynamicGroupRepo.BackfillPathsAsync();
    }
}