using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class TenantConfigService(
    IGroupTypeRepository groupTypeRepository,
    IDynamicGroupRepository dynamicGroupRepository,
    IRailroadGroupPlacementRepository railroadGroupPlacementRepository,
    IRailroadRepository railroadRepository,
    IGroupAttributeDefinitionRepository attributeDefinitionRepository,
    IGroupAttributeValueRepository attributeValueRepository) : TenantConfigSrvc.TenantConfigSrvcBase
{
    private readonly IGroupTypeRepository _groupTypeRepository = groupTypeRepository;
    private readonly IDynamicGroupRepository _dynamicGroupRepository = dynamicGroupRepository;
    private readonly IRailroadGroupPlacementRepository _railroadGroupPlacementRepository = railroadGroupPlacementRepository;
    private readonly IRailroadRepository _railroadRepository = railroadRepository;
    private readonly IGroupAttributeDefinitionRepository _attributeDefinitionRepository = attributeDefinitionRepository;
    private readonly IGroupAttributeValueRepository _attributeValueRepository = attributeValueRepository;

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
        var groupType = GroupType.Create(request.Name, request.Description, request.IsWorkArea, request.FlagsJson);
        await _groupTypeRepository.AddAsync(groupType);
        return MapGroupType(groupType);
    }

    public override async Task<GroupTypeResponse> UpdateGroupType(UpdateGroupTypeRequest request, ServerCallContext context)
    {
        var groupType = await _groupTypeRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"GroupType {request.CtrlNbr} not found."));

        groupType.Update(request.Name, request.Description, request.IsWorkArea, request.FlagsJson);
        await _groupTypeRepository.UpdateAsync(groupType);
        return MapGroupType(groupType);
    }

    public override async Task<DeleteResponse> DeleteGroupType(DeleteGroupTypeRequest request, ServerCallContext context)
    {
        await _groupTypeRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    // Groups
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
        var group = DynamicGroup.Create(
            request.GroupTypeCtrlNbr,
            request.Name,
            request.ParentGroupCtrlNbr > 0 ? request.ParentGroupCtrlNbr : null,
            request.Path,
            request.IsWorkArea);

        await _dynamicGroupRepository.AddAsync(group);
        return MapGroup(group);
    }

    public override async Task<GroupResponse> UpdateGroup(UpdateGroupRequest request, ServerCallContext context)
    {
        var group = await _dynamicGroupRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Group {request.CtrlNbr} not found."));

        group.Update(
            request.Name,
            request.ParentGroupCtrlNbr > 0 ? ControlNumber.Create(request.ParentGroupCtrlNbr) : null,
            request.Path,
            request.IsWorkArea);

        await _dynamicGroupRepository.UpdateAsync(group);
        return MapGroup(group);
    }

    public override async Task<DeleteResponse> DeleteGroup(DeleteGroupRequest request, ServerCallContext context)
    {
        await _dynamicGroupRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
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

    public override async Task<GetAllGroupsResponse> GetAncestors(GetAncestorsRequest request, ServerCallContext context)
    {
        var response = new GetAllGroupsResponse();
        var ancestors = await _dynamicGroupRepository.GetAncestorsAsync(ControlNumber.Create(request.CtrlNbr));

        foreach (var g in ancestors)
            response.Groups.Add(MapGroup(g));

        response.TotalCount = ancestors.Count;
        return response;
    }

    // Railroad Group Placements
    public override async Task<RailroadGroupPlacementResponse> PlaceRailroadInGroup(PlaceRailroadInGroupRequest request, ServerCallContext context)
    {
        _ = await _railroadRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.RailroadCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Railroad {request.RailroadCtrlNbr} not found."));

        _ = await _dynamicGroupRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.GroupCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Group {request.GroupCtrlNbr} not found."));

        var existing = await _railroadGroupPlacementRepository.GetByRailroadAndGroupAsync(
            ControlNumber.Create(request.RailroadCtrlNbr),
            ControlNumber.Create(request.GroupCtrlNbr));

        if (existing is not null)
            throw new RpcException(new Status(StatusCode.AlreadyExists, $"Railroad {request.RailroadCtrlNbr} is already placed in group {request.GroupCtrlNbr}."));

        var placement = RailroadGroupPlacement.Create(request.RailroadCtrlNbr, request.GroupCtrlNbr);
        await _railroadGroupPlacementRepository.AddAsync(placement);
        return MapPlacement(placement);
    }

    public override async Task<DeleteResponse> RemoveRailroadFromGroup(RemoveRailroadFromGroupRequest request, ServerCallContext context)
    {
        var placement = await _railroadGroupPlacementRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Placement {request.CtrlNbr} not found."));

        placement.Remove();
        await _railroadGroupPlacementRepository.UpdateAsync(placement);
        return new DeleteResponse { Success = true };
    }

    public override async Task<GetRailroadPlacementsResponse> GetRailroadPlacements(GetRailroadPlacementsRequest request, ServerCallContext context)
    {
        var placements = await _railroadGroupPlacementRepository.GetByRailroadCtrlNbrAsync(
            ControlNumber.Create(request.RailroadCtrlNbr));

        var response = new GetRailroadPlacementsResponse();
        foreach (var p in placements)
            response.Placements.Add(MapPlacement(p));

        return response;
    }

    public override async Task<GetRailroadPlacementsResponse> GetRailroadsInGroup(GetRailroadsInGroupRequest request, ServerCallContext context)
    {
        List<RailroadGroupPlacement> placements;

        if (request.IncludeDescendants)
        {
            var group = await _dynamicGroupRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.GroupCtrlNbr))
                ?? throw new RpcException(new Status(StatusCode.NotFound, $"Group {request.GroupCtrlNbr} not found."));

            placements = group.Path is not null
                ? await _railroadGroupPlacementRepository.GetByGroupSubtreeAsync(group.Path)
                : await _railroadGroupPlacementRepository.GetByGroupCtrlNbrAsync(ControlNumber.Create(request.GroupCtrlNbr));
        }
        else
        {
            placements = await _railroadGroupPlacementRepository.GetByGroupCtrlNbrAsync(
                ControlNumber.Create(request.GroupCtrlNbr));
        }

        var response = new GetRailroadPlacementsResponse();
        foreach (var p in placements)
            response.Placements.Add(MapPlacement(p));

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

        var definition = GroupAttributeDefinition.Create(
            request.GroupTypeCtrlNbr,
            request.AttributeName,
            request.DataType,
            request.IsRequired,
            string.IsNullOrEmpty(request.DefaultValue) ? null : request.DefaultValue);

        await _attributeDefinitionRepository.AddAsync(definition);
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

        await _attributeDefinitionRepository.UpdateAsync(definition);
        return MapAttributeDefinition(definition);
    }

    public override async Task<DeleteResponse> DeleteAttributeDefinition(DeleteAttributeDefinitionRequest request, ServerCallContext context)
    {
        await _attributeDefinitionRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
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
            await _attributeValueRepository.UpdateAsync(existing);
            return MapAttributeValue(existing);
        }

        var value = GroupAttributeValue.Create(
            request.GroupCtrlNbr,
            request.AttributeDefinitionCtrlNbr,
            string.IsNullOrEmpty(request.Value) ? null : request.Value);

        await _attributeValueRepository.AddAsync(value);
        return MapAttributeValue(value);
    }

    public override async Task<DeleteResponse> DeleteAttributeValue(DeleteAttributeValueRequest request, ServerCallContext context)
    {
        await _attributeValueRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    private static GroupTypeResponse MapGroupType(GroupType gt) => new()
    {
        CtrlNbr = gt.CtrlNbr.Value,
        Name = gt.Name,
        Description = gt.Description ?? string.Empty,
        IsWorkArea = gt.IsWorkArea,
        FlagsJson = gt.FlagsJson ?? string.Empty
    };

    private static GroupResponse MapGroup(DynamicGroup g) => new()
    {
        CtrlNbr = g.CtrlNbr.Value,
        GroupTypeCtrlNbr = g.GroupTypeCtrlNbr.Value,
        Name = g.Name,
        ParentGroupCtrlNbr = g.ParentGroupCtrlNbr?.Value ?? 0,
        Path = g.Path ?? string.Empty,
        IsWorkArea = g.IsWorkArea
    };

    private static RailroadGroupPlacementResponse MapPlacement(RailroadGroupPlacement p) => new()
    {
        CtrlNbr = p.CtrlNbr.Value,
        RailroadCtrlNbr = p.RailroadCtrlNbr.Value,
        GroupCtrlNbr = p.GroupCtrlNbr.Value
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
