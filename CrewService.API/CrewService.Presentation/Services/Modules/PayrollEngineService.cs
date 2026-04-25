using CrewService.Application.Payroll;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class PayrollEngineService(IServiceProvider serviceProvider)
    : PayrollEngineSrvc.PayrollEngineSrvcBase
{
    public override async Task<EarningCodeResultResponse> ResolveEarningCode(
        ResolveEarningCodeRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<EarningCodeResolver>();
        var ctx = new EarningContext(
            request.IsOffDay, request.IsHoliday, request.IsOvertime,
            request.HasAbsenceCode ? request.AbsenceCode : null, null);

        var result = await svc.ResolveAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), ctx, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No matching earning code rule"));

        return new EarningCodeResultResponse
        {
            ResultCode = result.ResultCode,
            RequiresApproval = result.RequiresApproval,
        };
    }

    public override async Task<PayrollRunStatusResponse> CalculateTrial(
        PayrollRunRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<PayrollPeriodService>();
        var run = await svc.CalculateTrialAsync(
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
        var svc = serviceProvider.GetRequiredService<PayrollPeriodService>();
        var run = await svc.LockFinalAsync(
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
