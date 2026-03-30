using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class WorkManagementService(
    IWorkInstanceRepository workInstanceRepository,
    IPositionSlotRepository positionSlotRepository,
    ICraftRoleRepository craftRoleRepository) : WorkManagementSrvc.WorkManagementSrvcBase
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
        var slot = PositionSlot.Create(request.WorkInstanceCtrlNbr, request.CraftRoleCtrlNbr);
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
        CraftRoleCtrlNbr = s.CraftRoleCtrlNbr.Value,
        Status = s.Status,
        BoundEmployeeCtrlNbr = s.BoundEmployeeCtrlNbr?.Value ?? 0,
        BindingSource = s.BindingSource ?? string.Empty
    };

    public override async Task<GetCraftRolesResponse> GetCraftRoles(GetCraftRolesRequest request, ServerCallContext context)
    {
        var roles = request.CraftCtrlNbr > 0
            ? await craftRoleRepository.GetByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr))
            : await craftRoleRepository.GetAllAsync();
        var response = new GetCraftRolesResponse { TotalCount = roles.Count };
        foreach (var r in roles) response.Roles.Add(MapRole(r));
        return response;
    }

    public override async Task<CraftRoleResponse> CreateCraftRole(CreateCraftRoleRequest request, ServerCallContext context)
    {
        var role = CraftRole.Create(request.CraftCtrlNbr, request.Code, request.Name, request.AlternateName);
        await craftRoleRepository.AddAsync(role);
        return MapRole(role);
    }


    public override async Task<CraftRoleResponse> UpdateCraftRole(UpdateCraftRoleRequest request, ServerCallContext context)
    {
        var role = await craftRoleRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"CraftRole {request.CtrlNbr} not found."));
        role.Update(request.Code, request.Name, request.AlternateName);
        await craftRoleRepository.UpdateAsync(role);
        return MapRole(role);
    }

    public override async Task<DeleteResponse> DeleteCraftRole(DeleteCraftRoleRequest request, ServerCallContext context)
    {
        await craftRoleRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    private static CraftRoleResponse MapRole(CraftRole r) => new()
    {
        CtrlNbr = r.CtrlNbr.Value,
        CraftCtrlNbr = r.CraftCtrlNbr.Value,
        Code = r.Code ?? string.Empty,
        Name = r.Name,
        AlternateName = r.AlternateName ?? string.Empty
    };
}
