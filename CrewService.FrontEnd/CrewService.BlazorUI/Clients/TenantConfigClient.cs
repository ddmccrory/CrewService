using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class TenantConfigClient(GrpcChannelProvider channelProvider, IHttpContextAccessor httpContextAccessor, ILogger<TenantConfigClient> logger)
: BaseGrpcClient<TenantConfigSrvc.TenantConfigSrvcClient>(channelProvider, httpContextAccessor, callInvoker => new TenantConfigSrvc.TenantConfigSrvcClient(callInvoker), logger)
{
    // ?? GroupTypes ????????????????????????????????????????????????????

    public async Task<GetAllGroupTypesResponse> GetAllGroupTypesAsync()
    {
        try
        {
            return await _client.GetAllGroupTypesAsync(new GetAllGroupTypesRequest());
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GroupTypeResponse> GetGroupTypeAsync(long ctrlNbr)
    {
        try
        {
            return await _client.GetGroupTypeAsync(new GetGroupTypeRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GroupTypeResponse> CreateGroupTypeAsync(string name, string description, bool isWorkArea, string? flagsJson = null)
    {
        try
        {
            return await _client.CreateGroupTypeAsync(new CreateGroupTypeRequest
            {
                Name = name,
                Description = description,
                IsWorkArea = isWorkArea,
                FlagsJson = flagsJson ?? ""
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GroupTypeResponse> UpdateGroupTypeAsync(long ctrlNbr, string name, string description, bool isWorkArea, string? flagsJson = null)
    {
        try
        {
            return await _client.UpdateGroupTypeAsync(new UpdateGroupTypeRequest
            {
                CtrlNbr = ctrlNbr,
                Name = name,
                Description = description,
                IsWorkArea = isWorkArea,
                FlagsJson = flagsJson ?? ""
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task DeleteGroupTypeAsync(long ctrlNbr)
    {
        try
        {
            await _client.DeleteGroupTypeAsync(new DeleteGroupTypeRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    // ?? DynamicGroups ????????????????????????????????????????????????

    public async Task<GetAllGroupsResponse> GetAllGroupsAsync(long parentGroupCtrlNbr = 0)
    {
        try
        {
            return await _client.GetAllGroupsAsync(new GetAllGroupsRequest { ParentGroupCtrlNbr = parentGroupCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GroupResponse> GetGroupAsync(long ctrlNbr)
    {
        try
        {
            return await _client.GetGroupAsync(new GetGroupRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GroupResponse> CreateGroupAsync(long groupTypeCtrlNbr, string name, long parentGroupCtrlNbr = 0, bool isWorkArea = false)
    {
        try
        {
            return await _client.CreateGroupAsync(new CreateGroupRequest
            {
                GroupTypeCtrlNbr = groupTypeCtrlNbr,
                Name = name,
                ParentGroupCtrlNbr = parentGroupCtrlNbr,
                IsWorkArea = isWorkArea
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GroupResponse> UpdateGroupAsync(long ctrlNbr, string name, long parentGroupCtrlNbr = 0, bool isWorkArea = false)
    {
        try
        {
            return await _client.UpdateGroupAsync(new UpdateGroupRequest
            {
                CtrlNbr = ctrlNbr,
                Name = name,
                ParentGroupCtrlNbr = parentGroupCtrlNbr,
                IsWorkArea = isWorkArea
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task DeleteGroupAsync(long ctrlNbr)
    {
        try
        {
            await _client.DeleteGroupAsync(new DeleteGroupRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetAllGroupsResponse> GetGroupTreeAsync(long rootCtrlNbr = 0)
    {
        try
        {
            return await _client.GetGroupTreeAsync(new GetGroupTreeRequest { RootCtrlNbr = rootCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetAllGroupsResponse> GetWorkAreasAsync()
    {
        try
        {
            return await _client.GetWorkAreasAsync(new GetWorkAreasRequest());
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    // ?? Railroad Placements ??????????????????????????????????????????

    public async Task<RailroadGroupPlacementResponse> PlaceRailroadAsync(long railroadCtrlNbr, long groupCtrlNbr)
    {
        try
        {
            return await _client.PlaceRailroadInGroupAsync(new PlaceRailroadInGroupRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr,
                GroupCtrlNbr = groupCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task RemoveRailroadFromGroupAsync(long placementCtrlNbr)
    {
        try
        {
            await _client.RemoveRailroadFromGroupAsync(new RemoveRailroadFromGroupRequest { CtrlNbr = placementCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetRailroadPlacementsResponse> GetRailroadPlacementsAsync(long railroadCtrlNbr)
    {
        try
        {
            return await _client.GetRailroadPlacementsAsync(new GetRailroadPlacementsRequest { RailroadCtrlNbr = railroadCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetRailroadPlacementsResponse> GetRailroadsInGroupAsync(long groupCtrlNbr, bool includeDescendants = false)
    {
        try
        {
            return await _client.GetRailroadsInGroupAsync(new GetRailroadsInGroupRequest
            {
                GroupCtrlNbr = groupCtrlNbr,
                IncludeDescendants = includeDescendants
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    // ?? Attribute Definitions ????????????????????????????????????????

    public async Task<GetAttributeDefinitionsResponse> GetAttributeDefinitionsAsync(long groupTypeCtrlNbr)
    {
        try
        {
            return await _client.GetAttributeDefinitionsAsync(new GetAttributeDefinitionsRequest { GroupTypeCtrlNbr = groupTypeCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<AttributeDefinitionResponse> CreateAttributeDefinitionAsync(long groupTypeCtrlNbr, string attributeName, string dataType, bool isRequired, string? defaultValue = null)
    {
        try
        {
            return await _client.CreateAttributeDefinitionAsync(new CreateAttributeDefinitionRequest
            {
                GroupTypeCtrlNbr = groupTypeCtrlNbr,
                AttributeName = attributeName,
                DataType = dataType,
                IsRequired = isRequired,
                DefaultValue = defaultValue ?? ""
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<AttributeDefinitionResponse> UpdateAttributeDefinitionAsync(long ctrlNbr, string attributeName, string dataType, bool isRequired, string? defaultValue = null)
    {
        try
        {
            return await _client.UpdateAttributeDefinitionAsync(new UpdateAttributeDefinitionRequest
            {
                CtrlNbr = ctrlNbr,
                AttributeName = attributeName,
                DataType = dataType,
                IsRequired = isRequired,
                DefaultValue = defaultValue ?? ""
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task DeleteAttributeDefinitionAsync(long ctrlNbr)
    {
        try
        {
            await _client.DeleteAttributeDefinitionAsync(new DeleteAttributeDefinitionRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    // ?? Attribute Values ?????????????????????????????????????????????

    public async Task<GetAttributeValuesResponse> GetAttributeValuesAsync(long groupCtrlNbr)
    {
        try
        {
            return await _client.GetAttributeValuesAsync(new GetAttributeValuesRequest { GroupCtrlNbr = groupCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<AttributeValueResponse> SetAttributeValueAsync(long groupCtrlNbr, long attributeDefinitionCtrlNbr, string value)
    {
        try
        {
            return await _client.SetAttributeValueAsync(new SetAttributeValueRequest
            {
                GroupCtrlNbr = groupCtrlNbr,
                AttributeDefinitionCtrlNbr = attributeDefinitionCtrlNbr,
                Value = value
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task DeleteAttributeValueAsync(long ctrlNbr)
    {
        try
        {
            await _client.DeleteAttributeValueAsync(new DeleteAttributeValueRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
