using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.TenantConfig;

public sealed class TenantConfigService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    private static string? ValidateAndNormalizeWorkAreaTimeZone(bool isWorkArea, string? timeZoneId)
    {
        if (!isWorkArea)
            return null;

        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Work area timezone is required.", nameof(timeZoneId));

        var normalized = timeZoneId.Trim();
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(normalized);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Invalid work area timezone id '{normalized}'.", nameof(timeZoneId));
        }
        catch (InvalidTimeZoneException)
        {
            throw new ArgumentException($"Invalid work area timezone id '{normalized}'.", nameof(timeZoneId));
        }

        return normalized;
    }

    // ── Group Types ──────────────────────────────────────────────────────────

    public async Task<List<GroupType>> GetAllGroupTypesAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.GroupTypes.GetAllAsync(ct);
    }

    public async Task<GroupType> GetGroupTypeAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.GroupTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"GroupType {ctrlNbr.Value} not found.");
    }

    public async Task<GroupType> CreateGroupTypeAsync(
        string name, string description, bool isWorkArea, string flagsJson,
        ControlNumber? parentCtrlNbr, ControlNumber? railroadCtrlNbr, ControlNumber? parentGroupTypeCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.GroupTypes.GetByNameIncludingDeletedAsync(name);
        if (existing is not null)
        {
            if (!existing.IsDeleted)
                throw new InvalidOperationException($"GroupType '{name}' already exists.");
            existing.Restore();
            existing.Update(name, description, isWorkArea, flagsJson, parentCtrlNbr, railroadCtrlNbr, parentGroupTypeCtrlNbr);
            uow.GroupTypes.Update(existing);
            await uow.CommitAsync(ct);
            return existing;
        }

        var groupType = GroupType.Create(name, description, isWorkArea, flagsJson, parentCtrlNbr, railroadCtrlNbr, parentGroupTypeCtrlNbr);
        uow.GroupTypes.Add(groupType);
        await uow.CommitAsync(ct);
        return groupType;
    }

    public async Task<GroupType> UpdateGroupTypeAsync(
        ControlNumber ctrlNbr, string name, string description, bool isWorkArea, string flagsJson,
        ControlNumber? parentCtrlNbr, ControlNumber? railroadCtrlNbr, ControlNumber? parentGroupTypeCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var groupType = await uow.GroupTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"GroupType {ctrlNbr.Value} not found.");
        if (groupType.IsSystemType && !string.Equals(groupType.Name, name, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"System type '{groupType.Name}' cannot be renamed.");
        groupType.Update(name, description, isWorkArea, flagsJson, parentCtrlNbr, railroadCtrlNbr, parentGroupTypeCtrlNbr);
        uow.GroupTypes.Update(groupType);
        await uow.CommitAsync(ct);
        return groupType;
    }

    public async Task DeleteGroupTypeAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var groupType = await uow.GroupTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"GroupType {ctrlNbr.Value} not found.");
        if (groupType.IsSystemType)
            throw new InvalidOperationException($"System type '{groupType.Name}' cannot be deleted.");
        uow.GroupTypes.Remove(groupType);
        await uow.CommitAsync(ct);
    }

    // ── Groups ───────────────────────────────────────────────────────────────

    public async Task<List<DynamicGroup>> GetGroupsByTypeNameAsync(
        string typeName, ControlNumber? parentCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.DynamicGroups.GetByGroupTypeNameAsync(typeName, parentCtrlNbr);
    }

    public async Task<List<DynamicGroup>> GetAllGroupsAsync(ControlNumber? parentGroupCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.DynamicGroups.GetByParentCtrlNbrAsync(parentGroupCtrlNbr);
    }

    public async Task<DynamicGroup> GetGroupAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.DynamicGroups.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Group {ctrlNbr.Value} not found.");
    }

    public async Task<DynamicGroup> CreateGroupAsync(
        long groupTypeCtrlNbr, string name, long? parentGroupCtrlNbr, bool isWorkArea,
        string? code, ControlNumber? parentCtrlNbr, ControlNumber? railroadCtrlNbr,
        string? timeZoneId = null,
        CancellationToken ct = default)
    {
        timeZoneId = ValidateAndNormalizeWorkAreaTimeZone(isWorkArea, timeZoneId);

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        string? parentPath = null;
        if (parentGroupCtrlNbr > 0)
        {
            var parentGroup = await uow.DynamicGroups.GetByCtrlNbrAsync(ControlNumber.Create(parentGroupCtrlNbr!.Value), ct);
            parentPath = parentGroup?.Path;
        }

        var existing = await uow.DynamicGroups.GetByGroupTypeAndNameIncludingDeletedAsync(
            ControlNumber.Create(groupTypeCtrlNbr), name);

        if (existing is not null)
        {
            if (!existing.IsDeleted)
                throw new InvalidOperationException($"Group '{name}' already exists.");
            existing.Restore();
            existing.Update(name, parentGroupCtrlNbr > 0 ? ControlNumber.Create(parentGroupCtrlNbr!.Value) : null,
                null, isWorkArea, code, parentCtrlNbr, railroadCtrlNbr, timeZoneId);
            existing.BuildPath(parentPath);
            uow.DynamicGroups.Update(existing);
            await uow.CommitAsync(ct);
            return existing;
        }

        var group = DynamicGroup.Create(groupTypeCtrlNbr, name,
            parentGroupCtrlNbr > 0 ? parentGroupCtrlNbr : null,
            null, isWorkArea, code, parentCtrlNbr, railroadCtrlNbr, timeZoneId);
        group.BuildPath(parentPath);
        uow.DynamicGroups.Add(group);

        if (group.IsWorkArea && group.RailroadCtrlNbr is not null)
        {
            var crafts = await uow.Crafts.GetByParentAndRailroadAsync(null, group.RailroadCtrlNbr);
            foreach (var craft in crafts)
            {
                var roster = Roster.Create(craft.CtrlNbr, group.CtrlNbr,
                    railroadPayrollDepartmentCtrlNbr: null, craft.CraftName, craft.CraftPluralName, rosterNumber: 1);
                uow.Rosters.Add(roster);

                uow.RosterBoards.Add(RosterBoard.Create(craft.CtrlNbr, roster.CtrlNbr,
                    $"{craft.CraftName} Extra Board", BoardType.ExtraBoard, RotationType.FirstInFirstOut));

                uow.RosterBoards.Add(RosterBoard.Create(craft.CtrlNbr, roster.CtrlNbr,
                    $"{craft.CraftName} Hangout", BoardType.Hangout));
            }
        }

        await uow.CommitAsync(ct);
        return group;
    }

    public async Task<DynamicGroup> UpdateGroupAsync(
        ControlNumber ctrlNbr, string name, long? parentGroupCtrlNbr, bool isWorkArea,
        string? code, ControlNumber? parentCtrlNbr, ControlNumber? railroadCtrlNbr,
        string? timeZoneId = null,
        CancellationToken ct = default)
    {
        timeZoneId = ValidateAndNormalizeWorkAreaTimeZone(isWorkArea, timeZoneId);

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var group = await uow.DynamicGroups.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Group {ctrlNbr.Value} not found.");

        var oldParentCtrlNbr = group.ParentGroupCtrlNbr;

        string? parentPath = null;
        if (parentGroupCtrlNbr > 0)
        {
            var parentGroup = await uow.DynamicGroups.GetByCtrlNbrAsync(ControlNumber.Create(parentGroupCtrlNbr!.Value), ct);
            parentPath = parentGroup?.Path;
        }

        group.Update(name, parentGroupCtrlNbr > 0 ? ControlNumber.Create(parentGroupCtrlNbr!.Value) : null,
            null, isWorkArea, code, parentCtrlNbr, railroadCtrlNbr, timeZoneId);
        group.BuildPath(parentPath);
        uow.DynamicGroups.Update(group);

        await RebuildDescendantPathsAsync(group, uow);

        await uow.CommitAsync(ct);
        return group;
    }

    public async Task DeleteGroupAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var group = await uow.DynamicGroups.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Group {ctrlNbr.Value} not found.");
        uow.DynamicGroups.Remove(group);
        await uow.CommitAsync(ct);
    }

    // ── Tree Queries ─────────────────────────────────────────────────────────

    public async Task<List<DynamicGroup>> GetGroupTreeAsync(ControlNumber? rootCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.DynamicGroups.GetTreeAsync(rootCtrlNbr);
    }

    public async Task<List<DynamicGroup>> GetWorkAreasAsync(ControlNumber? railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.DynamicGroups.GetWorkAreasAsync(railroadCtrlNbr);
    }

    public async Task<List<DynamicGroup>> GetWorkAreaTreeAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.DynamicGroups.GetWorkAreasWithDescendantsAsync();
    }

    public async Task<List<DynamicGroup>> GetAncestorsAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.DynamicGroups.GetAncestorsAsync(ctrlNbr);
    }

    // ── Attribute Definitions ────────────────────────────────────────────────

    public async Task<List<GroupAttributeDefinition>> GetAttributeDefinitionsAsync(
        ControlNumber groupTypeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.AttributeDefinitions.GetByGroupTypeCtrlNbrAsync(groupTypeCtrlNbr);
    }

    public async Task<GroupAttributeDefinition> GetAttributeDefinitionAsync(
        ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.AttributeDefinitions.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"AttributeDefinition {ctrlNbr.Value} not found.");
    }

    public async Task<GroupAttributeDefinition> CreateAttributeDefinitionAsync(
        long groupTypeCtrlNbr, string attributeName, string dataType, bool isRequired, string? defaultValue,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        _ = await uow.GroupTypes.GetByCtrlNbrAsync(ControlNumber.Create(groupTypeCtrlNbr), ct)
            ?? throw new KeyNotFoundException($"GroupType {groupTypeCtrlNbr} not found.");

        var existing = await uow.AttributeDefinitions.GetByGroupTypeAndAttributeNameIncludingDeletedAsync(
            ControlNumber.Create(groupTypeCtrlNbr), attributeName);
        if (existing is not null)
        {
            if (!existing.IsDeleted)
                throw new InvalidOperationException($"Attribute '{attributeName}' already exists.");
            existing.Restore();
            existing.Update(attributeName, dataType, isRequired, defaultValue);
            uow.AttributeDefinitions.Update(existing);
            await uow.CommitAsync(ct);
            return existing;
        }

        var definition = GroupAttributeDefinition.Create(groupTypeCtrlNbr, attributeName, dataType, isRequired, defaultValue);
        uow.AttributeDefinitions.Add(definition);
        await uow.CommitAsync(ct);
        return definition;
    }

    public async Task<GroupAttributeDefinition> UpdateAttributeDefinitionAsync(
        ControlNumber ctrlNbr, string attributeName, string dataType, bool isRequired, string? defaultValue,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var definition = await uow.AttributeDefinitions.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"AttributeDefinition {ctrlNbr.Value} not found.");
        definition.Update(attributeName, dataType, isRequired, defaultValue);
        uow.AttributeDefinitions.Update(definition);
        await uow.CommitAsync(ct);
        return definition;
    }

    public async Task DeleteAttributeDefinitionAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var definition = await uow.AttributeDefinitions.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"AttributeDefinition {ctrlNbr.Value} not found.");
        uow.AttributeDefinitions.Remove(definition);
        await uow.CommitAsync(ct);
    }

    // ── Attribute Values ─────────────────────────────────────────────────────

    public async Task<List<GroupAttributeValue>> GetAttributeValuesAsync(
        ControlNumber groupCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.AttributeValues.GetByGroupCtrlNbrAsync(groupCtrlNbr);
    }

    public async Task<GroupAttributeValue> SetAttributeValueAsync(
        long groupCtrlNbr, long attributeDefinitionCtrlNbr, string? value, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        _ = await uow.DynamicGroups.GetByCtrlNbrAsync(ControlNumber.Create(groupCtrlNbr), ct)
            ?? throw new KeyNotFoundException($"Group {groupCtrlNbr} not found.");
        _ = await uow.AttributeDefinitions.GetByCtrlNbrAsync(ControlNumber.Create(attributeDefinitionCtrlNbr), ct)
            ?? throw new KeyNotFoundException($"AttributeDefinition {attributeDefinitionCtrlNbr} not found.");

        var existing = (await uow.AttributeValues.GetByGroupCtrlNbrAsync(ControlNumber.Create(groupCtrlNbr)))
            .FirstOrDefault(v => v.AttributeDefinitionCtrlNbr == ControlNumber.Create(attributeDefinitionCtrlNbr));

        if (existing is not null)
        {
            existing.Update(value);
            uow.AttributeValues.Update(existing);
            await uow.CommitAsync(ct);
            return existing;
        }

        var av = GroupAttributeValue.Create(groupCtrlNbr, attributeDefinitionCtrlNbr, value);
        uow.AttributeValues.Add(av);
        await uow.CommitAsync(ct);
        return av;
    }

    public async Task DeleteAttributeValueAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var av = await uow.AttributeValues.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"AttributeValue {ctrlNbr.Value} not found.");
        uow.AttributeValues.Remove(av);
        await uow.CommitAsync(ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task RebuildDescendantPathsAsync(DynamicGroup parent, IOrchestrationUnitOfWork uow)
    {
        var queue = new Queue<DynamicGroup>();
        queue.Enqueue(parent);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var children = await uow.DynamicGroups.GetByParentCtrlNbrAsync(current.CtrlNbr);
            foreach (var child in children)
            {
                child.BuildPath(current.Path);
                uow.DynamicGroups.Update(child);
                queue.Enqueue(child);
            }
        }
    }
}
