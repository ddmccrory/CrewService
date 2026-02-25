using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.TenantConfig;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CrewService.GrpcService;

/// <summary>
/// Seeds the development database with sample data for the RailroadGroupPlacement
/// feature, covering all three supported scenarios:
///   1. Simple – railroad under a parent with no placement rows
///   2. Simple + WorkArea – railroad placed into a single work-area group
///   3. Holding company – multi-level tree (Region ? Subdivision ? WorkArea)
/// Idempotent: skips seeding when GroupTypes already exist.
/// </summary>
internal static class DevDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var groupTypeRepo = sp.GetRequiredService<IGroupTypeRepository>();
        var groupRepo = sp.GetRequiredService<IDynamicGroupRepository>();
        var parentRepo = sp.GetRequiredService<IParentRepository>();
        var railroadRepo = sp.GetRequiredService<IRailroadRepository>();
        var placementRepo = sp.GetRequiredService<IRailroadGroupPlacementRepository>();

        // Idempotent guard – if group types already exist, skip seeding
        var existing = await groupTypeRepo.GetAllAsync();
        if (existing.Count > 0)
            return;

        // ?? Group Types ??????????????????????????????????????????????
        var regionType = GroupType.Create("Region", "Geographic region", isWorkArea: false);
        var subdivType = GroupType.Create("Subdivision", "Track subdivision", isWorkArea: false);
        var workAreaType = GroupType.Create("WorkArea", "Operational work area", isWorkArea: true);

        await groupTypeRepo.AddAsync(regionType);
        await groupTypeRepo.AddAsync(subdivType);
        await groupTypeRepo.AddAsync(workAreaType);

        // ?? Scenario 1: Simple (no placements) ??????????????????????
        var simpleCorp = Parent.Create("Simple Corp");
        await parentRepo.AddAsync(simpleCorp);

        var simpleRR = Railroad.Create(simpleCorp.CtrlNbr.Value, "SMPL", "Simple Railroad");
        await railroadRepo.AddAsync(simpleRR);
        // No placement rows – backward-compatible scenario

        // ?? Scenario 2: Simple + WorkArea ????????????????????????????
        var waCorp = Parent.Create("WorkArea Corp");
        await parentRepo.AddAsync(waCorp);

        var waRR = Railroad.Create(waCorp.CtrlNbr.Value, "WARK", "WorkArea Railroad");
        await railroadRepo.AddAsync(waRR);

        var waGroup = DynamicGroup.Create(
            workAreaType.CtrlNbr.Value,
            "Main Yard",
            parentGroupCtrlNbr: null,
            path: "/main-yard",
            isWorkArea: true);
        await groupRepo.AddAsync(waGroup);

        var waPlacement = RailroadGroupPlacement.Create(waRR.CtrlNbr.Value, waGroup.CtrlNbr.Value);
        await placementRepo.AddAsync(waPlacement);

        // ?? Scenario 3: Holding Company ??????????????????????????????
        // Parent ? Region ? Subdivision ? WorkArea
        var holdingCorp = Parent.Create("CSX Corporation");
        await parentRepo.AddAsync(holdingCorp);

        var csxRR = Railroad.Create(holdingCorp.CtrlNbr.Value, "CSX", "CSX Transportation");
        await railroadRepo.AddAsync(csxRR);

        var csxtRR = Railroad.Create(holdingCorp.CtrlNbr.Value, "CSXT", "CSX Intermodal");
        await railroadRepo.AddAsync(csxtRR);

        // Group tree
        var southeast = DynamicGroup.Create(
            regionType.CtrlNbr.Value,
            "Southeast Region",
            parentGroupCtrlNbr: null,
            path: "/southeast",
            isWorkArea: false);
        await groupRepo.AddAsync(southeast);

        var jaxSub = DynamicGroup.Create(
            subdivType.CtrlNbr.Value,
            "Jacksonville Sub",
            parentGroupCtrlNbr: southeast.CtrlNbr.Value,
            path: "/southeast/jax",
            isWorkArea: false);
        await groupRepo.AddAsync(jaxSub);

        var jaxYard = DynamicGroup.Create(
            workAreaType.CtrlNbr.Value,
            "Jax Yard",
            parentGroupCtrlNbr: jaxSub.CtrlNbr.Value,
            path: "/southeast/jax/yard",
            isWorkArea: true);
        await groupRepo.AddAsync(jaxYard);

        var midwest = DynamicGroup.Create(
            regionType.CtrlNbr.Value,
            "Midwest Region",
            parentGroupCtrlNbr: null,
            path: "/midwest",
            isWorkArea: false);
        await groupRepo.AddAsync(midwest);

        // Place CSX at Region level, CSXT at the WorkArea level
        var csxPlacement = RailroadGroupPlacement.Create(csxRR.CtrlNbr.Value, southeast.CtrlNbr.Value);
        await placementRepo.AddAsync(csxPlacement);

        var csxtPlacement = RailroadGroupPlacement.Create(csxtRR.CtrlNbr.Value, jaxYard.CtrlNbr.Value);
        await placementRepo.AddAsync(csxtPlacement);
    }
}
