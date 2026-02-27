using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Modules.TenantConfig;
using CrewService.UnitTests.Fixtures;

namespace CrewService.UnitTests.Modules.TenantConfig;

public sealed class GroupAttributeTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Create_AttributeDefinition_Persists_All_Fields()
    {
        using var ctx = _factory.CreateContext();
        var groupTypeRepo = new GroupTypeRepository(ctx, _factory.CurrentUserService);
        var attrDefRepo = new GroupAttributeDefinitionRepository(ctx, _factory.CurrentUserService);

        var groupType = GroupType.Create("Region", "Geographic region", isWorkArea: false);
        await groupTypeRepo.AddAsync(groupType);

        var attrDef = GroupAttributeDefinition.Create(
            groupType.CtrlNbr.Value, "Timezone", "string", isRequired: true, defaultValue: "Eastern");
        await attrDefRepo.AddAsync(attrDef);

        var retrieved = await attrDefRepo.GetByCtrlNbrAsync(attrDef.CtrlNbr);

        Assert.NotNull(retrieved);
        Assert.Equal("Timezone", retrieved.AttributeName);
        Assert.Equal("string", retrieved.DataType);
        Assert.True(retrieved.IsRequired);
        Assert.Equal("Eastern", retrieved.DefaultValue);
        Assert.Equal(groupType.CtrlNbr, retrieved.GroupTypeCtrlNbr);
    }

    [Fact]
    public async Task GetByGroupType_Returns_Only_Matching_Definitions()
    {
        using var ctx = _factory.CreateContext();
        var groupTypeRepo = new GroupTypeRepository(ctx, _factory.CurrentUserService);
        var attrDefRepo = new GroupAttributeDefinitionRepository(ctx, _factory.CurrentUserService);

        var regionType = GroupType.Create("Region", "Region", isWorkArea: false);
        await groupTypeRepo.AddAsync(regionType);

        var yardType = GroupType.Create("Yard", "Yard", isWorkArea: true);
        await groupTypeRepo.AddAsync(yardType);

        await attrDefRepo.AddAsync(GroupAttributeDefinition.Create(regionType.CtrlNbr.Value, "Timezone", "string", false));
        await attrDefRepo.AddAsync(GroupAttributeDefinition.Create(regionType.CtrlNbr.Value, "Climate", "string", false));
        await attrDefRepo.AddAsync(GroupAttributeDefinition.Create(yardType.CtrlNbr.Value, "TrackCount", "int", true));

        var regionAttrs = await attrDefRepo.GetByGroupTypeCtrlNbrAsync(regionType.CtrlNbr);
        var yardAttrs = await attrDefRepo.GetByGroupTypeCtrlNbrAsync(yardType.CtrlNbr);

        Assert.Equal(2, regionAttrs.Count);
        Assert.Single(yardAttrs);
        Assert.All(regionAttrs, a => Assert.Equal(regionType.CtrlNbr, a.GroupTypeCtrlNbr));
        Assert.Equal("TrackCount", yardAttrs[0].AttributeName);
    }

    [Fact]
    public async Task Update_AttributeDefinition_Changes_Fields()
    {
        using var ctx = _factory.CreateContext();
        var groupTypeRepo = new GroupTypeRepository(ctx, _factory.CurrentUserService);
        var attrDefRepo = new GroupAttributeDefinitionRepository(ctx, _factory.CurrentUserService);

        var groupType = GroupType.Create("Division", "Division", isWorkArea: false);
        await groupTypeRepo.AddAsync(groupType);

        var attrDef = GroupAttributeDefinition.Create(groupType.CtrlNbr.Value, "MaxSpeed", "int", false, "60");
        await attrDefRepo.AddAsync(attrDef);

        attrDef.Update("MaxTrackSpeed", "decimal", true, "65.5");
        await attrDefRepo.UpdateAsync(attrDef);

        var updated = await attrDefRepo.GetByCtrlNbrAsync(attrDef.CtrlNbr);

        Assert.NotNull(updated);
        Assert.Equal("MaxTrackSpeed", updated.AttributeName);
        Assert.Equal("decimal", updated.DataType);
        Assert.True(updated.IsRequired);
        Assert.Equal("65.5", updated.DefaultValue);
    }

    [Fact]
    public async Task Delete_AttributeDefinition_SoftDeletes()
    {
        using var ctx = _factory.CreateContext();
        var groupTypeRepo = new GroupTypeRepository(ctx, _factory.CurrentUserService);
        var attrDefRepo = new GroupAttributeDefinitionRepository(ctx, _factory.CurrentUserService);

        var groupType = GroupType.Create("Yard", "Yard", isWorkArea: true);
        await groupTypeRepo.AddAsync(groupType);

        var attrDef = GroupAttributeDefinition.Create(groupType.CtrlNbr.Value, "TrackCount", "int", true, "10");
        await attrDefRepo.AddAsync(attrDef);

        await attrDefRepo.DeleteAsync(attrDef.CtrlNbr);

        var remaining = await attrDefRepo.GetByGroupTypeCtrlNbrAsync(groupType.CtrlNbr);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task Set_AttributeValue_Creates_New()
    {
        using var ctx = _factory.CreateContext();
        var groupTypeRepo = new GroupTypeRepository(ctx, _factory.CurrentUserService);
        var groupRepo = new DynamicGroupRepository(ctx, _factory.CurrentUserService);
        var attrDefRepo = new GroupAttributeDefinitionRepository(ctx, _factory.CurrentUserService);
        var attrValRepo = new GroupAttributeValueRepository(ctx, _factory.CurrentUserService);

        var groupType = GroupType.Create("Region", "Region", isWorkArea: false);
        await groupTypeRepo.AddAsync(groupType);

        var group = DynamicGroup.Create(groupType.CtrlNbr.Value, "Southeast", null, "/se", false);
        await groupRepo.AddAsync(group);

        var attrDef = GroupAttributeDefinition.Create(groupType.CtrlNbr.Value, "Timezone", "string", false, "Eastern");
        await attrDefRepo.AddAsync(attrDef);

        var value = GroupAttributeValue.Create(group.CtrlNbr.Value, attrDef.CtrlNbr.Value, "Central");
        await attrValRepo.AddAsync(value);

        var values = await attrValRepo.GetByGroupCtrlNbrAsync(group.CtrlNbr);

        var single = Assert.Single(values);
        Assert.Equal(group.CtrlNbr, single.GroupCtrlNbr);
        Assert.Equal(attrDef.CtrlNbr, single.AttributeDefinitionCtrlNbr);
        Assert.Equal("Central", single.Value);
    }

    [Fact]
    public async Task Set_AttributeValue_Upsert_Updates_Existing()
    {
        using var ctx = _factory.CreateContext();
        var groupTypeRepo = new GroupTypeRepository(ctx, _factory.CurrentUserService);
        var groupRepo = new DynamicGroupRepository(ctx, _factory.CurrentUserService);
        var attrDefRepo = new GroupAttributeDefinitionRepository(ctx, _factory.CurrentUserService);
        var attrValRepo = new GroupAttributeValueRepository(ctx, _factory.CurrentUserService);

        var groupType = GroupType.Create("Region", "Region", isWorkArea: false);
        await groupTypeRepo.AddAsync(groupType);

        var group = DynamicGroup.Create(groupType.CtrlNbr.Value, "Northeast", null, "/ne", false);
        await groupRepo.AddAsync(group);

        var attrDef = GroupAttributeDefinition.Create(groupType.CtrlNbr.Value, "Timezone", "string", false);
        await attrDefRepo.AddAsync(attrDef);

        // Create initial value
        var value = GroupAttributeValue.Create(group.CtrlNbr.Value, attrDef.CtrlNbr.Value, "Eastern");
        await attrValRepo.AddAsync(value);

        // Upsert — update existing
        value.Update("Pacific");
        await attrValRepo.UpdateAsync(value);

        var values = await attrValRepo.GetByGroupCtrlNbrAsync(group.CtrlNbr);

        var single = Assert.Single(values);
        Assert.Equal("Pacific", single.Value);
    }

    [Fact]
    public async Task GetByGroup_Returns_Only_Values_For_That_Group()
    {
        using var ctx = _factory.CreateContext();
        var groupTypeRepo = new GroupTypeRepository(ctx, _factory.CurrentUserService);
        var groupRepo = new DynamicGroupRepository(ctx, _factory.CurrentUserService);
        var attrDefRepo = new GroupAttributeDefinitionRepository(ctx, _factory.CurrentUserService);
        var attrValRepo = new GroupAttributeValueRepository(ctx, _factory.CurrentUserService);

        var groupType = GroupType.Create("Region", "Region", isWorkArea: false);
        await groupTypeRepo.AddAsync(groupType);

        var groupA = DynamicGroup.Create(groupType.CtrlNbr.Value, "Southeast", null, "/se", false);
        await groupRepo.AddAsync(groupA);

        var groupB = DynamicGroup.Create(groupType.CtrlNbr.Value, "Northeast", null, "/ne", false);
        await groupRepo.AddAsync(groupB);

        var attrDef = GroupAttributeDefinition.Create(groupType.CtrlNbr.Value, "Timezone", "string", false);
        await attrDefRepo.AddAsync(attrDef);

        await attrValRepo.AddAsync(GroupAttributeValue.Create(groupA.CtrlNbr.Value, attrDef.CtrlNbr.Value, "Eastern"));
        await attrValRepo.AddAsync(GroupAttributeValue.Create(groupB.CtrlNbr.Value, attrDef.CtrlNbr.Value, "Pacific"));

        var valuesA = await attrValRepo.GetByGroupCtrlNbrAsync(groupA.CtrlNbr);
        var valuesB = await attrValRepo.GetByGroupCtrlNbrAsync(groupB.CtrlNbr);

        Assert.Single(valuesA);
        Assert.Equal("Eastern", valuesA[0].Value);
        Assert.Single(valuesB);
        Assert.Equal("Pacific", valuesB[0].Value);
    }

    [Fact]
    public async Task Delete_AttributeValue_SoftDeletes()
    {
        using var ctx = _factory.CreateContext();
        var groupTypeRepo = new GroupTypeRepository(ctx, _factory.CurrentUserService);
        var groupRepo = new DynamicGroupRepository(ctx, _factory.CurrentUserService);
        var attrDefRepo = new GroupAttributeDefinitionRepository(ctx, _factory.CurrentUserService);
        var attrValRepo = new GroupAttributeValueRepository(ctx, _factory.CurrentUserService);

        var groupType = GroupType.Create("Yard", "Yard", isWorkArea: true);
        await groupTypeRepo.AddAsync(groupType);

        var group = DynamicGroup.Create(groupType.CtrlNbr.Value, "Jax Yard", null, "/jax", true);
        await groupRepo.AddAsync(group);

        var attrDef = GroupAttributeDefinition.Create(groupType.CtrlNbr.Value, "TrackCount", "int", true, "10");
        await attrDefRepo.AddAsync(attrDef);

        var value = GroupAttributeValue.Create(group.CtrlNbr.Value, attrDef.CtrlNbr.Value, "12");
        await attrValRepo.AddAsync(value);

        await attrValRepo.DeleteAsync(value.CtrlNbr);

        var remaining = await attrValRepo.GetByGroupCtrlNbrAsync(group.CtrlNbr);
        Assert.Empty(remaining);
    }
}
