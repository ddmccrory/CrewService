using CrewService.Application.WorkManagement;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class DepartmentService(IServiceProvider serviceProvider) : DepartmentSrvc.DepartmentSrvcBase
{
    public override async Task<GetDepartmentsResponse> GetAll(GetDepartmentsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.DepartmentService>();
        var departments = await svc.GetByParentAndRailroadAsync(
            request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
            request.DynamicGroupCtrlNbr > 0 ? ControlNumber.Create(request.DynamicGroupCtrlNbr) : null);
        var response = new GetDepartmentsResponse { TotalCount = departments.Count };
        foreach (var d in departments)
            response.Departments.Add(MapDepartment(d));
        return response;
    }

    public override async Task<DepartmentResponse> Create(CreateDepartmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.DepartmentService>();
        var department = await svc.CreateAsync(
            request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
            request.DynamicGroupCtrlNbr > 0 ? ControlNumber.Create(request.DynamicGroupCtrlNbr) : null,
            request.Name,
            string.IsNullOrEmpty(request.DefaultCallSheetView) ? "Vertical" : request.DefaultCallSheetView);
        return MapDepartment(department);
    }

    public override async Task<DepartmentResponse> Update(UpdateDepartmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.DepartmentService>();
        try
        {
            var department = await svc.UpdateAsync(
                ControlNumber.Create(request.CtrlNbr),
                request.Name,
                string.IsNullOrEmpty(request.DefaultCallSheetView) ? "Vertical" : request.DefaultCallSheetView);
            return MapDepartment(department);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> Delete(DeleteDepartmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.WorkManagement.DepartmentService>();
        try
        {
            await svc.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static DepartmentResponse MapDepartment(Domain.Modules.WorkManagement.Department d) => new()
    {
        CtrlNbr = d.CtrlNbr.Value,
        ParentCtrlNbr = d.ParentCtrlNbr?.Value ?? 0,
        DynamicGroupCtrlNbr = d.DynamicGroupCtrlNbr?.Value ?? 0,
        Name = d.Name,
        DefaultCallSheetView = d.DefaultCallSheetView
    };
}
