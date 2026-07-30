using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class WorkManagementService(IServiceProvider serviceProvider) : WorkManagementSrvc.WorkManagementSrvcBase
{
    public override async Task<GetWorkInstancesResponse> GetWorkInstances(GetWorkInstancesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        var instances = await svc.GetWorkInstancesAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
            DateTime.Parse(request.StartUtc).ToUniversalTime(),
            DateTime.Parse(request.EndUtc).ToUniversalTime(),
            context.CancellationToken);
        var response = new GetWorkInstancesResponse { TotalCount = instances.Count };
        foreach (var w in instances) response.Instances.Add(MapWorkInstance(w));
        return response;
    }

    public override async Task<WorkInstanceResponse> CreateWorkInstance(CreateWorkInstanceRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        var instance = await svc.CreateWorkInstanceAsync(
            request.AssignmentGroupCtrlNbr > 0 ? request.AssignmentGroupCtrlNbr : null,
            request.WorkAreaGroupCtrlNbr,
            DateTime.Parse(request.StartUtc).ToUniversalTime(),
            DateTime.Parse(request.EndUtc).ToUniversalTime(),
            string.IsNullOrEmpty(request.CallTimeUtc) ? null : DateTime.Parse(request.CallTimeUtc).ToUniversalTime(),
            context.CancellationToken);
        return MapWorkInstance(instance);
    }

    public override async Task<GetPositionSlotsResponse> GetPositionSlots(GetPositionSlotsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        var slots = await svc.GetPositionSlotsAsync(ControlNumber.Create(request.WorkInstanceCtrlNbr), context.CancellationToken);
        var response = new GetPositionSlotsResponse { TotalCount = slots.Count };
        foreach (var s in slots) response.Slots.Add(MapSlot(s));
        return response;
    }

    public override async Task<PositionSlotResponse> CreatePositionSlot(CreatePositionSlotRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        var slot = await svc.CreatePositionSlotAsync(request.WorkInstanceCtrlNbr, request.CraftRoleCtrlNbr, context.CancellationToken);
        return MapSlot(slot);
    }

    public override async Task<PositionSlotResponse> BindSlot(BindSlotRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        try
        {
            var slot = await svc.BindSlotAsync(ControlNumber.Create(request.CtrlNbr), request.EmployeeCtrlNbr, request.Source, context.CancellationToken);
            return MapSlot(slot);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<PositionSlotResponse> UnbindSlot(UnbindSlotRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        try
        {
            var slot = await svc.UnbindSlotAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapSlot(slot);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GetCraftRolesResponse> GetCraftRoles(GetCraftRolesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        var craftCtrlNbr = request.CraftCtrlNbr > 0 ? ControlNumber.Create(request.CraftCtrlNbr) : null;
        var railroadCtrlNbr = request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null;
        var roles = await svc.GetCraftRolesAsync(departmentCtrlNbr, craftCtrlNbr, railroadCtrlNbr, context.CancellationToken);
        var response = new GetCraftRolesResponse { TotalCount = roles.Count };
        foreach (var r in roles) response.Roles.Add(MapRole(r));
        return response;
    }

    public override async Task<CraftRoleResponse> CreateCraftRole(CreateCraftRoleRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        var role = await svc.CreateCraftRoleAsync(
            request.CraftCtrlNbr,
            request.Code,
            request.Name,
            request.AlternateName,
            request.DefaultRosterBoardCtrlNbr > 0 ? request.DefaultRosterBoardCtrlNbr : null,
            context.CancellationToken);
        return MapRole(role);
    }

    public override async Task<CraftRoleResponse> UpdateCraftRole(UpdateCraftRoleRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        try
        {
            var role = await svc.UpdateCraftRoleAsync(
                ControlNumber.Create(request.CtrlNbr),
                request.Code,
                request.Name,
                request.AlternateName,
                request.DefaultRosterBoardCtrlNbr > 0 ? request.DefaultRosterBoardCtrlNbr : null,
                context.CancellationToken);
            return MapRole(role);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteCraftRole(DeleteCraftRoleRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        try
        {
            await svc.DeleteCraftRoleAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GetCraftRoleQualificationsResponse> GetCraftRoleQualifications(GetCraftRoleQualificationsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        var quals = await svc.GetCraftRoleQualificationsAsync(ControlNumber.Create(request.CraftRoleCtrlNbr), context.CancellationToken);
        var response = new GetCraftRoleQualificationsResponse { TotalCount = quals.Count };
        foreach (var q in quals) response.Qualifications.Add(MapCraftRoleQualification(q));
        return response;
    }

    public override async Task<CraftRoleQualificationResponse> AddCraftRoleQualification(AddCraftRoleQualificationRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        try
        {
            var rq = await svc.AddCraftRoleQualificationAsync(
                ControlNumber.Create(request.CraftRoleCtrlNbr),
                ControlNumber.Create(request.QualificationTypeCtrlNbr),
                context.CancellationToken);
            return MapCraftRoleQualification(rq);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<DeleteResponse> RemoveCraftRoleQualification(RemoveCraftRoleQualificationRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        try
        {
            await svc.RemoveCraftRoleQualificationAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GetShiftDefinitionsResponse> GetShiftDefinitions(GetShiftDefinitionsRequest request, ServerCallContext context)
    {
        if (request.WorkAreaGroupCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "WorkAreaGroupCtrlNbr is required."));
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        var shifts = await svc.GetShiftDefinitionsAsync(ControlNumber.Create(request.WorkAreaGroupCtrlNbr), context.CancellationToken);
        var response = new GetShiftDefinitionsResponse { TotalCount = shifts.Count };
        foreach (var sd in shifts) response.ShiftDefinitions.Add(MapShiftDefinition(sd));
        return response;
    }

    public override async Task<ShiftDefinitionResponse> CreateShiftDefinition(CreateShiftDefinitionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        var shift = await svc.CreateShiftDefinitionAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
            request.ShiftCode, request.DisplayName, request.DisplayOrder, request.IsActive,
            context.CancellationToken);
        return MapShiftDefinition(shift);
    }

    public override async Task<ShiftDefinitionResponse> UpdateShiftDefinition(UpdateShiftDefinitionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        try
        {
            var shift = await svc.UpdateShiftDefinitionAsync(
                ControlNumber.Create(request.CtrlNbr),
                request.ShiftCode, request.DisplayName, request.DisplayOrder, request.IsActive,
                context.CancellationToken);
            return MapShiftDefinition(shift);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteShiftDefinition(DeleteShiftDefinitionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.WorkManagementService>();
        try
        {
            await svc.DeleteShiftDefinitionAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
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

    private static CraftRoleQualificationResponse MapCraftRoleQualification(CraftRoleQualification rq) => new()
    {
        CtrlNbr = rq.CtrlNbr.Value,
        CraftRoleCtrlNbr = rq.CraftRoleCtrlNbr.Value,
        QualificationTypeCtrlNbr = rq.QualificationTypeCtrlNbr.Value
    };

    private static CraftRoleResponse MapRole(CraftRole r) => new()
    {
        CtrlNbr = r.CtrlNbr.Value,
        CraftCtrlNbr = r.CraftCtrlNbr.Value,
        Code = r.Code ?? string.Empty,
        Name = r.Name,
        AlternateName = r.AlternateName ?? string.Empty,
        DefaultRosterBoardCtrlNbr = r.DefaultRosterBoardCtrlNbr?.Value ?? 0
    };

    private static ShiftDefinitionResponse MapShiftDefinition(ShiftDefinition sd) => new()
    {
        CtrlNbr = sd.CtrlNbr.Value,
        WorkAreaGroupCtrlNbr = sd.WorkAreaGroupCtrlNbr.Value,
        ShiftCode = sd.ShiftCode,
        DisplayName = sd.DisplayName,
        DisplayOrder = sd.DisplayOrder,
        IsActive = sd.IsActive
    };
}
