using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class RailroadEmployeeService(IRailroadEmployeeRepository railroadEmployeeRepository) : RailroadEmployeeSrvc.RailroadEmployeeSrvcBase
{
    private readonly IRailroadEmployeeRepository _railroadEmployeeRepository = railroadEmployeeRepository;

    public override async Task<GetAllRailroadEmployeesResponse> GetAllRailroadEmployeesAsync(GetAllRailroadEmployeesRequest request, ServerCallContext context)
    {
        var response = new GetAllRailroadEmployeesResponse();

        var entities = request.PageSize > 0
            ? await _railroadEmployeeRepository.GetAllAsync(request.PageNumber, request.PageSize)
            : await _railroadEmployeeRepository.GetAllAsync();

        foreach (var ent in entities)
            response.RailroadEmployees.Add(MapToRailroadEmployeeResponse(ent));

        return response;
    }

    public override async Task<GetRailroadEmployeeResponse> GetRailroadEmployeeAsync(GetRailroadEmployeeRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        var entity = await _railroadEmployeeRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"RailroadEmployee with control number {request.CtrlNbr} was not found."));

        return MapToRailroadEmployeeResponse(entity);
    }

    public override async Task<GetRailroadEmployeeResponse> GetRailroadEmployeeByEmployeeAsync(GetRailroadEmployeeByEmployeeRequest request, ServerCallContext context)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.EmployeeCtrlNbr <= 0)
            errors.Add("EmployeeCtrlNbr", ["Must be greater than 0"]);

        if (request.RailroadCtrlNbr <= 0)
            errors.Add("RailroadCtrlNbr", ["Must be greater than 0"]);

        if (errors.Count > 0)
            throw new ValidationException(errors);

        var entity = await _railroadEmployeeRepository.GetByEmployeeAndRailroadAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            ControlNumber.Create(request.RailroadCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"RailroadEmployee for employee {request.EmployeeCtrlNbr} and railroad {request.RailroadCtrlNbr} was not found."));

        return MapToRailroadEmployeeResponse(entity);
    }

    public override async Task<GetAllRailroadEmployeesResponse> GetRailroadEmployeesByRailroadAsync(GetRailroadEmployeesByRailroadRequest request, ServerCallContext context)
    {
        if (request.RailroadCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid railroad control number."));

        var entities = await _railroadEmployeeRepository.GetByRailroadCtrlNbrAsync(ControlNumber.Create(request.RailroadCtrlNbr));

        var response = new GetAllRailroadEmployeesResponse();
        foreach (var ent in entities)
            response.RailroadEmployees.Add(MapToRailroadEmployeeResponse(ent));

        return response;
    }

    public override Task<CreateRailroadEmployeeResponse> CreateRailroadEmployeeAsync(CreateRailroadEmployeeRequest request, ServerCallContext context)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.EmployeeCtrlNbr <= 0)
            errors.Add("EmployeeCtrlNbr", ["Must be greater than 0"]);

        if (request.RailroadCtrlNbr <= 0)
            errors.Add("RailroadCtrlNbr", ["Must be greater than 0"]);

        if (errors.Count > 0)
            throw new ValidationException(errors);

        var entity = RailroadEmployee.Create(request.EmployeeCtrlNbr, request.RailroadCtrlNbr, request.AssignedPoolsOnly);

        _railroadEmployeeRepository.Add(entity);

        var response = new CreateRailroadEmployeeResponse
        {
            CtrlNbr = entity.CtrlNbr.Value,
            EmployeeCtrlNbr = request.EmployeeCtrlNbr,
            RailroadCtrlNbr = request.RailroadCtrlNbr,
            AssignedPoolsOnly = request.AssignedPoolsOnly
        };

        return Task.FromResult(response);
    }

    public override async Task<UpdateRailroadEmployeeResponse> UpdateRailroadEmployeeAsync(UpdateRailroadEmployeeRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        var entity = await _railroadEmployeeRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"RailroadEmployee with control number {request.CtrlNbr} was not found."));

        entity.Update(request.AssignedPoolsOnly);

        _railroadEmployeeRepository.Update(entity);

        return new UpdateRailroadEmployeeResponse
        {
            CtrlNbr = entity.CtrlNbr.Value,
            EmployeeCtrlNbr = entity.EmployeeCtrlNbr.Value,
            RailroadCtrlNbr = entity.RailroadCtrlNbr.Value,
            AssignedPoolsOnly = entity.AssignedPoolsOnly
        };
    }

    public override async Task<DeleteResponse> DeleteRailroadEmployeeAsync(DeleteRailroadEmployeeRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        var entity = await _railroadEmployeeRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"RailroadEmployee with control number {request.CtrlNbr} was not found."));

        _railroadEmployeeRepository.Remove(entity);

        return new DeleteResponse
        {
            Success = true,
            Messages = { $"RailroadEmployee {request.CtrlNbr} deleted successfully." }
        };
    }

    private static GetRailroadEmployeeResponse MapToRailroadEmployeeResponse(RailroadEmployee entity)
    {
        return new GetRailroadEmployeeResponse
        {
            CtrlNbr = entity.CtrlNbr.Value,
            EmployeeCtrlNbr = entity.EmployeeCtrlNbr.Value,
            RailroadCtrlNbr = entity.RailroadCtrlNbr.Value,
            AssignedPoolsOnly = entity.AssignedPoolsOnly
        };
    }
}
