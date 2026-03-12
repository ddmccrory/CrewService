using CrewService.Application.MarkOff;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class MarkOffService(IAbsenceCodeRepository absenceCodeRepo)
    : MarkOffSrvc.MarkOffSrvcBase
{
    public override Task<MarkOffAbsenceResponse> CreateAbsenceRequest(
        CreateAbsenceRequestMsg request, ServerCallContext context)
    {
        var absenceCodeCtrlNbr = ControlNumber.Create(request.AbsenceCodeCtrlNbr);
        var positionSlotCtrlNbr = request.HasPositionSlotCtrlNbr
            ? ControlNumber.Create(request.PositionSlotCtrlNbr) : null;

        var absence = AbsenceRequest.CreateWithCode(
            request.EmployeeCtrlNbr,
            request.StartUtc.ToDateTime(),
            request.EndUtc?.ToDateTime(),
            absenceCodeCtrlNbr,
            "MARKOFF",
            positionSlotCtrlNbr,
            request.IsSystemGenerated,
            request.HasNotes ? request.Notes : null);

        return Task.FromResult(new MarkOffAbsenceResponse
        {
            CtrlNbr = absence.CtrlNbr.Value,
            EmployeeCtrlNbr = absence.EmployeeCtrlNbr.Value,
            ReasonCode = absence.ReasonCode,
            Status = absence.Status,
            StartUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(absence.StartUtc, DateTimeKind.Utc)),
            IsSystemGenerated = absence.IsSystemGenerated,
        });
    }

    public override Task<AbsenceApprovalResponse> ApproveAbsence(
        ApproveAbsenceMsg request, ServerCallContext context)
    {
        return Task.FromResult(new AbsenceApprovalResponse { Status = "APPROVED" });
    }

    public override Task<AbsenceApprovalResponse> DeclineAbsence(
        DeclineAbsenceMsg request, ServerCallContext context)
    {
        return Task.FromResult(new AbsenceApprovalResponse { Status = "DECLINED" });
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
