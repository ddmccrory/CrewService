using CrewService.Application.AbsenceVacancy;
using CrewService.Application.Authorization;
using CrewService.Application.Absence;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class AbsenceService(IServiceProvider serviceProvider)
    : MarkOffSrvc.MarkOffSrvcBase
{
    private readonly IRequestActorContextResolver _actorContextResolver =
        serviceProvider.GetRequiredService<IRequestActorContextResolver>();

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

    public override async Task<GetMarkOffCodesResponse> GetMarkOffCodes(
        GetMarkOffCodesMsg request,
        ServerCallContext context)
    {
        var selectedRailroadCtrlNbr = await GetSelectedRailroadCtrlNbrAsync(context.CancellationToken);
        var svc = serviceProvider.GetRequiredService<AbsenceCodeService>();
        var codes = await svc.GetCodesAsync(
            ControlNumber.Create(selectedRailroadCtrlNbr),
            request.ActiveOnly,
            context.CancellationToken);

        var response = new GetMarkOffCodesResponse { TotalCount = codes.Count };
        foreach (var code in codes)
            response.Codes.Add(MapMarkOffCode(code));

        return response;
    }

    public override async Task<MarkOffCodeResponse> CreateMarkOffCode(
        CreateMarkOffCodeMsg request,
        ServerCallContext context)
    {
        var selectedRailroadCtrlNbr = await GetSelectedRailroadCtrlNbrAsync(context.CancellationToken);
        var svc = serviceProvider.GetRequiredService<AbsenceCodeService>();
        try
        {
            var code = await svc.CreateCodeAsync(
                ControlNumber.Create(selectedRailroadCtrlNbr),
                request.Code,
                request.Description,
                request.IsExcused,
                request.IsCompensated,
                request.RequiresApproval,
                request.IsSystemOnly,
                request.IsHolidayExempt,
                request.HasDefaultAutoMarkUpHours ? Convert.ToDecimal(request.DefaultAutoMarkUpHours) : null,
                request.IsActive,
                context.CancellationToken);

            return MapMarkOffCode(code);
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<MarkOffCodeResponse> UpdateMarkOffCode(
        UpdateMarkOffCodeMsg request,
        ServerCallContext context)
    {
        var selectedRailroadCtrlNbr = await GetSelectedRailroadCtrlNbrAsync(context.CancellationToken);
        var svc = serviceProvider.GetRequiredService<AbsenceCodeService>();
        try
        {
            var code = await svc.UpdateCodeAsync(
                ControlNumber.Create(selectedRailroadCtrlNbr),
                ControlNumber.Create(request.CtrlNbr),
                request.Code,
                request.Description,
                request.IsExcused,
                request.IsCompensated,
                request.RequiresApproval,
                request.IsSystemOnly,
                request.IsHolidayExempt,
                request.HasDefaultAutoMarkUpHours ? Convert.ToDecimal(request.DefaultAutoMarkUpHours) : null,
                request.IsActive,
                context.CancellationToken);

            return MapMarkOffCode(code);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteMarkOffCode(
        DeleteMarkOffCodeMsg request,
        ServerCallContext context)
    {
        var selectedRailroadCtrlNbr = await GetSelectedRailroadCtrlNbrAsync(context.CancellationToken);
        var svc = serviceProvider.GetRequiredService<AbsenceCodeService>();
        try
        {
            await svc.DeleteCodeAsync(
                ControlNumber.Create(selectedRailroadCtrlNbr),
                ControlNumber.Create(request.CtrlNbr),
                context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static MarkOffCodeResponse MapMarkOffCode(AbsenceCode code)
    {
        var response = new MarkOffCodeResponse
        {
            CtrlNbr = code.CtrlNbr.Value,
            Code = code.Code,
            Description = code.Description,
            IsExcused = code.IsExcused,
            IsCompensated = code.IsCompensated,
            RequiresApproval = code.RequiresApproval,
            IsSystemOnly = code.IsSystemOnly,
            IsHolidayExempt = code.IsHolidayExempt,
            IsActive = code.IsActive
        };

        if (code.DefaultAutoMarkUpHours.HasValue)
            response.DefaultAutoMarkUpHours = Convert.ToDouble(code.DefaultAutoMarkUpHours.Value);

        return response;
    }

    private async Task<long> GetSelectedRailroadCtrlNbrAsync(CancellationToken ct)
    {
        var actorContext = await _actorContextResolver.ResolveAsync(ct: ct);
        if (!actorContext.RailroadCtrlNbr.HasValue || actorContext.RailroadCtrlNbr.Value <= 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Railroad context is required."));

        return actorContext.RailroadCtrlNbr.Value;
    }
}
