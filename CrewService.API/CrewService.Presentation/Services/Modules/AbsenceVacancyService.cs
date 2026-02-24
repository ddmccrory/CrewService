using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class AbsenceVacancyService(
    IAbsenceRequestRepository absenceRequestRepository) : AbsenceVacancySrvc.AbsenceVacancySrvcBase
{
    public override async Task<AbsenceRequestResponse> SubmitAbsenceRequest(SubmitAbsenceRequestReq request, ServerCallContext context)
    {
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        var absence = AbsenceRequest.Create(request.EmployeeCtrlNbr, startUtc, endUtc, request.ReasonCode, request.Notes);
        await absenceRequestRepository.AddAsync(absence);
        return MapAbsence(absence);
    }

    public override async Task<GetAbsenceRequestsResponse> GetPendingRequests(GetPendingRequestsReq request, ServerCallContext context)
    {
        var pending = await absenceRequestRepository.GetPendingAsync();
        var response = new GetAbsenceRequestsResponse { TotalCount = pending.Count };
        foreach (var r in pending) response.Requests.Add(MapAbsence(r));
        return response;
    }

    public override async Task<GetAbsenceRequestsResponse> GetEmployeeRequests(GetEmployeeRequestsReq request, ServerCallContext context)
    {
        var requests = await absenceRequestRepository.GetByEmployeeAsync(ControlNumber.Create(request.EmployeeCtrlNbr));
        var response = new GetAbsenceRequestsResponse { TotalCount = requests.Count };
        foreach (var r in requests) response.Requests.Add(MapAbsence(r));
        return response;
    }

    public override async Task<AbsenceRequestResponse> ApproveRequest(ApproveAbsenceReq request, ServerCallContext context)
    {
        var absence = await absenceRequestRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Absence request {request.CtrlNbr} not found."));
        absence.Approve(request.ApprovedByCtrlNbr);
        await absenceRequestRepository.UpdateAsync(absence);
        return MapAbsence(absence);
    }

    public override async Task<AbsenceRequestResponse> DenyRequest(DenyAbsenceReq request, ServerCallContext context)
    {
        var absence = await absenceRequestRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Absence request {request.CtrlNbr} not found."));
        absence.Deny(request.DeniedByCtrlNbr);
        await absenceRequestRepository.UpdateAsync(absence);
        return MapAbsence(absence);
    }

    public override async Task<AbsenceRequestResponse> CancelRequest(CancelAbsenceReq request, ServerCallContext context)
    {
        var absence = await absenceRequestRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Absence request {request.CtrlNbr} not found."));
        absence.Cancel();
        await absenceRequestRepository.UpdateAsync(absence);
        return MapAbsence(absence);
    }

    private static AbsenceRequestResponse MapAbsence(AbsenceRequest r) => new()
    {
        CtrlNbr = r.CtrlNbr.Value,
        EmployeeCtrlNbr = r.EmployeeCtrlNbr.Value,
        StartUtc = r.StartUtc.ToString("O"),
        EndUtc = r.EndUtc?.ToString("O") ?? string.Empty,
        ReasonCode = r.ReasonCode,
        Status = r.Status,
        Notes = r.Notes ?? string.Empty
    };
}
