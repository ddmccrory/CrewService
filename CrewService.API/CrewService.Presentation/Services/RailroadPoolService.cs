using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class RailroadPoolService(IRailroadPoolRepository poolRepository) : RailroadPoolSrvc.RailroadPoolSrvcBase
{
    private readonly IRailroadPoolRepository _poolRepository = poolRepository;

    public override async Task<GetAllRailroadPoolResponse> GetAllAsync(GetAllRailroadPoolRequest request, ServerCallContext context)
    {
        var response = new GetAllRailroadPoolResponse();
        var pools = await _poolRepository.GetByRailroadCtrlNbrAsync(ControlNumber.Create(request.RailroadCtrlNbr));
        response.Pools.AddRange(pools.Select(MapToResponse));
        response.TotalCount = pools.Count;
        return response;
    }

    public override async Task<RailroadPoolResponse> GetAsync(GetRailroadPoolRequest request, ServerCallContext context)
    {
        var pool = await _poolRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"RailroadPool {request.CtrlNbr} not found."));
        return MapToResponse(pool);
    }

    public override async Task<RailroadPoolResponse> CreateAsync(CreateRailroadPoolRequest request, ServerCallContext context)
    {
        var pool = RailroadPool.Create(request.RailroadCtrlNbr, request.PoolName, request.PoolNumber);
        await _poolRepository.AddAsync(pool);
        return MapToResponse(pool);
    }

    public override async Task<RailroadPoolResponse> UpdateAsync(UpdateRailroadPoolRequest request, ServerCallContext context)
    {
        var pool = await _poolRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"RailroadPool {request.CtrlNbr} not found."));
        pool.Update(request.PoolName, request.PoolNumber);
        await _poolRepository.UpdateAsync(pool);
        return MapToResponse(pool);
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteRailroadPoolRequest request, ServerCallContext context)
    {
        await _poolRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    private static RailroadPoolResponse MapToResponse(RailroadPool pool)
    {
        return new RailroadPoolResponse
        {
            CtrlNbr = pool.CtrlNbr.Value,
            PoolName = pool.PoolName,
            PoolNumber = pool.PoolNumber
        };
    }
}