using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class PayrollTierService(IPayrollTierRepository tierRepository) : PayrollTierSrvc.PayrollTierSrvcBase
{
    private readonly IPayrollTierRepository _tierRepository = tierRepository;

    public override async Task<GetAllPayrollTierResponse> GetAllAsync(GetAllPayrollTierRequest request, ServerCallContext context)
    {
        var response = new GetAllPayrollTierResponse();
        var tiers = await _tierRepository.GetByDynamicGroupCtrlNbrAsync(ControlNumber.Create(request.DynamicGroupCtrlNbr));
        response.Tiers.AddRange(tiers.Select(MapToResponse));
        response.TotalCount = tiers.Count;
        return response;
    }

    public override async Task<PayrollTierResponse> GetAsync(GetPayrollTierRequest request, ServerCallContext context)
    {
        var tier = await _tierRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"PayrollTier {request.CtrlNbr} not found."));
        return MapToResponse(tier);
    }

    public override async Task<PayrollTierResponse> CreateAsync(CreatePayrollTierRequest request, ServerCallContext context)
    {
        var tier = PayrollTier.Create(request.DynamicGroupCtrlNbr, request.NumberOfDays, request.TypeOfDay, request.RatePercentage);
        await _tierRepository.AddAsync(tier);
        return MapToResponse(tier);
    }

    public override async Task<PayrollTierResponse> UpdateAsync(UpdatePayrollTierRequest request, ServerCallContext context)
    {
        var tier = await _tierRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"PayrollTier {request.CtrlNbr} not found."));
        tier.Update(request.NumberOfDays, request.TypeOfDay, request.RatePercentage);
        await _tierRepository.UpdateAsync(tier);
        return MapToResponse(tier);
    }

    public override async Task<DeleteResponse> DeleteAsync(DeletePayrollTierRequest request, ServerCallContext context)
    {
        await _tierRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    private static PayrollTierResponse MapToResponse(PayrollTier tier)
    {
        return new PayrollTierResponse
        {
            CtrlNbr = tier.CtrlNbr.Value,
            DynamicGroupCtrlNbr = tier.DynamicGroupCtrlNbr.Value,
            NumberOfDays = tier.NumberOfDays,
            TypeOfDay = tier.TypeOfDay,
            RatePercentage = tier.RatePercentage
        };
    }
}
