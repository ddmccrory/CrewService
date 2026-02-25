using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class TenantConfigService(
    IGroupTypeRepository groupTypeRepository,
    IDynamicGroupRepository dynamicGroupRepository) : TenantConfigSrvc.TenantConfigSrvcBase
{
    private readonly IGroupTypeRepository _groupTypeRepository = groupTypeRepository;
    private readonly IDynamicGroupRepository _dynamicGroupRepository = dynamicGroupRepository;

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
}
