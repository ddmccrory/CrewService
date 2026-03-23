using CrewService.Domain.Modules.TenantConfig;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CrewService.GrpcService;

/// <summary>
/// Seeds baseline data required in all environments (dev, staging, production).
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

        // Universal GroupTypes — available to all parents
        var allTypes = await groupTypeRepo.GetAllAsync();
        if (!allTypes.Any(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == 0))
        {
            await groupTypeRepo.AddAsync(
                GroupType.Create("Railroad", "Railroad operational boundaries", isWorkArea: false));
        }

        if (!allTypes.Any(gt => gt.Name == "WorkArea" && gt.ParentCtrlNbr == 0))
        {
            await groupTypeRepo.AddAsync(
                GroupType.Create("WorkArea", "Operational work area", isWorkArea: true));
        }
    }
}
