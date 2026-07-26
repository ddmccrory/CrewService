using CrewService.Application.AbsenceVacancy;
using CrewService.Application.Authorization;
using CrewService.Application.Absence;
using CrewService.Application.Time;
using CrewService.Application.TenantConfig;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Interfaces;
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
    private readonly IOrchestrationUnitOfWorkFactory _uowFactory =
        serviceProvider.GetRequiredService<IOrchestrationUnitOfWorkFactory>();

    public override async Task<MarkOffAbsenceResponse> CreateAbsenceRequest(
        CreateAbsenceRequestMsg request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        var actorContext = await _actorContextResolver.ResolveAsync(
            requestedEmployeeCtrlNbr: request.EmployeeCtrlNbr,
            ct: context.CancellationToken);

        var approvedByCtrlNbr = request.HasApprovedByCtrlNbr
            ? ControlNumber.Create(request.ApprovedByCtrlNbr)
            : (ControlNumber?)null;

        if (approvedByCtrlNbr is not null)
        {
            var createContext = await ResolveCreateApprovalContextAsync(
                svc,
                request.AbsenceCodeCtrlNbr,
                actorContext,
                context.CancellationToken);

            var allowedOfficerCtrlNbrs = createContext.Approvers.Select(a => a.OfficerCtrlNbr).ToHashSet();
            if (!allowedOfficerCtrlNbrs.Contains(approvedByCtrlNbr.Value))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Selected Approved By is not allowed for this request."));
        }

        var absence = await svc.SubmitWithCodeAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            request.StartUtc.ToDateTime(),
            request.EndUtc?.ToDateTime(),
            ControlNumber.Create(request.AbsenceCodeCtrlNbr),
            "MARKOFF",
            request.IsSystemGenerated,
            request.HasNotes ? request.Notes : null,
            approvedByCtrlNbr,
            request.AutoMarkOffOnApproval,
            _workAreaClock.UtcNow.UtcDateTime);

        if (absence.IsWaitListed)
        {
            var waitListRecord = absence.WaitListRecord
                ?? throw new RpcException(new Status(StatusCode.Internal, "Waitlist result did not return a waitlist record."));

            return new MarkOffAbsenceResponse
            {
                CtrlNbr = waitListRecord.CtrlNbr.Value,
                EmployeeCtrlNbr = waitListRecord.EmployeeCtrlNbr.Value,
                ReasonCode = "WAITLISTED",
                Status = "WAITLISTED",
                StartUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(waitListRecord.RequestDateUtc, DateTimeKind.Utc).AddMinutes(1)),
                IsSystemGenerated = false
            };
        }

        var createdAbsence = absence.AbsenceRequest
            ?? throw new RpcException(new Status(StatusCode.Internal, "Absence result did not return an absence request."));

        return new MarkOffAbsenceResponse
        {
            CtrlNbr = createdAbsence.CtrlNbr.Value,
            EmployeeCtrlNbr = createdAbsence.EmployeeCtrlNbr.Value,
            ReasonCode = createdAbsence.ReasonCode,
            Status = createdAbsence.DerivedStatus,
            StartUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(createdAbsence.ScheduledStartUtc, DateTimeKind.Utc)),
            IsSystemGenerated = createdAbsence.IsSystemGenerated
        };
    }

    public override async Task<GetEndAbsenceLocalPresetResponse> GetEndAbsenceLocalPreset(
        GetEndAbsenceLocalPresetMsg request,
        ServerCallContext context)
    {
        var timeZone = await ResolveDisplayTimeZoneAsync(
            request.HasWorkAreaGroupCtrlNbr ? request.WorkAreaGroupCtrlNbr : null,
            context.CancellationToken);

        if (timeZone is null)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Work area time zone context is required."));

        DateTime baseLocal;
        if (request.HasBaseLocal)
        {
            if (!DateTime.TryParse(request.BaseLocal, out var parsedBaseLocal))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid base_local is required. Use yyyy-MM-ddTHH:mm."));

            baseLocal = DateTime.SpecifyKind(parsedBaseLocal, DateTimeKind.Unspecified);
        }
        else
        {
            baseLocal = TimeZoneInfo.ConvertTimeFromUtc(_workAreaClock.UtcNow.UtcDateTime, timeZone);
        }

        var resolvedLocal = request.NextDay
            ? baseLocal.Date.AddDays(1).AddMinutes(1)
            : baseLocal;

        return new GetEndAbsenceLocalPresetResponse
        {
            EndLocal = resolvedLocal.ToString("yyyy-MM-ddTHH:mm")
        };
    }

    public override async Task<GetAbsenceApprovalContextResponse> GetAbsenceApprovalContext(
        GetAbsenceApprovalContextMsg request,
        ServerCallContext context)
    {
        if (request.AbsenceRequestCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Absence request control number is required."));

        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();

        await using var uow = await _uowFactory.CreateAsync(cancellationToken: context.CancellationToken);
        var absenceRequest = await uow.AbsenceRequests.GetByCtrlNbrAsync(ControlNumber.Create(request.AbsenceRequestCtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Absence request {request.AbsenceRequestCtrlNbr} not found."));

        if (absenceRequest.AbsenceCodeCtrlNbr is null)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Absence request is missing an absence code."));

        var actorContext = await _actorContextResolver.ResolveAsync(ct: context.CancellationToken);
        if (!actorContext.ParentCtrlNbr.HasValue || actorContext.ParentCtrlNbr.Value <= 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Parent context is required."));

        var selectedRailroadCtrlNbr = await GetSelectedRailroadCtrlNbrAsync(context.CancellationToken);

        var policy = await svc.ResolveApprovalPolicyAsync(absenceRequest.AbsenceCodeCtrlNbr, context.CancellationToken);
        var approvers = await svc.GetApprovalOfficersAsync(
            ControlNumber.Create(actorContext.ParentCtrlNbr.Value),
            ControlNumber.Create(selectedRailroadCtrlNbr),
            policy.Level,
            context.CancellationToken);

        var response = new GetAbsenceApprovalContextResponse
        {
            ApprovalLevel = policy.Level.ToString(),
            ApprovalLevelDescription = policy.Description,
            DefaultOfficerCtrlNbr = approvers.FirstOrDefault()?.OfficerCtrlNbr ?? 0,
            CanSelectApprovedBy = true
        };

        response.Approvers.AddRange(approvers.Select(a => new AbsenceApprovalOfficerItem
        {
            OfficerCtrlNbr = a.OfficerCtrlNbr,
            DisplayName = a.FullName,
            EmployeeNumber = a.EmployeeNumber,
            Role = a.Role ?? string.Empty
        }));

        return response;
    }

    public override async Task<GetMarkOffAbsenceRequestsResponse> GetAbsenceApprovals(
        GetAbsenceApprovalsMsg request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        var selectedRailroadCtrlNbr = await GetSelectedRailroadCtrlNbrAsync(context.CancellationToken);
        var railroadCtrlNbr = ControlNumber.Create(selectedRailroadCtrlNbr);

        var displayTimeZone = await ResolveDisplayTimeZoneAsync(
            request.HasWorkAreaGroupCtrlNbr ? request.WorkAreaGroupCtrlNbr : null,
            context.CancellationToken);

        var requests = await svc.GetByDateRangeAsync(
            railroadCtrlNbr,
            DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc),
            DateTime.SpecifyKind(new DateTime(2100, 1, 1), DateTimeKind.Utc),
            includeAllStatuses: true,
            request.HasCraftCtrlNbr ? ControlNumber.Create(request.CraftCtrlNbr) : null,
            request.HasDepartmentCtrlNbr ? ControlNumber.Create(request.DepartmentCtrlNbr) : null,
            context.CancellationToken);

        requests = requests
            .Where(AbsenceStatusHelper.IsPending)
            .ToList();

        if (request.HasEmployeeCtrlNbr)
        {
            var employeeCtrlNbr = ControlNumber.Create(request.EmployeeCtrlNbr);
            requests = requests
                .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr)
                .ToList();
        }

        requests = requests
            .OrderBy(r => r.ScheduledStartUtc)
            .ThenBy(r => r.CtrlNbr)
            .ToList();

        var response = new GetMarkOffAbsenceRequestsResponse { TotalCount = requests.Count };
        foreach (var item in requests)
            response.Requests.Add(MapAbsenceRequest(item, displayTimeZone));

        return response;
    }

    public override async Task<GetMarkOffAbsenceRequestsResponse> GetAbsenceHistory(
        GetAbsenceHistoryMsg request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        var selectedRailroadCtrlNbr = await GetSelectedRailroadCtrlNbrAsync(context.CancellationToken);
        var railroadCtrlNbr = ControlNumber.Create(selectedRailroadCtrlNbr);

        var displayTimeZone = await ResolveDisplayTimeZoneAsync(
            request.HasWorkAreaGroupCtrlNbr ? request.WorkAreaGroupCtrlNbr : null,
            context.CancellationToken);

        var fromDateUtc = request.FromDateUtc is not null
            ? DateTime.SpecifyKind(request.FromDateUtc.ToDateTime(), DateTimeKind.Utc)
            : DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-7), DateTimeKind.Utc);

        var fromRange = await ResolveDayRangeUtcAsync(
            fromDateUtc,
            request.HasWorkAreaGroupCtrlNbr ? request.WorkAreaGroupCtrlNbr : null,
            context.CancellationToken);

        DateTime? toRangeEndUtc = null;
        if (request.ToDateUtc is not null)
        {
            var toDateUtc = DateTime.SpecifyKind(request.ToDateUtc.ToDateTime(), DateTimeKind.Utc);
            var toRange = await ResolveDayRangeUtcAsync(
                toDateUtc,
                request.HasWorkAreaGroupCtrlNbr ? request.WorkAreaGroupCtrlNbr : null,
                context.CancellationToken);
            toRangeEndUtc = toRange.EndUtc;
        }

        List<AbsenceRequest> requests;
        if (request.HasEmployeeCtrlNbr && !request.HasCraftCtrlNbr && !request.HasDepartmentCtrlNbr)
        {
            requests = await svc.GetByEmployeeAsync(ControlNumber.Create(request.EmployeeCtrlNbr));
        }
        else
        {
            requests = await svc.GetByDateRangeAsync(
                railroadCtrlNbr,
                DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc),
                _workAreaClock.UtcNow.UtcDateTime.AddDays(1),
                includeAllStatuses: true,
                request.HasCraftCtrlNbr ? ControlNumber.Create(request.CraftCtrlNbr) : null,
                request.HasDepartmentCtrlNbr ? ControlNumber.Create(request.DepartmentCtrlNbr) : null,
                context.CancellationToken);
        }

        requests = requests
            .Where(AbsenceStatusHelper.IsComplete)
            .Where(r => ResolveCompletedAtUtc(r) >= fromRange.StartUtc)
            .Where(r => !toRangeEndUtc.HasValue || ResolveCompletedAtUtc(r) < toRangeEndUtc.Value)
            .ToList();

        if (request.HasEmployeeCtrlNbr)
        {
            var employeeCtrlNbr = ControlNumber.Create(request.EmployeeCtrlNbr);
            requests = requests
                .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr)
                .ToList();
        }

        requests = requests
            .OrderByDescending(ResolveHistorySortUtc)
            .ThenByDescending(r => r.CtrlNbr)
            .ToList();

        var response = new GetMarkOffAbsenceRequestsResponse { TotalCount = requests.Count };
        foreach (var item in requests)
            response.Requests.Add(MapAbsenceRequest(item, displayTimeZone));

        return response;
    }

    public override async Task<GetAbsenceApprovalContextResponse> GetCreateAbsenceApprovalContext(
        GetCreateAbsenceApprovalContextMsg request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        var actorContext = await _actorContextResolver.ResolveAsync(
            requestedEmployeeCtrlNbr: request.EmployeeCtrlNbr > 0 ? request.EmployeeCtrlNbr : null,
            ct: context.CancellationToken);

        return await ResolveCreateApprovalContextAsync(
            svc,
            request.AbsenceCodeCtrlNbr,
            actorContext,
            context.CancellationToken);
    }

    private static DateTime ResolveHistorySortUtc(AbsenceRequest request)
    {
        var completedAtUtc = ResolveCompletedAtUtc(request);
        if (completedAtUtc.HasValue)
            return completedAtUtc.Value;

        if (request.ScheduledEndUtc.HasValue)
            return DateTime.SpecifyKind(request.ScheduledEndUtc.Value, DateTimeKind.Utc);

        return DateTime.SpecifyKind(request.ScheduledStartUtc, DateTimeKind.Utc);
    }

    private static DateTime? ResolveCompletedAtUtc(AbsenceRequest request)
    {
        if (request.EndRecords.Count == 0)
            return null;

        return DateTime.SpecifyKind(request.EndRecords.Max(r => r.ActualEndUtc), DateTimeKind.Utc);
    }

    public override async Task<GetCreateAbsenceEndLocalPresetResponse> GetCreateAbsenceEndLocalPreset(
        GetCreateAbsenceEndLocalPresetMsg request,
        ServerCallContext context)
    {
        if (request.AbsenceCodeCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Absence code control number is required."));

        if (string.IsNullOrWhiteSpace(request.StartLocal)
            || !DateTime.TryParse(request.StartLocal, out var startLocalParsed))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid start_local is required. Use yyyy-MM-ddTHH:mm."));
        }

        var selectedRailroadCtrlNbr = await GetSelectedRailroadCtrlNbrAsync(context.CancellationToken);
        var svc = serviceProvider.GetRequiredService<AbsenceCodeService>();
        var markOffCode = (await svc.GetCodesAsync(
                ControlNumber.Create(selectedRailroadCtrlNbr),
                activeOnly: false,
                context.CancellationToken))
            .FirstOrDefault(c => c.CtrlNbr == ControlNumber.Create(request.AbsenceCodeCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Absence code {request.AbsenceCodeCtrlNbr} not found."));

        if (!markOffCode.DefaultAutoMarkUpHours.HasValue || markOffCode.DefaultAutoMarkUpHours.Value <= 0)
            return new GetCreateAbsenceEndLocalPresetResponse { EndLocal = string.Empty };

        var startLocal = DateTime.SpecifyKind(startLocalParsed, DateTimeKind.Unspecified);
        var endLocal = startLocal.AddHours(Convert.ToDouble(markOffCode.DefaultAutoMarkUpHours.Value));

        return new GetCreateAbsenceEndLocalPresetResponse
        {
            EndLocal = endLocal.ToString("yyyy-MM-ddTHH:mm")
        };
    }

    public override async Task<GetCreateAbsenceStartProposalResponse> GetCreateAbsenceStartProposal(
        GetCreateAbsenceStartProposalMsg request,
        ServerCallContext context)
    {
        if (request.EmployeeCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Employee control number is required."));

        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        var proposal = await svc.GetStartProposalAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            context.CancellationToken);

        var displayTimeZone = await ResolveDisplayTimeZoneAsync(
            request.HasWorkAreaGroupCtrlNbr ? request.WorkAreaGroupCtrlNbr : null,
            context.CancellationToken);

        var startUtcKind = DateTime.SpecifyKind(proposal.StartUtc, DateTimeKind.Utc);
        var startLocal = displayTimeZone is null
            ? startUtcKind
            : TimeZoneInfo.ConvertTimeFromUtc(startUtcKind, displayTimeZone);

        return new GetCreateAbsenceStartProposalResponse
        {
            StartUtc = Timestamp.FromDateTime(startUtcKind),
            StartLocal = startLocal.ToString("yyyy-MM-ddTHH:mm"),
            RequestWindowCapDays = proposal.RequestWindowCapDays ?? 0
        };
    }

    private async Task<GetAbsenceApprovalContextResponse> ResolveCreateApprovalContextAsync(
        AbsenceRequestService svc,
        long absenceCodeCtrlNbr,
        Application.Authorization.RequestActorContext actorContext,
        CancellationToken ct)
    {
        if (!actorContext.ParentCtrlNbr.HasValue || actorContext.ParentCtrlNbr.Value <= 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Parent context is required."));

        var selectedRailroadCtrlNbr = await GetSelectedRailroadCtrlNbrAsync(ct);
        var policy = absenceCodeCtrlNbr > 0
            ? await svc.ResolveApprovalPolicyAsync(ControlNumber.Create(absenceCodeCtrlNbr), ct)
            : AbsenceApprovalPolicy.ForLevel(AbsenceApprovalLevel.CallerManager);

        var approvers = await svc.GetApprovalOfficersAsync(
            ControlNumber.Create(actorContext.ParentCtrlNbr.Value),
            ControlNumber.Create(selectedRailroadCtrlNbr),
            policy.Level,
            ct);

        var response = new GetAbsenceApprovalContextResponse
        {
            ApprovalLevel = policy.Level.ToString(),
            ApprovalLevelDescription = policy.Description,
            DefaultOfficerCtrlNbr = approvers.FirstOrDefault()?.OfficerCtrlNbr ?? 0,
            CanSelectApprovedBy = true
        };

        response.Approvers.AddRange(approvers.Select(a => new AbsenceApprovalOfficerItem
        {
            OfficerCtrlNbr = a.OfficerCtrlNbr,
            DisplayName = a.FullName,
            EmployeeNumber = a.EmployeeNumber,
            Role = a.Role ?? string.Empty
        }));

        return response;
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
            return new AbsenceApprovalResponse { Status = absence.DerivedStatus };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<AbsenceApprovalResponse> EndAbsence(
        EndAbsenceMsg request, ServerCallContext context)
    {
        if (request.AbsenceRequestCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Absence request control number is required."));

        if (string.IsNullOrWhiteSpace(request.EndLocal)
            || !DateTime.TryParse(request.EndLocal, out var parsedLocal))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid end_local is required. Use yyyy-MM-ddTHH:mm."));
        }

        var timeZone = await ResolveDisplayTimeZoneAsync(
            request.HasWorkAreaGroupCtrlNbr ? request.WorkAreaGroupCtrlNbr : null,
            context.CancellationToken);

        if (timeZone is null)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Work area time zone context is required."));

        var localUnspecified = DateTime.SpecifyKind(parsedLocal, DateTimeKind.Unspecified);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localUnspecified, timeZone);

        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        try
        {
            var absence = await svc.EndAbsenceAsync(
                ControlNumber.Create(request.AbsenceRequestCtrlNbr),
                endUtc);
            return new AbsenceApprovalResponse { Status = absence.DerivedStatus };
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

    public override async Task<AbsenceApprovalResponse> SetAbsenceAutoProcess(
        SetAbsenceAutoProcessMsg request, ServerCallContext context)
    {
        if (request.AbsenceRequestCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Absence request control number is required."));

        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        try
        {
            var absence = await svc.SetAutoMarkOffOnApprovalAsync(
                ControlNumber.Create(request.AbsenceRequestCtrlNbr),
                request.AutoMarkOffOnApproval);
            return new AbsenceApprovalResponse { Status = absence.DerivedStatus };
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

    public override async Task<AbsenceApprovalResponse> MarkOffAbsence(
        MarkOffAbsenceMsg request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<AbsenceRequestService>();
        try
        {
            var absence = await svc.MarkOffAsync(
                ControlNumber.Create(request.AbsenceRequestCtrlNbr),
                _workAreaClock.UtcNow.UtcDateTime);
            return new AbsenceApprovalResponse { Status = absence.DerivedStatus };
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

        if (request.HasEmployeeCtrlNbr)
        {
            var employeeCtrlNbr = ControlNumber.Create(request.EmployeeCtrlNbr);
            requests = requests
                .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr)
                .ToList();
        }

        var displayTimeZone = await ResolveDisplayTimeZoneAsync(
            request.HasWorkAreaGroupCtrlNbr ? request.WorkAreaGroupCtrlNbr : null,
            context.CancellationToken);

        var standardItems = requests
            .Select(item => MapAbsenceRequest(item, displayTimeZone))
            .OrderBy(r => r.StartUtc?.ToDateTime() ?? DateTime.MaxValue)
            .ThenBy(r => r.CtrlNbr)
            .ToList();

        var waitListItems = await GetWaitListRequestItemsAsync(
            dayRange.StartUtc,
            dayRange.EndUtc,
            ControlNumber.Create(selectedRailroadCtrlNbr),
            request.HasCraftCtrlNbr ? ControlNumber.Create(request.CraftCtrlNbr) : null,
            request.HasDepartmentCtrlNbr ? ControlNumber.Create(request.DepartmentCtrlNbr) : null,
            request.HasEmployeeCtrlNbr ? ControlNumber.Create(request.EmployeeCtrlNbr) : null,
            displayTimeZone,
            context.CancellationToken);

        var responseItems = new List<MarkOffAbsenceRequestListItem>(standardItems.Count + waitListItems.Count);
        responseItems.AddRange(standardItems);
        responseItems.AddRange(waitListItems);

        var response = new GetMarkOffAbsenceRequestsResponse { TotalCount = responseItems.Count };
        response.Requests.AddRange(responseItems);

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
        var railroadCtrlNbr = ControlNumber.Create(selectedRailroadCtrlNbr);
        var craftCtrlNbr = request.HasCraftCtrlNbr ? ControlNumber.Create(request.CraftCtrlNbr) : null;
        var departmentCtrlNbr = request.HasDepartmentCtrlNbr ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        var employeeCtrlNbr = request.HasEmployeeCtrlNbr ? ControlNumber.Create(request.EmployeeCtrlNbr) : null;

        List<AbsenceRequest> scheduled;
        if (request.CurrentMonthOnly)
        {
            DateTime monthStartUtc;
            DateTime monthEndUtc;

            if (displayTimeZone is null)
            {
                var nowUtcMonth = _workAreaClock.UtcNow.UtcDateTime;
                monthStartUtc = new DateTime(nowUtcMonth.Year, nowUtcMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                monthEndUtc = monthStartUtc.AddMonths(1);
            }
            else
            {
                var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(_workAreaClock.UtcNow.UtcDateTime, displayTimeZone);
                var monthStartLocal = new DateTime(nowLocal.Year, nowLocal.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
                var monthEndLocal = monthStartLocal.AddMonths(1);
                monthStartUtc = TimeZoneInfo.ConvertTimeToUtc(monthStartLocal, displayTimeZone);
                monthEndUtc = TimeZoneInfo.ConvertTimeToUtc(monthEndLocal, displayTimeZone);
            }

            scheduled = await svc.GetByDateRangeAsync(
                railroadCtrlNbr,
                monthStartUtc,
                monthEndUtc,
                includeAllStatuses: true,
                craftCtrlNbr,
                departmentCtrlNbr,
                context.CancellationToken);
        }
        else
        {
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
                railroadCtrlNbr,
                todayStartUtc,
                todayEndUtc,
                includeAllStatuses: true,
                craftCtrlNbr,
                departmentCtrlNbr,
                context.CancellationToken);

            var next24Approved = await svc.GetByDateRangeAsync(
                railroadCtrlNbr,
                nowUtc,
                next24Utc,
                includeAllStatuses: true,
                craftCtrlNbr,
                departmentCtrlNbr,
                context.CancellationToken);

            scheduled = todayApproved
                .Concat(next24Approved)
                .GroupBy(r => r.CtrlNbr)
                .Select(g => g.First())
                .ToList();
        }

        scheduled = scheduled
            .Where(r => AbsenceStatusHelper.IsPending(r) || AbsenceStatusHelper.IsApproved(r))
            .ToList();

        if (!request.HasApprovedOnly || request.ApprovedOnly)
            scheduled = scheduled.Where(AbsenceStatusHelper.IsApproved).ToList();

        scheduled = scheduled
            .Where(r => employeeCtrlNbr is null || r.EmployeeCtrlNbr == employeeCtrlNbr)
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

        if (request.HasEmployeeCtrlNbr)
        {
            var employeeCtrlNbr = ControlNumber.Create(request.EmployeeCtrlNbr);
            requests = requests
                .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr)
                .ToList();
        }

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
            return new AbsenceApprovalResponse { Status = absence.DerivedStatus };
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
        var derivedStatus = AbsenceStatusHelper.Derive(request);
        var displayStartUtc = request.StartRecords.Count > 0
            ? DateTime.SpecifyKind(request.StartRecords[0].ActualStartUtc, DateTimeKind.Utc)
            : DateTime.SpecifyKind(request.ScheduledStartUtc, DateTimeKind.Utc);
        var approvalMetadata = ResolveApprovalMetadata(request);
        var item = new MarkOffAbsenceRequestListItem
        {
            CtrlNbr = request.CtrlNbr.Value,
            EmployeeCtrlNbr = request.EmployeeCtrlNbr.Value,
            AbsenceCodeCtrlNbr = request.AbsenceCodeCtrlNbr?.Value ?? 0,
            ReasonCode = request.ReasonCode,
            Status = derivedStatus,
            StartUtc = Timestamp.FromDateTime(displayStartUtc),
            StartLocal = _workAreaClock.FormatLocalIso(displayStartUtc, displayTimeZone),
            IsSystemGenerated = request.IsSystemGenerated,
            IsWaitlisted = false,
            AutoMarkOffOnApproval = request.AutoMarkOffOnApproval,
            CanStartAbsence = AbsenceStatusHelper.IsApproved(derivedStatus),
            CanEndAbsence = false,
            ApprovalLevel = approvalMetadata.Level,
            ApprovalLevelDescription = approvalMetadata.Description
        };

        DateTime? displayEndUtc = null;
        if (request.EndRecords.Count > 0)
        {
            displayEndUtc = DateTime.SpecifyKind(request.EndRecords.Max(r => r.ActualEndUtc), DateTimeKind.Utc);
        }
        else if (!AbsenceStatusHelper.IsComplete(derivedStatus)
            && request.ScheduledEndUtc.HasValue)
        {
            displayEndUtc = DateTime.SpecifyKind(request.ScheduledEndUtc.Value, DateTimeKind.Utc);
        }

        if (displayEndUtc.HasValue)
        {
            item.EndUtc = Timestamp.FromDateTime(displayEndUtc.Value);
            item.EndLocal = _workAreaClock.FormatLocalIso(displayEndUtc.Value, displayTimeZone);
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
            item.Notes = request.Notes;

        return item;
    }

    private async Task<List<MarkOffAbsenceRequestListItem>> GetWaitListRequestItemsAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        ControlNumber railroadCtrlNbr,
        ControlNumber? craftCtrlNbr,
        ControlNumber? departmentCtrlNbr,
        ControlNumber? employeeCtrlNbr,
        TimeZoneInfo? displayTimeZone,
        CancellationToken ct)
    {
        await using var uow = await _uowFactory.CreateAsync(cancellationToken: ct);
        var waitListRecordRepo = serviceProvider.GetRequiredService<IAbsenceRequestWaitListRecordRepository>();

        var compDay = await waitListRecordRepo.GetPendingByDateRangeAsync(
            rangeStartUtc,
            rangeEndUtc,
            AbsenceRequestWaitListType.CompensableDay,
            ct);

        var vacationWeek = await waitListRecordRepo.GetPendingByDateRangeAsync(
            rangeStartUtc,
            rangeEndUtc,
            AbsenceRequestWaitListType.VacationWeek,
            ct);

        var records = compDay
            .Concat(vacationWeek)
            .OrderBy(r => r.EntryUtc)
            .ThenBy(r => r.CtrlNbr)
            .ToList();

        var absenceCodeRepo = serviceProvider.GetRequiredService<IAbsenceCodeRepository>();
        var codeCache = new Dictionary<ControlNumber, AbsenceCode?>();
        var craftCache = new Dictionary<ControlNumber, Craft?>();
        var items = new List<MarkOffAbsenceRequestListItem>();

        foreach (var record in records)
        {
            if (employeeCtrlNbr is not null && record.EmployeeCtrlNbr != employeeCtrlNbr)
                continue;

            if (craftCtrlNbr is not null)
            {
                if (record.CraftCtrlNbr is null || record.CraftCtrlNbr != craftCtrlNbr)
                    continue;
            }

            if (departmentCtrlNbr is not null)
            {
                if (record.DepartmentCtrlNbr is null || record.DepartmentCtrlNbr != departmentCtrlNbr)
                    continue;
            }

            if (record.CraftCtrlNbr is null)
                continue;

            if (!craftCache.TryGetValue(record.CraftCtrlNbr, out var craft))
            {
                craft = await uow.Crafts.GetByCtrlNbrAsync(record.CraftCtrlNbr, ct);
                craftCache[record.CraftCtrlNbr] = craft;
            }

            if (craft?.DynamicGroupCtrlNbr != railroadCtrlNbr)
                continue;

            if (!codeCache.TryGetValue(record.AbsenceCodeCtrlNbr, out var code))
            {
                code = await absenceCodeRepo.GetByCtrlNbrAsync(record.AbsenceCodeCtrlNbr, ct);
                codeCache[record.AbsenceCodeCtrlNbr] = code;
            }

            var startUtc = DateTime.SpecifyKind(record.EntryUtc, DateTimeKind.Utc);
            items.Add(new MarkOffAbsenceRequestListItem
            {
                CtrlNbr = record.CtrlNbr.Value,
                EmployeeCtrlNbr = record.EmployeeCtrlNbr.Value,
                AbsenceCodeCtrlNbr = record.AbsenceCodeCtrlNbr.Value,
                ReasonCode = code?.Code ?? "WAITLISTED",
                Status = "WAITLISTED",
                StartUtc = Timestamp.FromDateTime(startUtc),
                StartLocal = _workAreaClock.FormatLocalIso(startUtc, displayTimeZone),
                IsSystemGenerated = false,
                IsWaitlisted = true,
                AutoMarkOffOnApproval = false,
                CanStartAbsence = false,
                CanEndAbsence = false,
                ApprovalLevel = AbsenceApprovalLevel.CallerManager.ToString(),
                ApprovalLevelDescription = "Waitlisted"
            });
        }

        return items;
    }

    private MarkOffAbsenceRequestListItem MapOpenAbsence(AbsenceRequest request, TimeZoneInfo? displayTimeZone)
    {
        var item = MapAbsenceRequest(request, displayTimeZone);
        var openStartUtc = request.StartRecords.Count > 0
            ? DateTime.SpecifyKind(request.StartRecords[0].ActualStartUtc, DateTimeKind.Utc)
            : DateTime.SpecifyKind(request.ScheduledStartUtc, DateTimeKind.Utc);
        item.StartUtc = Timestamp.FromDateTime(openStartUtc);
        item.StartLocal = _workAreaClock.FormatLocalIso(openStartUtc, displayTimeZone);
        item.Status = AbsenceStatusValues.Open;
        item.IsWaitlisted = false;
        item.CanStartAbsence = false;
        item.CanEndAbsence = request.ScheduledEndUtc is null;
        return item;
    }

    private static (string Level, string Description) ResolveApprovalMetadata(AbsenceRequest request)
    {
        if (request.IsSystemGenerated || request.AbsenceCodeCtrlNbr is null)
            return (AbsenceApprovalLevel.Automatic.ToString(), "Automatic approval (System)");

        var hasSystemApproval = request.ApprovedByCtrlNbr?.Value == AbsenceRequestService.SystemApprovalOfficerCtrlNbr;
        if (hasSystemApproval)
            return (AbsenceApprovalLevel.Automatic.ToString(), "Automatic approval (System)");

        return (AbsenceApprovalLevel.CallerManager.ToString(), "Caller or Manager approval required");
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
