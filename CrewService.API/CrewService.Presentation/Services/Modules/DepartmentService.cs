using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class DepartmentService(
    IDepartmentRepository departmentRepository) : DepartmentSrvc.DepartmentSrvcBase
{
    public override async Task<GetDepartmentsResponse> GetAll(GetDepartmentsRequest request, ServerCallContext context)
    {
        var departments = await departmentRepository.GetByParentAndRailroadAsync(
            request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
            request.DynamicGroupCtrlNbr > 0 ? ControlNumber.Create(request.DynamicGroupCtrlNbr) : null);
        var response = new GetDepartmentsResponse { TotalCount = departments.Count };
        foreach (var d in departments)
            response.Departments.Add(MapDepartment(d));
        return response;
    }

    public override async Task<DepartmentResponse> Create(CreateDepartmentRequest request, ServerCallContext context)
    {
        var department = Department.Create(
            request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
            request.DynamicGroupCtrlNbr > 0 ? ControlNumber.Create(request.DynamicGroupCtrlNbr) : null,
            request.Name);
        await departmentRepository.AddAsync(department);
        return MapDepartment(department);
    }

    public override async Task<DepartmentResponse> Update(UpdateDepartmentRequest request, ServerCallContext context)
    {
        var department = await departmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Department {request.CtrlNbr} not found."));
        department.Update(request.Name);
        await departmentRepository.UpdateAsync(department);
        return MapDepartment(department);
    }

    public override async Task<DeleteResponse> Delete(DeleteDepartmentRequest request, ServerCallContext context)
    {
        await departmentRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    private static DepartmentResponse MapDepartment(Department d) => new()
    {
        CtrlNbr = d.CtrlNbr.Value,
        ParentCtrlNbr = d.ParentCtrlNbr?.Value ?? 0,
        DynamicGroupCtrlNbr = d.DynamicGroupCtrlNbr?.Value ?? 0,
        Name = d.Name
    };
}
