using CrewService.Application.AbsenceVacancy;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class MarkOffService(IServiceProvider serviceProvider)
    : MarkOffSrvc.MarkOffSrvcBase
{
    public override async Task<MarkOffAbsenceResponse> CreateAbsenceRequest(
        CreateAbsenceRequestMsg request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        var absence = await svc.SubmitWithCodeAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            request.StartUtc.ToDateTime(),
            request.EndUtc?.ToDateTime(),
            ControlNumber.Create(request.AbsenceCodeCtrlNbr),
            "MARKOFF",
            request.HasPositionSlotCtrlNbr ? ControlNumber.Create(request.PositionSlotCtrlNbr) : null,
            request.IsSystemGenerated,
            request.HasNotes ? request.Notes : null);

        return new MarkOffAbsenceResponse
        {
            CtrlNbr = absence.CtrlNbr.Value,
            EmployeeCtrlNbr = absence.EmployeeCtrlNbr.Value,
            ReasonCode = absence.ReasonCode,
            Status = absence.Status,
            StartUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(absence.StartUtc, DateTimeKind.Utc)),
            IsSystemGenerated = absence.IsSystemGenerated,
        };
    }

    public override async Task<AbsenceApprovalResponse> ApproveAbsence(
        ApproveAbsenceMsg request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        try
        {
            var absence = await svc.ApproveAsync(
                ControlNumber.Create(request.AbsenceRequestCtrlNbr),
                ControlNumber.Create(request.OfficerCtrlNbr));
            return new AbsenceApprovalResponse { Status = absence.Status };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<AbsenceApprovalResponse> DeclineAbsence(
        DeclineAbsenceMsg request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        try
        {
            var absence = await svc.DenyAsync(
                ControlNumber.Create(request.AbsenceRequestCtrlNbr),
                ControlNumber.Create(request.OfficerCtrlNbr));
            return new AbsenceApprovalResponse { Status = absence.Status };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override Task<CompensationBalanceResponse> GetCompensationBalance(
        GetCompensationBalanceMsg request, ServerCallContext context)
    {
        return Task.FromResult(new CompensationBalanceResponse());
    }

    public override Task<GetAbsenceCodesResponse> GetAbsenceCodes(
        GetAbsenceCodesMsg request, ServerCallContext context)
    {
        return Task.FromResult(new GetAbsenceCodesResponse());
    }
}
