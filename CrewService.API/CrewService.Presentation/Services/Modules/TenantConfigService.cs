using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class TenantConfigService(IServiceProvider serviceProvider) : TenantConfigSrvc.TenantConfigSrvcBase
{
    // GroupTypes
    public override async Task<GetAllGroupTypesResponse> GetAllGroupTypes(GetAllGroupTypesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        var groupTypes = await svc.GetAllGroupTypesAsync(context.CancellationToken);
        var response = new GetAllGroupTypesResponse { TotalCount = groupTypes.Count };
        foreach (var gt in groupTypes) response.GroupTypes.Add(MapGroupType(gt));
        return response;
    }

    public override async Task<GroupTypeResponse> GetGroupType(GetGroupTypeRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            return MapGroupType(await svc.GetGroupTypeAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GroupTypeResponse> CreateGroupType(CreateGroupTypeRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            var gt = await svc.CreateGroupTypeAsync(
                request.Name, request.Description, request.IsWorkArea, request.FlagsJson,
                request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
                request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null,
                request.ParentGroupTypeCtrlNbr > 0 ? ControlNumber.Create(request.ParentGroupTypeCtrlNbr) : null,
                context.CancellationToken);
            return MapGroupType(gt);
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
    }

    public override async Task<GroupTypeResponse> UpdateGroupType(UpdateGroupTypeRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            var gt = await svc.UpdateGroupTypeAsync(
                ControlNumber.Create(request.CtrlNbr),
                request.Name, request.Description, request.IsWorkArea, request.FlagsJson,
                request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
                request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null,
                request.ParentGroupTypeCtrlNbr > 0 ? ControlNumber.Create(request.ParentGroupTypeCtrlNbr) : null,
                context.CancellationToken);
            return MapGroupType(gt);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteGroupType(DeleteGroupTypeRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            await svc.DeleteGroupTypeAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    // Groups
    public override async Task<GetAllGroupsResponse> GetGroupsByTypeName(GetGroupsByTypeNameRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        var groups = await svc.GetGroupsByTypeNameAsync(
            request.TypeName,
            request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
            context.CancellationToken);
        var response = new GetAllGroupsResponse { TotalCount = groups.Count };
        foreach (var g in groups) response.Groups.Add(MapGroup(g));
        return response;
    }

    public override async Task<GetAllGroupsResponse> GetAllGroups(GetAllGroupsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        var parentCtrlNbr = request.ParentGroupCtrlNbr > 0 ? ControlNumber.Create(request.ParentGroupCtrlNbr) : (ControlNumber?)null;
        var groups = await svc.GetAllGroupsAsync(parentCtrlNbr, context.CancellationToken);
        var response = new GetAllGroupsResponse { TotalCount = groups.Count };
        foreach (var g in groups) response.Groups.Add(MapGroup(g));
        return response;
    }

    public override async Task<GroupResponse> GetGroup(GetGroupRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            return MapGroup(await svc.GetGroupAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken));
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GroupResponse> CreateGroup(CreateGroupRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            var group = await svc.CreateGroupAsync(
                request.GroupTypeCtrlNbr, request.Name,
                request.ParentGroupCtrlNbr > 0 ? request.ParentGroupCtrlNbr : null,
                request.IsWorkArea,
                string.IsNullOrEmpty(request.Code) ? null : request.Code,
                request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
                request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null,
                string.IsNullOrEmpty(request.TimeZoneId) ? null : request.TimeZoneId,
                context.CancellationToken);
            return MapGroup(group);
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
    }

    public override async Task<GroupResponse> UpdateGroup(UpdateGroupRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            var group = await svc.UpdateGroupAsync(
                ControlNumber.Create(request.CtrlNbr), request.Name,
                request.ParentGroupCtrlNbr > 0 ? request.ParentGroupCtrlNbr : null,
                request.IsWorkArea,
                string.IsNullOrEmpty(request.Code) ? null : request.Code,
                request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
                request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null,
                string.IsNullOrEmpty(request.TimeZoneId) ? null : request.TimeZoneId,
                context.CancellationToken);
            return MapGroup(group);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteGroup(DeleteGroupRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            await svc.DeleteGroupAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    // Tree queries
    public override async Task<GetAllGroupsResponse> GetGroupTree(GetGroupTreeRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        var rootCtrlNbr = request.RootCtrlNbr > 0 ? ControlNumber.Create(request.RootCtrlNbr) : (ControlNumber?)null;
        var groups = await svc.GetGroupTreeAsync(rootCtrlNbr, context.CancellationToken);
        var response = new GetAllGroupsResponse { TotalCount = groups.Count };
        foreach (var g in groups) response.Groups.Add(MapGroup(g));
        return response;
    }

    public override async Task<GetAllGroupsResponse> GetWorkAreas(GetWorkAreasRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        var railroadCtrlNbr = request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : (ControlNumber?)null;
        var workAreas = await svc.GetWorkAreasAsync(railroadCtrlNbr, context.CancellationToken);
        var response = new GetAllGroupsResponse { TotalCount = workAreas.Count };
        foreach (var g in workAreas) response.Groups.Add(MapGroup(g));
        return response;
    }

    public override async Task<GetAllGroupsResponse> GetWorkAreaTree(GetWorkAreasRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        var groups = await svc.GetWorkAreaTreeAsync(context.CancellationToken);
        var response = new GetAllGroupsResponse { TotalCount = groups.Count };
        foreach (var g in groups) response.Groups.Add(MapGroup(g));
        return response;
    }

    public override async Task<GetAllGroupsResponse> GetAncestors(GetAncestorsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        var ancestors = await svc.GetAncestorsAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
        var response = new GetAllGroupsResponse { TotalCount = ancestors.Count };
        foreach (var g in ancestors) response.Groups.Add(MapGroup(g));
        return response;
    }

    // Attribute Definitions
    public override async Task<GetAttributeDefinitionsResponse> GetAttributeDefinitions(GetAttributeDefinitionsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        var definitions = await svc.GetAttributeDefinitionsAsync(ControlNumber.Create(request.GroupTypeCtrlNbr), context.CancellationToken);
        var response = new GetAttributeDefinitionsResponse();
        foreach (var ad in definitions) response.AttributeDefinitions.Add(MapAttributeDefinition(ad));
        return response;
    }

    public override async Task<AttributeDefinitionResponse> GetAttributeDefinition(GetAttributeDefinitionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            return MapAttributeDefinition(await svc.GetAttributeDefinitionAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<AttributeDefinitionResponse> CreateAttributeDefinition(CreateAttributeDefinitionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            var def = await svc.CreateAttributeDefinitionAsync(
                request.GroupTypeCtrlNbr, request.AttributeName, request.DataType, request.IsRequired,
                string.IsNullOrEmpty(request.DefaultValue) ? null : request.DefaultValue,
                context.CancellationToken);
            return MapAttributeDefinition(def);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
    }

    public override async Task<AttributeDefinitionResponse> UpdateAttributeDefinition(UpdateAttributeDefinitionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            var def = await svc.UpdateAttributeDefinitionAsync(
                ControlNumber.Create(request.CtrlNbr), request.AttributeName, request.DataType, request.IsRequired,
                string.IsNullOrEmpty(request.DefaultValue) ? null : request.DefaultValue,
                context.CancellationToken);
            return MapAttributeDefinition(def);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAttributeDefinition(DeleteAttributeDefinitionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            await svc.DeleteAttributeDefinitionAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    // Attribute Values
    public override async Task<GetAttributeValuesResponse> GetAttributeValues(GetAttributeValuesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        var values = await svc.GetAttributeValuesAsync(ControlNumber.Create(request.GroupCtrlNbr), context.CancellationToken);
        var response = new GetAttributeValuesResponse();
        foreach (var av in values) response.AttributeValues.Add(MapAttributeValue(av));
        return response;
    }

    public override async Task<AttributeValueResponse> SetAttributeValue(SetAttributeValueRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            var av = await svc.SetAttributeValueAsync(
                request.GroupCtrlNbr, request.AttributeDefinitionCtrlNbr,
                string.IsNullOrEmpty(request.Value) ? null : request.Value,
                context.CancellationToken);
            return MapAttributeValue(av);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAttributeValue(DeleteAttributeValueRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
        try
        {
            await svc.DeleteAttributeValueAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
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
        RailroadCtrlNbr = g.RailroadCtrlNbr?.Value ?? 0,
        TimeZoneId = g.TimeZoneId ?? string.Empty
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
