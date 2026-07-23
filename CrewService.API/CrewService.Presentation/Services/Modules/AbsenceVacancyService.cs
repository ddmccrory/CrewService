using CrewService.Application.AbsenceVacancy;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class AbsenceVacancyService(IServiceProvider serviceProvider) : AbsenceVacancySrvc.AbsenceVacancySrvcBase
{
    public override async Task<AbsenceRequestResponse> SubmitAbsenceRequest(SubmitAbsenceRequestReq request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        var absence = await svc.SubmitAsync(ControlNumber.Create(request.EmployeeCtrlNbr), startUtc, endUtc, request.ReasonCode, request.Notes);
        return MapAbsence(absence);
    }

    public override async Task<GetAbsenceRequestsResponse> GetPendingRequests(GetPendingRequestsReq request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        var pending = await svc.GetPendingAsync();
        var response = new GetAbsenceRequestsResponse { TotalCount = pending.Count };
        foreach (var r in pending) response.Requests.Add(MapAbsence(r));
        return response;
    }

    public override async Task<GetAbsenceRequestsResponse> GetEmployeeRequests(GetEmployeeRequestsReq request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        var requests = await svc.GetByEmployeeAsync(ControlNumber.Create(request.EmployeeCtrlNbr));
        var response = new GetAbsenceRequestsResponse { TotalCount = requests.Count };
        foreach (var r in requests) response.Requests.Add(MapAbsence(r));
        return response;
    }

    public override async Task<AbsenceRequestResponse> ApproveRequest(ApproveAbsenceReq request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        try
        {
            var absence = await svc.ApproveAsync(ControlNumber.Create(request.CtrlNbr), ControlNumber.Create(request.ApprovedByCtrlNbr));
            return MapAbsence(absence);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<AbsenceRequestResponse> DenyRequest(DenyAbsenceReq request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        try
        {
            var absence = await svc.DenyAsync(ControlNumber.Create(request.CtrlNbr), ControlNumber.Create(request.DeniedByCtrlNbr));
            return MapAbsence(absence);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<AbsenceRequestResponse> CancelRequest(CancelAbsenceReq request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        try
        {
            var absence = await svc.CancelAsync(ControlNumber.Create(request.CtrlNbr));
            return MapAbsence(absence);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static AbsenceRequestResponse MapAbsence(AbsenceRequest r) => new()
    {
        CtrlNbr = r.CtrlNbr.Value,
        EmployeeCtrlNbr = r.EmployeeCtrlNbr.Value,
        StartUtc = r.ScheduledStartUtc.ToString("O"),
        EndUtc = r.MarkUps
            .OrderByDescending(m => m.ScheduledMarkUpUtc)
            .Select(m => m.ScheduledMarkUpUtc.ToString("O"))
            .FirstOrDefault() ?? string.Empty,
        ReasonCode = r.ReasonCode,
        Status = r.Status,
        Notes = r.Notes ?? string.Empty
    };
}
