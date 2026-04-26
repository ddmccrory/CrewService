using CrewService.Application.Payroll;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class PayrollTierService(PayrollTierAppService payrollTierAppService) : PayrollTierSrvc.PayrollTierSrvcBase
{
    public override async Task<GetAllPayrollTierResponse> GetAllAsync(GetAllPayrollTierRequest request, ServerCallContext context)
    {
        var tiers = await payrollTierAppService.GetAllByGroupAsync(
            ControlNumber.Create(request.DynamicGroupCtrlNbr), context.CancellationToken);
        var response = new GetAllPayrollTierResponse();
        response.Tiers.AddRange(tiers.Select(MapToResponse));
        response.TotalCount = tiers.Count;
        return response;
    }

    public override async Task<PayrollTierResponse> GetAsync(GetPayrollTierRequest request, ServerCallContext context)
    {
        try
        {
            var tier = await payrollTierAppService.GetAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapToResponse(tier);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<PayrollTierResponse> CreateAsync(CreatePayrollTierRequest request, ServerCallContext context)
    {
        var tier = await payrollTierAppService.CreateAsync(
            ControlNumber.Create(request.DynamicGroupCtrlNbr), request.NumberOfDays,
            request.TypeOfDay, request.RatePercentage, context.CancellationToken);
        return MapToResponse(tier);
    }

    public override async Task<PayrollTierResponse> UpdateAsync(UpdatePayrollTierRequest request, ServerCallContext context)
    {
        try
        {
            var tier = await payrollTierAppService.UpdateAsync(
                ControlNumber.Create(request.CtrlNbr), request.NumberOfDays,
                request.TypeOfDay, request.RatePercentage, context.CancellationToken);
            return MapToResponse(tier);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAsync(DeletePayrollTierRequest request, ServerCallContext context)
    {
        try
        {
            await payrollTierAppService.DeleteAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static PayrollTierResponse MapToResponse(Domain.Models.Railroads.PayrollTier tier)
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
