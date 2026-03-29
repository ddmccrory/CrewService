using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class WorkManagementService(
    IWorkInstanceRepository workInstanceRepository,
    IPositionSlotRepository positionSlotRepository,
    IPositionRoleRepository positionRoleRepository) : WorkManagementSrvc.WorkManagementSrvcBase
{





    public override async Task<GetWorkInstancesResponse> GetWorkInstances(GetWorkInstancesRequest request, ServerCallContext context)
    {
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        var endUtc = DateTime.Parse(request.EndUtc).ToUniversalTime();
        var instances = await workInstanceRepository.GetByWorkAreaAndDateRangeAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), startUtc, endUtc);
        var response = new GetWorkInstancesResponse { TotalCount = instances.Count };
        foreach (var w in instances)
            response.Instances.Add(MapWorkInstance(w));
        return response;
    }

    public override async Task<WorkInstanceResponse> CreateWorkInstance(CreateWorkInstanceRequest request, ServerCallContext context)
    {
        var instance = WorkInstance.Create(
            request.AssignmentGroupCtrlNbr > 0 ? request.AssignmentGroupCtrlNbr : null,
            request.WorkAreaGroupCtrlNbr,
            DateTime.Parse(request.StartUtc).ToUniversalTime(),
            DateTime.Parse(request.EndUtc).ToUniversalTime(),
            string.IsNullOrEmpty(request.CallTimeUtc) ? null : DateTime.Parse(request.CallTimeUtc).ToUniversalTime());
        await workInstanceRepository.AddAsync(instance);
        return MapWorkInstance(instance);
    }

    public override async Task<GetPositionSlotsResponse> GetPositionSlots(GetPositionSlotsRequest request, ServerCallContext context)
    {
        var slots = await positionSlotRepository.GetByWorkInstanceAsync(ControlNumber.Create(request.WorkInstanceCtrlNbr));
        var response = new GetPositionSlotsResponse { TotalCount = slots.Count };
        foreach (var s in slots)
            response.Slots.Add(MapSlot(s));
        return response;
    }

    public override async Task<PositionSlotResponse> CreatePositionSlot(CreatePositionSlotRequest request, ServerCallContext context)
    {
        var slot = PositionSlot.Create(request.WorkInstanceCtrlNbr, request.PositionRoleCtrlNbr);
        await positionSlotRepository.AddAsync(slot);
        return MapSlot(slot);
    }

    public override async Task<PositionSlotResponse> BindSlot(BindSlotRequest request, ServerCallContext context)
    {
        var slot = await positionSlotRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Slot {request.CtrlNbr} not found."));
        slot.Bind(request.EmployeeCtrlNbr, request.Source);
        await positionSlotRepository.UpdateAsync(slot);
        return MapSlot(slot);
    }

    public override async Task<PositionSlotResponse> UnbindSlot(UnbindSlotRequest request, ServerCallContext context)
    {
        var slot = await positionSlotRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Slot {request.CtrlNbr} not found."));
        slot.Unbind();
        await positionSlotRepository.UpdateAsync(slot);
        return MapSlot(slot);
    }


    private static WorkInstanceResponse MapWorkInstance(WorkInstance w) => new()
    {
        CtrlNbr = w.CtrlNbr.Value,
        AssignmentGroupCtrlNbr = w.AssignmentGroupCtrlNbr?.Value ?? 0,
        WorkAreaGroupCtrlNbr = w.WorkAreaGroupCtrlNbr.Value,
        StartUtc = w.StartUtc.ToString("O"),
        EndUtc = w.EndUtc.ToString("O"),
        CallTimeUtc = w.CallTimeUtc?.ToString("O") ?? string.Empty,
        Status = w.Status
    };

    private static PositionSlotResponse MapSlot(PositionSlot s) => new()
    {
        CtrlNbr = s.CtrlNbr.Value,
        WorkInstanceCtrlNbr = s.WorkInstanceCtrlNbr.Value,
        PositionRoleCtrlNbr = s.PositionRoleCtrlNbr.Value,
        Status = s.Status,
        BoundEmployeeCtrlNbr = s.BoundEmployeeCtrlNbr?.Value ?? 0,
        BindingSource = s.BindingSource ?? string.Empty
    };

    public override async Task<GetPositionRolesResponse> GetPositionRoles(GetPositionRolesRequest request, ServerCallContext context)
    {
        var roles = await positionRoleRepository.GetByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr));
        var response = new GetPositionRolesResponse { TotalCount = roles.Count };
        foreach (var r in roles) response.Roles.Add(MapRole(r));
        return response;
    }

    public override async Task<PositionRoleResponse> CreatePositionRole(CreatePositionRoleRequest request, ServerCallContext context)
    {
        var role = PositionRole.Create(request.CraftCtrlNbr, request.Code, request.Name, request.AlternateName);
        await positionRoleRepository.AddAsync(role);
        return MapRole(role);
    }


    public override async Task<PositionRoleResponse> UpdatePositionRole(UpdatePositionRoleRequest request, ServerCallContext context)
    {
        var role = await positionRoleRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"PositionRole {request.CtrlNbr} not found."));
        role.Update(request.Code, request.Name, request.AlternateName);
        await positionRoleRepository.UpdateAsync(role);
        return MapRole(role);
    }

    public override async Task<DeleteResponse> DeletePositionRole(DeletePositionRoleRequest request, ServerCallContext context)
    {
        await positionRoleRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    private static PositionRoleResponse MapRole(PositionRole r) => new()
    {
        CtrlNbr = r.CtrlNbr.Value,
        CraftCtrlNbr = r.CraftCtrlNbr.Value,
        Code = r.Code ?? string.Empty,
        Name = r.Name,
        AlternateName = r.AlternateName ?? string.Empty
    };
}
