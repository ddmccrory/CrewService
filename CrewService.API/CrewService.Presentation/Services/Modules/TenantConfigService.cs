using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class TenantConfigService(
    IGroupTypeRepository groupTypeRepository,
    IDynamicGroupRepository dynamicGroupRepository,
    IGroupAttributeDefinitionRepository attributeDefinitionRepository,
    IGroupAttributeValueRepository attributeValueRepository,
    IOrchestrationUnitOfWorkFactory uowFactory) : TenantConfigSrvc.TenantConfigSrvcBase
{
    private readonly IGroupTypeRepository _groupTypeRepository = groupTypeRepository;
    private readonly IDynamicGroupRepository _dynamicGroupRepository = dynamicGroupRepository;
    private readonly IGroupAttributeDefinitionRepository _attributeDefinitionRepository = attributeDefinitionRepository;
    private readonly IGroupAttributeValueRepository _attributeValueRepository = attributeValueRepository;
    private readonly IOrchestrationUnitOfWorkFactory _uowFactory = uowFactory;

    // GroupTypes
    public override async Task<GetAllGroupTypesResponse> GetAllGroupTypes(GetAllGroupTypesRequest request, ServerCallContext context)
    {
        var response = new GetAllGroupTypesResponse();
        var groupTypes = await _groupTypeRepository.GetAllAsync();

        foreach (var gt in groupTypes)
        {
            response.GroupTypes.Add(MapGroupType(gt));
        }

        response.TotalCount = groupTypes.Count;
        return response;
    }

    public override async Task<GroupTypeResponse> GetGroupType(GetGroupTypeRequest request, ServerCallContext context)
    {
        var groupType = await _groupTypeRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"GroupType {request.CtrlNbr} not found."));

        return MapGroupType(groupType);
    }

    public override async Task<GroupTypeResponse> CreateGroupType(CreateGroupTypeRequest request, ServerCallContext context)
    {
        var existing = await _groupTypeRepository.GetByNameIncludingDeletedAsync(request.Name, request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null);

        if (existing is not null)
        {
            if (!existing.IsDeleted)
                throw new RpcException(new Status(StatusCode.AlreadyExists, $"GroupType '{request.Name}' already exists."));

            existing.Restore();
            existing.Update(request.Name, request.Description, request.IsWorkArea, request.FlagsJson,
                request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
                request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null,
                request.ParentGroupTypeCtrlNbr > 0 ? ControlNumber.Create(request.ParentGroupTypeCtrlNbr) : null);

            await using var uow = await _uowFactory.CreateAsync();
            uow.GroupTypes.Update(existing);
            await uow.CommitAsync();

            return MapGroupType(existing);
        }

        var groupType = GroupType.Create(request.Name, request.Description, request.IsWorkArea, request.FlagsJson,
            request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
            request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null,
            request.ParentGroupTypeCtrlNbr > 0 ? ControlNumber.Create(request.ParentGroupTypeCtrlNbr) : null);

        await using var uow2 = await _uowFactory.CreateAsync();
        uow2.GroupTypes.Add(groupType);
        await uow2.CommitAsync();

        return MapGroupType(groupType);
    }

    public override async Task<GroupTypeResponse> UpdateGroupType(UpdateGroupTypeRequest request, ServerCallContext context)
    {
        var groupType = await _groupTypeRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"GroupType {request.CtrlNbr} not found."));

        if (groupType.IsSystemType && !string.Equals(groupType.Name, request.Name, StringComparison.OrdinalIgnoreCase))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"System type '{groupType.Name}' cannot be renamed."));

        groupType.Update(request.Name, request.Description, request.IsWorkArea, request.FlagsJson,
            request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
            request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null,
            request.ParentGroupTypeCtrlNbr > 0 ? ControlNumber.Create(request.ParentGroupTypeCtrlNbr) : null);

        await using var uow = await _uowFactory.CreateAsync();
        uow.GroupTypes.Update(groupType);
        await uow.CommitAsync();

        return MapGroupType(groupType);
    }

    public override async Task<DeleteResponse> DeleteGroupType(DeleteGroupTypeRequest request, ServerCallContext context)
    {
        var groupType = await _groupTypeRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"GroupType {request.CtrlNbr} not found."));

        if (groupType.IsSystemType)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"System type '{groupType.Name}' cannot be deleted."));

        await using var uow = await _uowFactory.CreateAsync();
        uow.GroupTypes.Remove(groupType);
        await uow.CommitAsync();

        return new DeleteResponse { Success = true };
    }

    // Groups
    public override async Task<GetAllGroupsResponse> GetGroupsByTypeName(GetGroupsByTypeNameRequest request, ServerCallContext context)
    {
        var response = new GetAllGroupsResponse();
        var groups = await _dynamicGroupRepository.GetByGroupTypeNameAsync(request.TypeName, request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null);

        foreach (var g in groups)
            response.Groups.Add(MapGroup(g));

        response.TotalCount = groups.Count;
        return response;
    }

    public override async Task<GetAllGroupsResponse> GetAllGroups(GetAllGroupsRequest request, ServerCallContext context)
    {
        var response = new GetAllGroupsResponse();
        var parentCtrlNbr = request.ParentGroupCtrlNbr > 0
            ? ControlNumber.Create(request.ParentGroupCtrlNbr)
            : null;

        var groups = await _dynamicGroupRepository.GetByParentCtrlNbrAsync(parentCtrlNbr);

        foreach (var g in groups)
        {
            response.Groups.Add(MapGroup(g));
        }

        response.TotalCount = groups.Count;
        return response;
    }

    public override async Task<GroupResponse> GetGroup(GetGroupRequest request, ServerCallContext context)
    {
        var group = await _dynamicGroupRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Group {request.CtrlNbr} not found."));

        return MapGroup(group);
    }

    public override async Task<GroupResponse> CreateGroup(CreateGroupRequest request, ServerCallContext context)
    {
        // Enforce Assignment hierarchy: must be under a work area or its descendant
        var groupType = await _groupTypeRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.GroupTypeCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"GroupType {request.GroupTypeCtrlNbr} not found."));
        if (string.Equals(groupType.Name, "Assignment", StringComparison.OrdinalIgnoreCase))
            await ValidateAssignmentParentAsync(request.ParentGroupCtrlNbr);

        // Resolve parent's path for materialized path computation
        string? parentPath = null;
        if (request.ParentGroupCtrlNbr > 0)
        {
            var parentGroup = await _dynamicGroupRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.ParentGroupCtrlNbr));
            parentPath = parentGroup?.Path;
        }

        var existing = await _dynamicGroupRepository.GetByGroupTypeAndNameIncludingDeletedAsync(
            ControlNumber.Create(request.GroupTypeCtrlNbr), request.Name);

        if (existing is not null)
        {
            if (!existing.IsDeleted)
                throw new RpcException(new Status(StatusCode.AlreadyExists, $"Group '{request.Name}' already exists."));

            existing.Restore();
            existing.Update(
                request.Name,
                request.ParentGroupCtrlNbr > 0 ? ControlNumber.Create(request.ParentGroupCtrlNbr) : null,
                null,
                request.IsWorkArea,
                string.IsNullOrEmpty(request.Code) ? null : request.Code,
                request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
                request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null);
            existing.BuildPath(parentPath);

            await using var uow = await _uowFactory.CreateAsync();
            uow.DynamicGroups.Update(existing);
            await uow.CommitAsync();

            return MapGroup(existing);
        }

        var group = DynamicGroup.Create(
            request.GroupTypeCtrlNbr,
            request.Name,
            request.ParentGroupCtrlNbr > 0 ? request.ParentGroupCtrlNbr : null,
            null,
            request.IsWorkArea,
            string.IsNullOrEmpty(request.Code) ? null : request.Code,
            request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
            request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null);
        group.BuildPath(parentPath);

        await using var uow2 = await _uowFactory.CreateAsync();
        uow2.DynamicGroups.Add(group);
        await uow2.CommitAsync();

        return MapGroup(group);
    }

    public override async Task<GroupResponse> UpdateGroup(UpdateGroupRequest request, ServerCallContext context)
    {
        var group = await _dynamicGroupRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Group {request.CtrlNbr} not found."));

        // Enforce Assignment hierarchy: must be under a work area or its descendant
        var groupType = await _groupTypeRepository.GetByCtrlNbrAsync(group.GroupTypeCtrlNbr);
        if (groupType is not null && string.Equals(groupType.Name, "Assignment", StringComparison.OrdinalIgnoreCase))
            await ValidateAssignmentParentAsync(request.ParentGroupCtrlNbr);

        var oldParentCtrlNbr = group.ParentGroupCtrlNbr;

        // Resolve new parent's path
        string? parentPath = null;
        if (request.ParentGroupCtrlNbr > 0)
        {
            var parentGroup = await _dynamicGroupRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.ParentGroupCtrlNbr));
            parentPath = parentGroup?.Path;
        }

        group.Update(
            request.Name,
            request.ParentGroupCtrlNbr > 0 ? ControlNumber.Create(request.ParentGroupCtrlNbr) : null,
            null,
            request.IsWorkArea,
            string.IsNullOrEmpty(request.Code) ? null : request.Code,
            request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
            request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null);
        group.BuildPath(parentPath);

        await using var uow = await _uowFactory.CreateAsync();
        uow.DynamicGroups.Update(group);

        // If parent changed, cascade path updates to all descendants
        var newParentCtrlNbr = request.ParentGroupCtrlNbr > 0
            ? ControlNumber.Create(request.ParentGroupCtrlNbr)
            : (ControlNumber?)null;
        if (oldParentCtrlNbr != newParentCtrlNbr)
        {
            await RebuildDescendantPaths(group, uow);
        }

        await uow.CommitAsync();

        return MapGroup(group);
    }

    public override async Task<DeleteResponse> DeleteGroup(DeleteGroupRequest request, ServerCallContext context)
    {
        var group = await _dynamicGroupRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Group {request.CtrlNbr} not found."));

        await using var uow = await _uowFactory.CreateAsync();
        uow.DynamicGroups.Remove(group);
        await uow.CommitAsync();

        return new DeleteResponse { Success = true };
    }

    // Tree queries
    public override async Task<GetAllGroupsResponse> GetGroupTree(GetGroupTreeRequest request, ServerCallContext context)
    {
        var response = new GetAllGroupsResponse();
        var rootCtrlNbr = request.RootCtrlNbr > 0 ? ControlNumber.Create(request.RootCtrlNbr) : null;
        var groups = await _dynamicGroupRepository.GetTreeAsync(rootCtrlNbr);

        foreach (var g in groups)
            response.Groups.Add(MapGroup(g));

        response.TotalCount = groups.Count;
        return response;
    }

    public override async Task<GetAllGroupsResponse> GetWorkAreas(GetWorkAreasRequest request, ServerCallContext context)
    {
        var response = new GetAllGroupsResponse();
        var workAreas = await _dynamicGroupRepository.GetWorkAreasAsync();

        foreach (var g in workAreas)
            response.Groups.Add(MapGroup(g));

        response.TotalCount = workAreas.Count;
        return response;
    }

    public override async Task<GetAllGroupsResponse> GetWorkAreaTree(GetWorkAreasRequest request, ServerCallContext context)
    {
        var response = new GetAllGroupsResponse();
        var groups = await _dynamicGroupRepository.GetWorkAreasWithDescendantsAsync();

        foreach (var g in groups)
            response.Groups.Add(MapGroup(g));

        response.TotalCount = groups.Count;
        return response;
    }

    public override async Task<GetAllGroupsResponse> GetAncestors(GetAncestorsRequest request, ServerCallContext context)
    {
        var response = new GetAllGroupsResponse();
        var ancestors = await _dynamicGroupRepository.GetAncestorsAsync(ControlNumber.Create(request.CtrlNbr));

        foreach (var g in ancestors)
            response.Groups.Add(MapGroup(g));

        response.TotalCount = ancestors.Count;
        return response;
    }

    // Attribute Definitions
    public override async Task<GetAttributeDefinitionsResponse> GetAttributeDefinitions(GetAttributeDefinitionsRequest request, ServerCallContext context)
    {
        var definitions = await _attributeDefinitionRepository.GetByGroupTypeCtrlNbrAsync(
            ControlNumber.Create(request.GroupTypeCtrlNbr));

        var response = new GetAttributeDefinitionsResponse();
        foreach (var ad in definitions)
            response.AttributeDefinitions.Add(MapAttributeDefinition(ad));

        return response;
    }

    public override async Task<AttributeDefinitionResponse> GetAttributeDefinition(GetAttributeDefinitionRequest request, ServerCallContext context)
    {
        var definition = await _attributeDefinitionRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"AttributeDefinition {request.CtrlNbr} not found."));

        return MapAttributeDefinition(definition);
    }

    public override async Task<AttributeDefinitionResponse> CreateAttributeDefinition(CreateAttributeDefinitionRequest request, ServerCallContext context)
    {
        _ = await _groupTypeRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.GroupTypeCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"GroupType {request.GroupTypeCtrlNbr} not found."));

        var existing = await _attributeDefinitionRepository.GetByGroupTypeAndAttributeNameIncludingDeletedAsync(
            ControlNumber.Create(request.GroupTypeCtrlNbr), request.AttributeName);

        if (existing is not null)
        {
            if (!existing.IsDeleted)
                throw new RpcException(new Status(StatusCode.AlreadyExists, $"Attribute '{request.AttributeName}' already exists."));

            existing.Restore();
            existing.Update(
                request.AttributeName,
                request.DataType,
                request.IsRequired,
                string.IsNullOrEmpty(request.DefaultValue) ? null : request.DefaultValue);

            await using var uow = await _uowFactory.CreateAsync();
            uow.AttributeDefinitions.Update(existing);
            await uow.CommitAsync();

            return MapAttributeDefinition(existing);
        }

        var definition = GroupAttributeDefinition.Create(
            request.GroupTypeCtrlNbr,
            request.AttributeName,
            request.DataType,
            request.IsRequired,
            string.IsNullOrEmpty(request.DefaultValue) ? null : request.DefaultValue);

        await using var uow2 = await _uowFactory.CreateAsync();
        uow2.AttributeDefinitions.Add(definition);
        await uow2.CommitAsync();

        return MapAttributeDefinition(definition);
    }

    public override async Task<AttributeDefinitionResponse> UpdateAttributeDefinition(UpdateAttributeDefinitionRequest request, ServerCallContext context)
    {
        var definition = await _attributeDefinitionRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"AttributeDefinition {request.CtrlNbr} not found."));

        definition.Update(
            request.AttributeName,
            request.DataType,
            request.IsRequired,
            string.IsNullOrEmpty(request.DefaultValue) ? null : request.DefaultValue);

        await using var uow = await _uowFactory.CreateAsync();
        uow.AttributeDefinitions.Update(definition);
        await uow.CommitAsync();

        return MapAttributeDefinition(definition);
    }

    public override async Task<DeleteResponse> DeleteAttributeDefinition(DeleteAttributeDefinitionRequest request, ServerCallContext context)
    {
        var definition = await _attributeDefinitionRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"AttributeDefinition {request.CtrlNbr} not found."));

        await using var uow = await _uowFactory.CreateAsync();
        uow.AttributeDefinitions.Remove(definition);
        await uow.CommitAsync();

        return new DeleteResponse { Success = true };
    }

    // Attribute Values
    public override async Task<GetAttributeValuesResponse> GetAttributeValues(GetAttributeValuesRequest request, ServerCallContext context)
    {
        var values = await _attributeValueRepository.GetByGroupCtrlNbrAsync(
            ControlNumber.Create(request.GroupCtrlNbr));

        var response = new GetAttributeValuesResponse();
        foreach (var av in values)
            response.AttributeValues.Add(MapAttributeValue(av));

        return response;
    }

    public override async Task<AttributeValueResponse> SetAttributeValue(SetAttributeValueRequest request, ServerCallContext context)
    {
        _ = await _dynamicGroupRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.GroupCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Group {request.GroupCtrlNbr} not found."));

        _ = await _attributeDefinitionRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.AttributeDefinitionCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"AttributeDefinition {request.AttributeDefinitionCtrlNbr} not found."));

        var existing = (await _attributeValueRepository.GetByGroupCtrlNbrAsync(ControlNumber.Create(request.GroupCtrlNbr)))
            .FirstOrDefault(v => v.AttributeDefinitionCtrlNbr == ControlNumber.Create(request.AttributeDefinitionCtrlNbr));

        if (existing is not null)
        {
            existing.Update(string.IsNullOrEmpty(request.Value) ? null : request.Value);

            await using var uow = await _uowFactory.CreateAsync();
            uow.AttributeValues.Update(existing);
            await uow.CommitAsync();

            return MapAttributeValue(existing);
        }

        var value = GroupAttributeValue.Create(
            request.GroupCtrlNbr,
            request.AttributeDefinitionCtrlNbr,
            string.IsNullOrEmpty(request.Value) ? null : request.Value);

        await using var uow2 = await _uowFactory.CreateAsync();
        uow2.AttributeValues.Add(value);
        await uow2.CommitAsync();

        return MapAttributeValue(value);
    }

    public override async Task<DeleteResponse> DeleteAttributeValue(DeleteAttributeValueRequest request, ServerCallContext context)
    {
        var attributeValue = await _attributeValueRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"AttributeValue {request.CtrlNbr} not found."));

        await using var uow = await _uowFactory.CreateAsync();
        uow.AttributeValues.Remove(attributeValue);
        await uow.CommitAsync();

        return new DeleteResponse { Success = true };
    }

    /// <summary>
    /// Validates that an Assignment group is placed under a work area or a descendant of a work area.
    /// </summary>
    private async Task ValidateAssignmentParentAsync(long parentGroupCtrlNbr)
    {
        if (parentGroupCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "Assignment groups must be placed under a work area or its descendant."));

        var parent = await _dynamicGroupRepository.GetByCtrlNbrAsync(ControlNumber.Create(parentGroupCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Parent group {parentGroupCtrlNbr} not found."));

        if (parent.IsWorkArea)
            return;

        var ancestors = await _dynamicGroupRepository.GetAncestorsAsync(parent.CtrlNbr);
        if (ancestors.Any(a => a.IsWorkArea))
            return;

        throw new RpcException(new Status(StatusCode.InvalidArgument,
            "Assignment groups must be placed under a work area or its descendant."));
    }

    /// <summary>
    /// BFS cascade: recomputes materialized paths for all descendants of the given group.
    /// </summary>
    private async Task RebuildDescendantPaths(DynamicGroup parent, IOrchestrationUnitOfWork uow)
    {
        var queue = new Queue<DynamicGroup>();
        queue.Enqueue(parent);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var children = await _dynamicGroupRepository.GetByParentCtrlNbrAsync(current.CtrlNbr);

            foreach (var child in children)
            {
                child.BuildPath(current.Path);
                uow.DynamicGroups.Update(child);
                queue.Enqueue(child);
            }
        }
    }

    private static GroupTypeResponse MapGroupType(GroupType gt) => new()
    {
        CtrlNbr = gt.CtrlNbr.Value,
        Name = gt.Name,
        Description = gt.Description ?? string.Empty,
        IsWorkArea = gt.IsWorkArea,
        FlagsJson = gt.FlagsJson ?? string.Empty,
        ParentCtrlNbr = gt.ParentCtrlNbr?.Value ?? 0,
        RailroadCtrlNbr = gt.RailroadCtrlNbr?.Value ?? 0,
        ParentGroupTypeCtrlNbr = gt.ParentGroupTypeCtrlNbr?.Value ?? 0,
        IsSystemType = gt.IsSystemType
    };

    private static GroupResponse MapGroup(DynamicGroup g) => new()
    {
        CtrlNbr = g.CtrlNbr.Value,
        GroupTypeCtrlNbr = g.GroupTypeCtrlNbr.Value,
        Name = g.Name,
        Code = g.Code ?? string.Empty,
        ParentGroupCtrlNbr = g.ParentGroupCtrlNbr?.Value ?? 0,
        Path = g.Path ?? string.Empty,
        IsWorkArea = g.IsWorkArea,
        ParentCtrlNbr = g.ParentCtrlNbr?.Value ?? 0,
        RailroadCtrlNbr = g.RailroadCtrlNbr?.Value ?? 0
    };

    private static AttributeDefinitionResponse MapAttributeDefinition(GroupAttributeDefinition ad) => new()
    {
        CtrlNbr = ad.CtrlNbr.Value,
        GroupTypeCtrlNbr = ad.GroupTypeCtrlNbr.Value,
        AttributeName = ad.AttributeName,
        DataType = ad.DataType,
        IsRequired = ad.IsRequired,
        DefaultValue = ad.DefaultValue ?? string.Empty
    };

    private static AttributeValueResponse MapAttributeValue(GroupAttributeValue av) => new()
    {
        CtrlNbr = av.CtrlNbr.Value,
        GroupCtrlNbr = av.GroupCtrlNbr.Value,
        AttributeDefinitionCtrlNbr = av.AttributeDefinitionCtrlNbr.Value,
        Value = av.Value ?? string.Empty
    };
}
