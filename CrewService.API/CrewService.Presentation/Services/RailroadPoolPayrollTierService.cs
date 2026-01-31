using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class RailroadPoolPayrollTierService(IRailroadPoolPayrollTierRepository tierRepository) : RailroadPoolPayrollTierSrvc.RailroadPoolPayrollTierSrvcBase
{
    private readonly IRailroadPoolPayrollTierRepository _tierRepository = tierRepository;

    public override async Task<GetAllRailroadPoolPayrollTierResponse> GetAllAsync(GetAllRailroadPoolPayrollTierRequest request, ServerCallContext context)
    {
        var response = new GetAllRailroadPoolPayrollTierResponse();
        var tiers = await _tierRepository.GetAllByPoolAsync(ControlNumber.Create(request.RailroadPoolCtrlNbr));
        response.Tiers.AddRange(tiers.Select(MapToResponse));
        response.TotalCount = tiers.Count;
        return response;
    }

    public override async Task<RailroadPoolPayrollTierResponse> GetAsync(GetRailroadPoolPayrollTierRequest request, ServerCallContext context)
    {
        var tier = await _tierRepository.GetByIdAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"RailroadPoolPayrollTier {request.CtrlNbr} not found."));
        return MapToResponse(tier);
    }

    public override async Task<RailroadPoolPayrollTierResponse> CreateAsync(CreateRailroadPoolPayrollTierRequest request, ServerCallContext context)
    {
        var tier = RailroadPoolPayrollTier.Create(request.RailroadPoolCtrlNbr, request.NumberOfDays, request.TypeOfDay, request.RatePercentage);
        await _tierRepository.AddAsync(tier);
        return MapToResponse(tier);
    }

    public override async Task<RailroadPoolPayrollTierResponse> UpdateAsync(UpdateRailroadPoolPayrollTierRequest request, ServerCallContext context)
    {
        var tier = await _tierRepository.GetByIdAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"RailroadPoolPayrollTier {request.CtrlNbr} not found."));
        tier.Update(request.NumberOfDays, request.TypeOfDay, request.RatePercentage);
        await _tierRepository.UpdateAsync(tier);
        return MapToResponse(tier);
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteRailroadPoolPayrollTierRequest request, ServerCallContext context)
    {
        await _tierRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    private static RailroadPoolPayrollTierResponse MapToResponse(RailroadPoolPayrollTier tier)
    {
        return new RailroadPoolPayrollTierResponse
        {
            CtrlNbr = tier.CtrlNbr.Value,
            RailroadPoolCtrlNbr = tier.RailroadPoolCtrlNbr.Value,
            NumberOfDays = tier.NumberOfDays,
            TypeOfDay = tier.TypeOfDay,
            RatePercentage = tier.RatePercentage
        };
    }
}