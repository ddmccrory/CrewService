using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class RailroadService(IRailroadRepository railroadRepository, IOrchestrationUnitOfWorkFactory uowFactory) : RailroadSrvc.RailroadSrvcBase
{
    private readonly IRailroadRepository _railroadRepository = railroadRepository;
    private readonly IOrchestrationUnitOfWorkFactory _uowFactory = uowFactory;

    public override async Task<GetAllRailroadsResponse> GetAllRailroadsAsync(GetAllRailroadsRequest request, ServerCallContext context)
    {
        var response = new GetAllRailroadsResponse();
        var railroads = await _railroadRepository.GetAllAsync();

        foreach (var railroad in railroads)
        {
            response.Railroad.Add(new GetRailroadResponse
            {
                CtrlNbr = railroad.CtrlNbr.Value,
                RrMark = railroad.RailroadMark,
                Name = railroad.Name.Value
            });
        }

        return response;
    }

    public override async Task<GetAllParentRailroadsResponse> GetAllParentRailroadsAsync(GetAllParentRailroadsRequest request, ServerCallContext context)
    {
        var response = new GetAllParentRailroadsResponse();
        var railroads = await _railroadRepository.GetByParentCtrlNbrAsync(ControlNumber.Create(request.ParentCtrlNbr));

        response.ParentCtrlNbr = request.ParentCtrlNbr;

        foreach (var railroad in railroads)
        {
            response.Railroad.Add(new GetRailroadResponse
            {
                CtrlNbr = railroad.CtrlNbr.Value,
                RrMark = railroad.RailroadMark,
                Name = railroad.Name.Value
            });
        }

        return response;
    }

    public override async Task<GetRailroadResponse> GetRailroadAsync(GetRailroadRequest request, ServerCallContext context)
    {
        var railroad = await _railroadRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr));

        return railroad is null
            ? throw new RpcException(new Status(StatusCode.NotFound, $"Railroad, with control number {request.CtrlNbr}, was not found."))
            : new GetRailroadResponse
            {
                CtrlNbr = railroad.CtrlNbr.Value,
                RrMark = railroad.RailroadMark,
                Name = railroad.Name.Value,
            };
    }

    public override async Task<CreateRailroadResponse> CreateRailroadAsync(CreateRailroadRequest request, ServerCallContext context)
    {
        if (request is null || string.IsNullOrEmpty(request.Name))
            throw new ValidationException("Name", "Required");

        var railroad = Railroad.Create(request.ParentCtrlNbr, request.RrMark, request.Name);

        await using var uow = await _uowFactory.CreateAsync();
        uow.Railroads.Add(railroad);
        await uow.CommitAsync();

        return new CreateRailroadResponse
        {
            CtrlNbr = railroad.CtrlNbr.Value,
            ParentCtrlNbr = railroad.ParentCtrlNbr.Value,
            RrMark = railroad.RailroadMark,
            Name = railroad.Name.Value,
        };
    }

    public override async Task<UpdateRailroadResponse> UpdateRailroadAsync(UpdateRailroadRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        if (request.ParentCtrlNbr <= 0)
            throw new ValidationException("ParentCtrlNbr", "Must be greater than 0");

        if (string.IsNullOrEmpty(request.RrMark))
            throw new ValidationException("RrMark", "Required");

        if (string.IsNullOrEmpty(request.Name))
            throw new ValidationException("Name", "Required");

        await using var uow = await _uowFactory.CreateAsync();

        var railroad = await uow.Railroads.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr)) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Railroad, with control number {request.CtrlNbr}, was not found."));

        railroad.Update(request.ParentCtrlNbr, request.RrMark, request.Name);

        uow.Railroads.Update(railroad);
        await uow.CommitAsync();

        return new UpdateRailroadResponse
        {
            CtrlNbr = railroad.CtrlNbr.Value,
            ParentCtrlNbr = railroad.ParentCtrlNbr.Value,
            RrMark = railroad.RailroadMark,
            Name = railroad.Name.Value,
        };
    }

    public override async Task<DeleteRailroadResponse> DeleteRailroadAsync(DeleteRailroadRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid railroad control number."));

        await using var uow = await _uowFactory.CreateAsync();

        var railroad = await uow.Railroads.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr)) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Railroad, with control number {request.CtrlNbr}, was not found."));

        uow.Railroads.Remove(railroad);
        await uow.CommitAsync();

        return new DeleteRailroadResponse
        {
            CtrlNbr = railroad.CtrlNbr.Value,
            RrMark = railroad.RailroadMark,
            Name = railroad.Name.Value,
        };
    }
}
