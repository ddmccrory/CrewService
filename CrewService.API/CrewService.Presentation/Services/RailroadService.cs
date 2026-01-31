using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class RailroadService(IRailroadRepository railroadRepository) : RailroadSrvc.RailroadSrvcBase
{
    private readonly IRailroadRepository _railroadRepository = railroadRepository;

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

        return await Task.FromResult(response);
    }

    public override async Task<GetAllParentRailroadsResponse> GetAllParentRailroadsAsync(GetAllParentRailroadsRequest request, ServerCallContext context)
    {
        var response = new GetAllParentRailroadsResponse();
        var railroads = await _railroadRepository.GetAllRailroadsByParentCtrlNbrAsync(request.ParentCtrlNbr);
       
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

        return await Task.FromResult(response);
    }

    public override async Task<GetRailroadResponse> GetRailroadAsync(GetRailroadRequest request, ServerCallContext context)
    {
        var railroad = await _railroadRepository.GetByCtrlNbrAsync(request.CtrlNbr);

        return railroad is null
            ? throw new RpcException(new Status(StatusCode.NotFound, $"Railroad, with control number {request.CtrlNbr}, was not found."))
            : await Task.FromResult(new GetRailroadResponse
            {
                CtrlNbr = railroad.CtrlNbr.Value,
                RrMark = railroad.RailroadMark,
                Name = railroad.Name.Value,
            });
    }

    public override async Task<CreateRailroadResponse> CreateRailroadAsync(CreateRailroadRequest request, ServerCallContext context)
    {
        if (request is null || string.IsNullOrEmpty(request.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid railroad object."));

        var railroad = Railroad.Create(request.ParentCtrlNbr, request.RrMark, request.Name);

        _railroadRepository.Add(railroad);

        return await Task.FromResult(new CreateRailroadResponse
        {
            CtrlNbr = railroad.CtrlNbr.Value,
            ParentCtrlNbr = railroad.ParentCtrlNbr.Value,
            RrMark = railroad.RailroadMark,
            Name = railroad.Name.Value,
        });
    }

    public override async Task<UpdateRailroadResponse> UpdateRailroadAsync(UpdateRailroadRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid railroad control number."));

        if (request.ParentCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid railroad parent control number."));

        if (string.IsNullOrEmpty(request.RrMark))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid railroad name."));

        if (string.IsNullOrEmpty(request.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid railroad name."));

        var railroad = await _railroadRepository.GetByCtrlNbrAsync(request.CtrlNbr) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Railroad, with control number {request.CtrlNbr}, was not found."));

        railroad.Update(request.ParentCtrlNbr, request.RrMark, request.Name);

        _railroadRepository.Update(railroad);

        return await Task.FromResult(new UpdateRailroadResponse
        {
            CtrlNbr = railroad.CtrlNbr.Value,
            ParentCtrlNbr = railroad.ParentCtrlNbr.Value,
            RrMark = railroad.RailroadMark,
            Name = railroad.Name.Value,
        }); ;
    }

    public override async Task<DeleteRailroadResponse> DeleteRailroadAsync(DeleteRailroadRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid railroad control number."));

        var railroad = await _railroadRepository.GetByCtrlNbrAsync(request.CtrlNbr) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Railroad, with control number {request.CtrlNbr}, was not found."));

        _railroadRepository.Remove(railroad);

        return await Task.FromResult(new DeleteRailroadResponse
        {
            CtrlNbr = railroad.CtrlNbr.Value,
            RrMark = railroad.RailroadMark,
            Name = railroad.Name.Value,
        });
    }
}
