using CrewService.Application.Payroll;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class PayrollEngineService(
    EarningCodeResolver earningCodeResolver,
    PayrollPeriodService periodService)
    : PayrollEngineSrvc.PayrollEngineSrvcBase
{
    public override async Task<EarningCodeResultResponse> ResolveEarningCode(
        ResolveEarningCodeRequest request, ServerCallContext context)
    {
        var ctx = new EarningContext(
            request.IsOffDay, request.IsHoliday, request.IsOvertime,
            request.HasAbsenceCode ? request.AbsenceCode : null, null);

        var result = await earningCodeResolver.ResolveAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), ctx, context.CancellationToken);

        if (result is null)
            throw new RpcException(new Status(StatusCode.NotFound, "No matching earning code rule"));

        return new EarningCodeResultResponse
        {
            ResultCode = result.ResultCode,
            RequiresApproval = result.RequiresApproval,
        };
    }

    public override async Task<PayrollRunStatusResponse> CalculateTrial(
        PayrollRunRequest request, ServerCallContext context)
    {
        var run = await periodService.CalculateTrialAsync(
            ControlNumber.Create(request.RunCtrlNbr), context.CancellationToken);

        return new PayrollRunStatusResponse
        {
            CtrlNbr = run.CtrlNbr.Value,
            Status = run.Status,
            Version = run.Version,
        };
    }

    public override async Task<PayrollRunStatusResponse> LockFinal(
        PayrollRunRequest request, ServerCallContext context)
    {
        var run = await periodService.LockFinalAsync(
            ControlNumber.Create(request.RunCtrlNbr), context.CancellationToken);

        return new PayrollRunStatusResponse
        {
            CtrlNbr = run.CtrlNbr.Value,
            Status = run.Status,
            Version = run.Version,
        };
    }

    public override Task<EarningApprovalStatusResponse> ApproveEarning(
        ApproveEarningRequest request, ServerCallContext context)
    {
        return Task.FromResult(new EarningApprovalStatusResponse
        {
            CtrlNbr = request.ApprovalCtrlNbr,
            Status = request.Approve ? "APPROVED" : "DECLINED",
        });
    }
}
