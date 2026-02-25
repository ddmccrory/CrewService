using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Modules.TenantConfig;
using CrewService.Persistance.Repositories;
using CrewService.UnitTests.Fixtures;

namespace CrewService.UnitTests.Modules.TenantConfig;

public sealed class RailroadGroupPlacementTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    /// <summary>
    /// Scenario 1 – Simple: Railroad is a direct child of Parent with no placement rows.
    /// Verifies backward-compatible behaviour where a railroad exists under a parent
    /// but has zero RailroadGroupPlacement records.
    /// </summary>
    [Fact]
    public async Task Simple_Railroad_Has_No_Placements()
    {
        // Arrange – create a Parent and Railroad (no placements)
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var railroadRepo = new RailroadRepository(ctx, _factory.CurrentUserService);
        var placementRepo = new RailroadGroupPlacementRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("BNSF Holdings");
        await parentRepo.AddAsync(parent);

        var railroad = Railroad.Create(parent.CtrlNbr.Value, "BNSF", "Burlington Northern Santa Fe");
        await railroadRepo.AddAsync(railroad);

        // Act
        var placements = await placementRepo.GetByRailroadCtrlNbrAsync(railroad.CtrlNbr);

        // Assert – no placement rows exist
        Assert.Empty(placements);
    }

    /// <summary>
    /// Scenario 2 – Simple + WorkArea: Railroad's group node is itself marked IsWorkArea=true.
    /// A single DynamicGroup acts as both the railroad's placement group and a work area.
    /// </summary>
    [Fact]
    public async Task Simple_Railroad_Placed_In_WorkArea_Group()
    {
        // Arrange
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var railroadRepo = new RailroadRepository(ctx, _factory.CurrentUserService);
        var groupTypeRepo = new GroupTypeRepository(ctx, _factory.CurrentUserService);
        var groupRepo = new DynamicGroupRepository(ctx, _factory.CurrentUserService);
        var placementRepo = new RailroadGroupPlacementRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("UP Holdings");
        await parentRepo.AddAsync(parent);

        var railroad = Railroad.Create(parent.CtrlNbr.Value, "UP", "Union Pacific");
        await railroadRepo.AddAsync(railroad);

        var groupType = GroupType.Create("WorkArea", "Work area node", isWorkArea: true);
        await groupTypeRepo.AddAsync(groupType);

        // Group is a work-area leaf directly under nothing (root-level)
        var workAreaGroup = DynamicGroup.Create(
            groupType.CtrlNbr.Value,
            "UP Main Yard",
            parentGroupCtrlNbr: null,
            path: $"/{groupType.CtrlNbr.Value}",
            isWorkArea: true);
        await groupRepo.AddAsync(workAreaGroup);

        // Act – place railroad into the work-area group
        var placement = RailroadGroupPlacement.Create(railroad.CtrlNbr.Value, workAreaGroup.CtrlNbr.Value);
        await placementRepo.AddAsync(placement);

        var byRailroad = await placementRepo.GetByRailroadCtrlNbrAsync(railroad.CtrlNbr);
        var byGroup = await placementRepo.GetByGroupCtrlNbrAsync(workAreaGroup.CtrlNbr);

        // Assert
        Assert.Single(byRailroad);
        Assert.Single(byGroup);
        Assert.Equal(railroad.CtrlNbr, byRailroad[0].RailroadCtrlNbr);
        Assert.Equal(workAreaGroup.CtrlNbr, byRailroad[0].GroupCtrlNbr);
    }

    /// <summary>
    /// Scenario 3 – Holding Company: Parent ? Region ? Railroad ? Subdivision ? WorkArea.
    /// Builds a multi-level group tree and places a railroad at the "Railroad" level.
    /// Tests subtree query returns placements across the full depth.
    /// </summary>
    [Fact]
    public async Task HoldingCompany_Subtree_Returns_All_Placements()
    {
        // Arrange
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var railroadRepo = new RailroadRepository(ctx, _factory.CurrentUserService);
        var groupTypeRepo = new GroupTypeRepository(ctx, _factory.CurrentUserService);
        var groupRepo = new DynamicGroupRepository(ctx, _factory.CurrentUserService);
        var placementRepo = new RailroadGroupPlacementRepository(ctx, _factory.CurrentUserService);

        // Create parent company
        var parent = Parent.Create("CSX Corporation");
        await parentRepo.AddAsync(parent);

        // Create group types
        var regionType = GroupType.Create("Region", "Geographic region", isWorkArea: false);
        await groupTypeRepo.AddAsync(regionType);

        var subdivType = GroupType.Create("Subdivision", "Track subdivision", isWorkArea: false);
        await groupTypeRepo.AddAsync(subdivType);

        var workAreaType = GroupType.Create("WorkArea", "Work area", isWorkArea: true);
        await groupTypeRepo.AddAsync(workAreaType);

        // Build group tree: Region ? Subdivision ? WorkArea
        var regionGroup = DynamicGroup.Create(
            regionType.CtrlNbr.Value,
            "Southeast Region",
            parentGroupCtrlNbr: null,
            path: "/southeast",
            isWorkArea: false);
        await groupRepo.AddAsync(regionGroup);

        var subdivGroup = DynamicGroup.Create(
            subdivType.CtrlNbr.Value,
            "Jacksonville Sub",
            parentGroupCtrlNbr: regionGroup.CtrlNbr.Value,
            path: "/southeast/jax",
            isWorkArea: false);
        await groupRepo.AddAsync(subdivGroup);

        var workAreaGroup = DynamicGroup.Create(
            workAreaType.CtrlNbr.Value,
            "Jax Yard",
            parentGroupCtrlNbr: subdivGroup.CtrlNbr.Value,
            path: "/southeast/jax/yard",
            isWorkArea: true);
        await groupRepo.AddAsync(workAreaGroup);

        // Create two railroads
        var csx = Railroad.Create(parent.CtrlNbr.Value, "CSX", "CSX Transportation");
        await railroadRepo.AddAsync(csx);

        var csxt = Railroad.Create(parent.CtrlNbr.Value, "CSXT", "CSX Intermodal");
        await railroadRepo.AddAsync(csxt);

        // Place CSX at the Region level, CSXT at the WorkArea level
        var placementRegion = RailroadGroupPlacement.Create(csx.CtrlNbr.Value, regionGroup.CtrlNbr.Value);
        await placementRepo.AddAsync(placementRegion);

        var placementWorkArea = RailroadGroupPlacement.Create(csxt.CtrlNbr.Value, workAreaGroup.CtrlNbr.Value);
        await placementRepo.AddAsync(placementWorkArea);

        // Act – subtree query from region should return both placements
        var subtreePlacements = await placementRepo.GetByGroupSubtreeAsync("/southeast");

        // Assert
        Assert.Equal(2, subtreePlacements.Count);
        Assert.Contains(subtreePlacements, p => p.RailroadCtrlNbr == csx.CtrlNbr);
        Assert.Contains(subtreePlacements, p => p.RailroadCtrlNbr == csxt.CtrlNbr);

        // Subtree from subdivision should return only CSXT (jax subtree)
        var jaxSubtree = await placementRepo.GetByGroupSubtreeAsync("/southeast/jax");
        var singlePlacement = Assert.Single(jaxSubtree);
        Assert.Equal(csxt.CtrlNbr, singlePlacement.RailroadCtrlNbr);
    }

    /// <summary>
    /// Verifies GetByRailroadAndGroupAsync returns a specific placement or null.
    /// </summary>
    [Fact]
    public async Task GetByRailroadAndGroup_Returns_Exact_Match()
    {
        // Arrange
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var railroadRepo = new RailroadRepository(ctx, _factory.CurrentUserService);
        var groupTypeRepo = new GroupTypeRepository(ctx, _factory.CurrentUserService);
        var groupRepo = new DynamicGroupRepository(ctx, _factory.CurrentUserService);
        var placementRepo = new RailroadGroupPlacementRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("NS Corp");
        await parentRepo.AddAsync(parent);

        var railroad = Railroad.Create(parent.CtrlNbr.Value, "NS", "Norfolk Southern");
        await railroadRepo.AddAsync(railroad);

        var groupType = GroupType.Create("Division", "Operating division", isWorkArea: false);
        await groupTypeRepo.AddAsync(groupType);

        var group = DynamicGroup.Create(groupType.CtrlNbr.Value, "Pocahontas Division", null, "/poc", false);
        await groupRepo.AddAsync(group);

        var placement = RailroadGroupPlacement.Create(railroad.CtrlNbr.Value, group.CtrlNbr.Value);
        await placementRepo.AddAsync(placement);

        // Act
        var found = await placementRepo.GetByRailroadAndGroupAsync(railroad.CtrlNbr, group.CtrlNbr);
        var notFound = await placementRepo.GetByRailroadAndGroupAsync(
            ControlNumber.Create(999999999999999),
            group.CtrlNbr);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(placement.CtrlNbr, found.CtrlNbr);
        Assert.Null(notFound);
    }

    /// <summary>
    /// Verifies that soft-deleting a placement via Remove() excludes it from queries.
    /// </summary>
    [Fact]
    public async Task Remove_SoftDeletes_Placement()
    {
        // Arrange
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var railroadRepo = new RailroadRepository(ctx, _factory.CurrentUserService);
        var groupTypeRepo = new GroupTypeRepository(ctx, _factory.CurrentUserService);
        var groupRepo = new DynamicGroupRepository(ctx, _factory.CurrentUserService);
        var placementRepo = new RailroadGroupPlacementRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("KCS Corp");
        await parentRepo.AddAsync(parent);

        var railroad = Railroad.Create(parent.CtrlNbr.Value, "KCS", "Kansas City Southern");
        await railroadRepo.AddAsync(railroad);

        var groupType = GroupType.Create("Region", "Region", isWorkArea: false);
        await groupTypeRepo.AddAsync(groupType);

        var group = DynamicGroup.Create(groupType.CtrlNbr.Value, "Gulf Region", null, "/gulf", false);
        await groupRepo.AddAsync(group);

        var placement = RailroadGroupPlacement.Create(railroad.CtrlNbr.Value, group.CtrlNbr.Value);
        await placementRepo.AddAsync(placement);

        // Act – soft-delete via repository
        await placementRepo.DeleteAsync(placement.CtrlNbr);

        var remaining = await placementRepo.GetByRailroadCtrlNbrAsync(railroad.CtrlNbr);

        // Assert – soft-deleted row should be excluded by global query filter
        Assert.Empty(remaining);
    }
}
