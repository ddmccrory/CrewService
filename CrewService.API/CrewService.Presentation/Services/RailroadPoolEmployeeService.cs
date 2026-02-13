using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class RailroadPoolEmployeeService(IRailroadPoolEmployeeRepository employeeRepository) : RailroadPoolEmployeeSrvc.RailroadPoolEmployeeSrvcBase
{
    private readonly IRailroadPoolEmployeeRepository _employeeRepository = employeeRepository;

    public override async Task<GetAllRailroadPoolEmployeeResponse> GetAllAsync(GetAllRailroadPoolEmployeeRequest request, ServerCallContext context)
    {
        var response = new GetAllRailroadPoolEmployeeResponse();
        var employees = await _employeeRepository.GetByRailroadPoolCtrlNbrAsync(ControlNumber.Create(request.RailroadPoolCtrlNbr));
        response.Employees.AddRange(employees.Select(MapToResponse));
        response.TotalCount = employees.Count;
        return response;
    }

    public override async Task<RailroadPoolEmployeeResponse> GetAsync(GetRailroadPoolEmployeeRequest request, ServerCallContext context)
    {
        var employee = await _employeeRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"RailroadPoolEmployee {request.CtrlNbr} not found."));
        return MapToResponse(employee);
    }

    public override async Task<RailroadPoolEmployeeResponse> CreateAsync(CreateRailroadPoolEmployeeRequest request, ServerCallContext context)
    {
        var employee = RailroadPoolEmployee.Create(request.RailroadPoolCtrlNbr, request.EmployeeCtrlNbr, request.IsActive);
        await _employeeRepository.AddAsync(employee);
        return MapToResponse(employee);
    }

    public override async Task<RailroadPoolEmployeeResponse> UpdateAsync(UpdateRailroadPoolEmployeeRequest request, ServerCallContext context)
    {
        var employee = await _employeeRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"RailroadPoolEmployee {request.CtrlNbr} not found."));
        employee.Update(request.IsActive);
        await _employeeRepository.UpdateAsync(employee);
        return MapToResponse(employee);
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteRailroadPoolEmployeeRequest request, ServerCallContext context)
    {
        await _employeeRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    private static RailroadPoolEmployeeResponse MapToResponse(RailroadPoolEmployee employee)
    {
        return new RailroadPoolEmployeeResponse
        {
            CtrlNbr = employee.CtrlNbr.Value,
            RailroadPoolCtrlNbr = employee.RailroadPoolCtrlNbr.Value,
            EmployeeCtrlNbr = employee.EmployeeCtrlNbr.Value,
            IsActive = employee.IsActive
        };
    }
}