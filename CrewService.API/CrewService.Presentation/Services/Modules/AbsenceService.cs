using CrewService.Application.AbsenceVacancy;
using CrewService.Application.Authorization;
using CrewService.Application.Absence;
using CrewService.Application.Time;
using CrewService.Application.TenantConfig;
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
    private readonly IRailroadResolver _railroadResolver =
        serviceProvider.GetRequiredService<IRailroadResolver>();
    private readonly IWorkAreaClock _workAreaClock =
        serviceProvider.GetRequiredService<IWorkAreaClock>();

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
            request.IsSystemGenerated,
            request.HasNotes ? request.Notes : null);

        return new MarkOffAbsenceResponse
        {
            CtrlNbr = absence.CtrlNbr.Value,
            EmployeeCtrlNbr = absence.EmployeeCtrlNbr.Value,
            ReasonCode = absence.ReasonCode,
            Status = absence.Status,
            StartUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(absence.ScheduledStartUtc, DateTimeKind.Utc)),
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

    public override async Task<AbsenceApprovalResponse> MarkOffAbsence(
        MarkOffAbsenceMsg request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        try
        {
            var absence = await svc.MarkOffAsync(
                ControlNumber.Create(request.AbsenceRequestCtrlNbr),
                _workAreaClock.UtcNow.UtcDateTime);
            return new AbsenceApprovalResponse { Status = absence.Status };
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

    public override async Task<GetMarkOffAbsenceRequestsResponse> GetAbsenceRequests(
        GetAbsenceRequestsMsg request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        var selectedRailroadCtrlNbr = await GetSelectedRailroadCtrlNbrAsync(context.CancellationToken);
        var requestDateUtc = request.RequestDateUtc is not null
            ? DateTime.SpecifyKind(request.RequestDateUtc.ToDateTime(), DateTimeKind.Utc)
            : DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        var dayRange = await ResolveDayRangeUtcAsync(
            requestDateUtc,
            request.HasWorkAreaGroupCtrlNbr ? request.WorkAreaGroupCtrlNbr : null,
            context.CancellationToken);

        var requests = await svc.GetByDateRangeAsync(
            ControlNumber.Create(selectedRailroadCtrlNbr),
            dayRange.StartUtc,
            dayRange.EndUtc,
            request.IncludeAllStatuses,
            request.HasCraftCtrlNbr ? ControlNumber.Create(request.CraftCtrlNbr) : null,
            request.HasDepartmentCtrlNbr ? ControlNumber.Create(request.DepartmentCtrlNbr) : null,
            context.CancellationToken);

        var displayTimeZone = await ResolveDisplayTimeZoneAsync(
            request.HasWorkAreaGroupCtrlNbr ? request.WorkAreaGroupCtrlNbr : null,
            context.CancellationToken);

        var response = new GetMarkOffAbsenceRequestsResponse { TotalCount = requests.Count };
        foreach (var item in requests)
        {
            response.Requests.Add(MapAbsenceRequest(item, displayTimeZone));
        }

        return response;
    }

    public override async Task<GetMarkOffAbsenceRequestsResponse> GetScheduledAbsences(
        GetScheduledAbsencesMsg request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        var selectedRailroadCtrlNbr = await GetSelectedRailroadCtrlNbrAsync(context.CancellationToken);

        var displayTimeZone = await ResolveDisplayTimeZoneAsync(
            request.HasWorkAreaGroupCtrlNbr ? request.WorkAreaGroupCtrlNbr : null,
            context.CancellationToken);

        var nowUtc = _workAreaClock.UtcNow.UtcDateTime;
        var next24Utc = nowUtc.AddHours(24);

        DateTime todayStartUtc;
        DateTime todayEndUtc;

        if (displayTimeZone is null)
        {
            todayStartUtc = nowUtc.Date;
            todayEndUtc = todayStartUtc.AddDays(1);
        }
        else
        {
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, displayTimeZone);
            var todayLocal = nowLocal.Date;
            var startLocal = DateTime.SpecifyKind(todayLocal, DateTimeKind.Unspecified);
            var endLocal = startLocal.AddDays(1);
            todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, displayTimeZone);
            todayEndUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, displayTimeZone);
        }

        var todayApproved = await svc.GetByDateRangeAsync(
            ControlNumber.Create(selectedRailroadCtrlNbr),
            todayStartUtc,
            todayEndUtc,
            includeAllStatuses: true,
            craftCtrlNbr: request.HasCraftCtrlNbr ? ControlNumber.Create(request.CraftCtrlNbr) : null,
            departmentCtrlNbr: request.HasDepartmentCtrlNbr ? ControlNumber.Create(request.DepartmentCtrlNbr) : null,
            context.CancellationToken);

        var next24Approved = await svc.GetByDateRangeAsync(
            ControlNumber.Create(selectedRailroadCtrlNbr),
            nowUtc,
            next24Utc,
            includeAllStatuses: true,
            craftCtrlNbr: request.HasCraftCtrlNbr ? ControlNumber.Create(request.CraftCtrlNbr) : null,
            departmentCtrlNbr: request.HasDepartmentCtrlNbr ? ControlNumber.Create(request.DepartmentCtrlNbr) : null,
            context.CancellationToken);

        var scheduled = todayApproved
            .Concat(next24Approved)
            .Where(r => string.Equals(r.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => r.CtrlNbr)
            .Select(g => g.First())
            .OrderBy(r => r.ScheduledStartUtc)
            .ToList();

        var response = new GetMarkOffAbsenceRequestsResponse { TotalCount = scheduled.Count };
        foreach (var item in scheduled)
            response.Requests.Add(MapAbsenceRequest(item, displayTimeZone));

        return response;
    }

    public override async Task<GetOpenAbsencesResponse> GetOpenAbsences(
        GetOpenAbsencesMsg request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        var selectedRailroadCtrlNbr = await GetSelectedRailroadCtrlNbrAsync(context.CancellationToken);

        var rangeStart = request.RangeStartUtc is not null
            ? request.RangeStartUtc.ToDateTime()
            : DateTime.UtcNow.Date;

        var rangeEnd = request.RangeEndUtc is not null
            ? request.RangeEndUtc.ToDateTime()
            : rangeStart.AddDays(1);

        var requests = await svc.GetOpenAbsencesByRangeAsync(
            ControlNumber.Create(selectedRailroadCtrlNbr),
            rangeStart,
            rangeEnd,
            request.HasCraftCtrlNbr ? ControlNumber.Create(request.CraftCtrlNbr) : null,
            request.HasDepartmentCtrlNbr ? ControlNumber.Create(request.DepartmentCtrlNbr) : null,
            context.CancellationToken);

        var displayTimeZone = await ResolveDisplayTimeZoneAsync(
            request.HasWorkAreaGroupCtrlNbr ? request.WorkAreaGroupCtrlNbr : null,
            context.CancellationToken);

        var response = new GetOpenAbsencesResponse { TotalCount = requests.Count };
        foreach (var item in requests)
            response.Requests.Add(MapOpenAbsence(item, displayTimeZone));

        return response;
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

    private MarkOffAbsenceRequestListItem MapAbsenceRequest(AbsenceRequest request, TimeZoneInfo? displayTimeZone)
    {
        var scheduledStartUtc = DateTime.SpecifyKind(request.ScheduledStartUtc, DateTimeKind.Utc);
        var item = new MarkOffAbsenceRequestListItem
        {
            CtrlNbr = request.CtrlNbr.Value,
            EmployeeCtrlNbr = request.EmployeeCtrlNbr.Value,
            AbsenceCodeCtrlNbr = request.AbsenceCodeCtrlNbr?.Value ?? 0,
            ReasonCode = request.ReasonCode,
            Status = GetDerivedStatus(request),
            StartUtc = Timestamp.FromDateTime(scheduledStartUtc),
            StartLocal = _workAreaClock.FormatLocalIso(scheduledStartUtc, displayTimeZone),
            IsSystemGenerated = request.IsSystemGenerated,
            IsWaitlisted = string.Equals(request.Status, "WAITLISTED", StringComparison.OrdinalIgnoreCase)
        };

        var latestScheduledMarkUpUtc = request.MarkUps
            .OrderByDescending(m => m.ScheduledMarkUpUtc)
            .Select(m => (DateTime?)DateTime.SpecifyKind(m.ScheduledMarkUpUtc, DateTimeKind.Utc))
            .FirstOrDefault();

        if (latestScheduledMarkUpUtc.HasValue)
        {
            var endUtc = latestScheduledMarkUpUtc.Value;
            item.EndUtc = Timestamp.FromDateTime(endUtc);
            item.EndLocal = _workAreaClock.FormatLocalIso(endUtc, displayTimeZone);
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
            item.Notes = request.Notes;

        return item;
    }

    private MarkOffAbsenceRequestListItem MapOpenAbsence(AbsenceRequest request, TimeZoneInfo? displayTimeZone)
    {
        var item = MapAbsenceRequest(request, displayTimeZone);
        var openStartUtc = request.MarkOffStartUtc.HasValue
            ? DateTime.SpecifyKind(request.MarkOffStartUtc.Value, DateTimeKind.Utc)
            : DateTime.SpecifyKind(request.ScheduledStartUtc, DateTimeKind.Utc);
        item.StartUtc = Timestamp.FromDateTime(openStartUtc);
        item.StartLocal = _workAreaClock.FormatLocalIso(openStartUtc, displayTimeZone);
        item.Status = "OPEN";
        item.IsWaitlisted = false;
        return item;
    }

    private string GetDerivedStatus(AbsenceRequest request)
    {
        if (!string.Equals(request.Status, "EXERCISED", StringComparison.OrdinalIgnoreCase))
            return request.Status;

        var latestActualMarkUpUtc = request.MarkUps
            .Where(m => m.ActualMarkUpUtc.HasValue)
            .OrderByDescending(m => m.ActualMarkUpUtc)
            .Select(m => m.ActualMarkUpUtc)
            .FirstOrDefault();

        if (latestActualMarkUpUtc.HasValue
            && DateTime.SpecifyKind(latestActualMarkUpUtc.Value, DateTimeKind.Utc) <= _workAreaClock.UtcNow.UtcDateTime)
            return "COMPLETED";

        return "EXERCISED";
    }

    private async Task<TimeZoneInfo?> ResolveDisplayTimeZoneAsync(long? workAreaGroupCtrlNbr, CancellationToken ct)
    {
        if (workAreaGroupCtrlNbr is > 0)
            return await _workAreaClock.GetWorkAreaTimeZoneAsync(ControlNumber.Create(workAreaGroupCtrlNbr.Value), ct);

        var actorContext = await _actorContextResolver.ResolveAsync(ct: ct);
        if (actorContext.WorkAreaCtrlNbr is > 0)
            return await _workAreaClock.GetWorkAreaTimeZoneAsync(ControlNumber.Create(actorContext.WorkAreaCtrlNbr.Value), ct);

        return null;
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
        if (actorContext.RailroadCtrlNbr.HasValue && actorContext.RailroadCtrlNbr.Value > 0)
            return actorContext.RailroadCtrlNbr.Value;

        if (actorContext.WorkAreaCtrlNbr.HasValue && actorContext.WorkAreaCtrlNbr.Value > 0)
        {
            await using var uow = await serviceProvider
                .GetRequiredService<CrewService.Domain.Interfaces.IOrchestrationUnitOfWorkFactory>()
                .CreateAsync(cancellationToken: ct);

            var resolved = await _railroadResolver.ResolveFromWorkAreaAsync(
                uow,
                ControlNumber.Create(actorContext.WorkAreaCtrlNbr.Value),
                ct);

            if (resolved is not null)
                return resolved.Value;
        }

        throw new RpcException(new Status(StatusCode.FailedPrecondition, "Railroad context is required."));

    }

    private async Task<(DateTime StartUtc, DateTime EndUtc)> ResolveDayRangeUtcAsync(
        DateTime requestDateUtc,
        long? workAreaGroupCtrlNbr,
        CancellationToken ct)
    {
        var utcDay = requestDateUtc.Date;

        if (workAreaGroupCtrlNbr is null || workAreaGroupCtrlNbr.Value <= 0)
            return (utcDay, utcDay.AddDays(1));

        var timeZone = await _workAreaClock.GetWorkAreaTimeZoneAsync(
            ControlNumber.Create(workAreaGroupCtrlNbr.Value),
            ct);

        if (timeZone is null)
            return (utcDay, utcDay.AddDays(1));

        // requestDateUtc carries the selected calendar day intent from the UI.
        // Do not reinterpret that date by converting the UTC midnight instant to local,
        // or west-of-UTC time zones shift it to the previous day.
        var localDate = requestDateUtc.Date;
        var localStart = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
        var localEnd = localStart.AddDays(1);

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);
        return (startUtc, endUtc);
    }
}
