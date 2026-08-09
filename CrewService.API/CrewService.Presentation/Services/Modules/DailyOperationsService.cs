using CrewService.Application.DailyOperations;
using CrewService.Application.TenantConfig;
using CrewService.Application.Time;
using CrewService.Application.BackgroundWorkers;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Presentation.Formatting;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace CrewService.Presentation.Services.Modules;

public class DailyOperationsService(IServiceProvider serviceProvider) : DailyOperationsSrvc.DailyOperationsSrvcBase
{
    public override async Task<GetNextCallSheetEventResponse> GetNextCallSheetEvent(
        GetNextCallSheetEventRequest request, ServerCallContext context)
    {
        var nextRunResolver = serviceProvider.GetRequiredService<IBackgroundJobNextRunResolver>();
        var railroadResolver = serviceProvider.GetRequiredService<IRailroadResolver>();
        var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();
        var workAreaRepo = serviceProvider.GetRequiredService<Domain.Modules.TenantConfig.IDynamicGroupRepository>();

        var workAreaCtrlNbr = ControlNumber.Create(request.WorkAreaGroupCtrlNbr);
        var workArea = await workAreaRepo.GetByCtrlNbrAsync(workAreaCtrlNbr, context.CancellationToken);
        if (workArea is null)
            return new GetNextCallSheetEventResponse { NextEventLocal = string.Empty };

        var railroadCtrlNbr = railroadResolver.ResolveFromGroup(workArea);
        if (railroadCtrlNbr is null)
            return new GetNextCallSheetEventResponse { NextEventLocal = string.Empty };

        var nextRun = await nextRunResolver.ResolveAsync(
            "CallSheet",
            workAreaCtrlNbr,
            railroadCtrlNbr,
            context.CancellationToken);

        if (nextRun is null)
            return new GetNextCallSheetEventResponse { NextEventLocal = string.Empty };

        var tz = await clock.GetWorkAreaTimeZoneAsync(workAreaCtrlNbr, context.CancellationToken);
        return new GetNextCallSheetEventResponse
        {
            NextEventLocal = clock.FormatLocalIso(DateTime.SpecifyKind(nextRun.NextUtc, DateTimeKind.Utc), tz),
            ShiftCode = nextRun.ShiftCode ?? string.Empty,
            ShiftDisplayName = nextRun.ShiftDisplayName ?? string.Empty,
            TargetDate = nextRun.TargetDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            DepartmentName = nextRun.DepartmentName ?? string.Empty
        };
    }

    public override async Task<GetCallSheetResponse> GetCallSheet(
        GetCallSheetRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();

        if (!DateOnly.TryParse(request.TargetDate, out var targetDate))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid target_date format. Use yyyy-MM-dd."));

        var workAreaCtrlNbr = ControlNumber.Create(request.WorkAreaGroupCtrlNbr);
        var tz = await clock.GetWorkAreaTimeZoneAsync(workAreaCtrlNbr, context.CancellationToken);
        var nowLocal = tz is null
            ? clock.UtcNow.UtcDateTime
            : TimeZoneInfo.ConvertTime(clock.UtcNow, tz).DateTime;

        var shifts = await svc.GetCallSheetAsync(
            workAreaCtrlNbr, targetDate, context.CancellationToken);

        var response = new GetCallSheetResponse();
        foreach (var shift in shifts)
        {
            if (!request.IncludeClosed && shift.IsComplete)
                continue;
            response.Shifts.Add(await MapShiftToResponseAsync(shift, employeeNameSvc, targetDate, nowLocal));
        }
        return response;
    }

    public override async Task<GetVacancyFillCandidatesResponse> GetVacancyFillCandidates(
        GetVacancyFillCandidatesRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<VacancyResolutionOrchestrationService>();

        var candidates = await svc.GetFillCandidatesAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
            ControlNumber.Create(request.ShiftInstanceCtrlNbr),
            ControlNumber.Create(request.PositionSlotCtrlNbr),
            request.HasCraftCtrlNbr ? ControlNumber.Create(request.CraftCtrlNbr) : null,
            context.CancellationToken);

        var response = new GetVacancyFillCandidatesResponse();
        foreach (var candidate in candidates)
        {
            var row = new VacancyFillCandidateResponse
            {
                EmployeeCtrlNbr = candidate.EmployeeCtrlNbr.Value,
                EmployeeNumber = candidate.EmployeeNumber,
                EmployeeName = candidate.EmployeeName,
                BoardType = candidate.BoardType,
                BoardOrder = candidate.BoardOrder,
                CallSequence = candidate.CallSequence,
                QualificationStatus = candidate.QualificationStatus,
                StatusDisplay = candidate.StatusDisplay,
                ProjectedVacancyDisplay = candidate.ProjectedVacancyDisplay,
                OnDutyDisplay = candidate.OnDutyDisplay
            };

            row.Contacts.AddRange(candidate.Contacts.Select(c => new VacancyCandidateContactResponse
            {
                ContactType = c.ContactType,
                ContactValue = c.ContactValue,
                CallingOrder = c.CallingOrder
            }));

            response.Candidates.Add(row);
        }

        return response;
    }

    public override async Task<FillVacancyPositionResponse> FillVacancyPosition(
        FillVacancyPositionRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<VacancyResolutionOrchestrationService>();

        var result = await svc.FillVacancyAsync(
            new VacancyFillRequest(
                WorkAreaGroupCtrlNbr: ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
                ShiftInstanceCtrlNbr: ControlNumber.Create(request.ShiftInstanceCtrlNbr),
                PositionSlotCtrlNbr: ControlNumber.Create(request.PositionSlotCtrlNbr),
                EmployeeCtrlNbr: ControlNumber.Create(request.EmployeeCtrlNbr),
                ForceOverride: request.ForceOverride,
                ForceReason: request.ForceReason,
                DispatcherNote: request.DispatcherNote,
                Accepted: request.Accepted,
                IsLateCall: request.IsLateCall,
                LateCallNote: request.LateCallNote,
                ArrivalFollowUpNote: request.ArrivalFollowUpNote,
                AcceptedAtUtc: request.AcceptedAtUtc?.ToDateTime(),
                ExpectedArrivalAtUtc: request.ExpectedArrivalAtUtc?.ToDateTime(),
                CraftCtrlNbr: request.HasCraftCtrlNbr ? ControlNumber.Create(request.CraftCtrlNbr) : null),
            context.CancellationToken);

        return new FillVacancyPositionResponse
        {
            Success = result.Success,
            Status = result.Status,
            ShiftInstanceCtrlNbr = result.ShiftInstanceCtrlNbr.Value,
            PositionSlotCtrlNbr = result.PositionSlotCtrlNbr.Value,
            EmployeeCtrlNbr = result.EmployeeCtrlNbr.Value,
            OnDutyRecordCtrlNbr = result.OnDutyRecordCtrlNbr.Value,
            VacancyFillLogCtrlNbr = result.VacancyFillLogCtrlNbr.Value
        };
    }

    public override async Task<GetVacancyFillAuditReportResponse> GetVacancyFillAuditReport(
        GetVacancyFillAuditReportRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<VacancyResolutionOrchestrationService>();
        var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();

        if (!DateOnly.TryParse(request.TargetDate, out var targetDate))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid target_date format. Use yyyy-MM-dd."));

        var records = await svc.GetAuditReportAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
            targetDate,
            request.HasDepartmentCtrlNbr ? ControlNumber.Create(request.DepartmentCtrlNbr) : null,
            context.CancellationToken);

        var response = new GetVacancyFillAuditReportResponse();
        response.Records.AddRange(records.Select(r => new VacancyFillAuditRecordResponse
        {
            VacancyFillLogCtrlNbr = r.VacancyFillLogCtrlNbr.Value,
            ShiftInstanceCtrlNbr = r.ShiftInstanceCtrlNbr.Value,
            PositionSlotCtrlNbr = r.PositionSlotCtrlNbr.Value,
            AssignmentCode = r.AssignmentCode,
            CraftRoleName = r.CraftRoleName,
            EmployeeCtrlNbr = r.EmployeeCtrlNbr.Value,
            EmployeeName = r.EmployeeName,
            Status = r.Status,
            ForceOverride = r.ForceOverride,
            ForceReason = r.ForceReason ?? string.Empty,
            IsLateCall = r.IsLateCall,
            LateCallNote = r.LateCallNote ?? string.Empty,
            ArrivalFollowUpNote = r.ArrivalFollowUpNote ?? string.Empty,
            DispatcherNote = r.DispatcherNote ?? string.Empty,
            CreatedAtUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(r.CreatedAtUtc, DateTimeKind.Utc)),
            CreatedAtLocal = string.Empty
        }));

        foreach (var row in response.Records)
        {
            var source = records.FirstOrDefault(x => x.VacancyFillLogCtrlNbr.Value == row.VacancyFillLogCtrlNbr);
            if (source is null)
                continue;

            var tz = await clock.GetWorkAreaTimeZoneAsync(source.WorkAreaGroupCtrlNbr, context.CancellationToken);
            var local = tz is null
                ? DateTime.SpecifyKind(source.CreatedAtUtc, DateTimeKind.Utc)
                : TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(source.CreatedAtUtc, DateTimeKind.Utc), tz);
            row.CreatedAtLocal = local.ToString("MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        return response;
    }

    public override async Task<GetVacancyResolutionResponse> GetVacancyResolution(
        GetVacancyResolutionRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();

        if (!DateOnly.TryParse(request.TargetDate, out var targetDate))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid target_date format. Use yyyy-MM-dd."));

        var workAreaCtrlNbr = ControlNumber.Create(request.WorkAreaGroupCtrlNbr);
        var tz = await clock.GetWorkAreaTimeZoneAsync(workAreaCtrlNbr, context.CancellationToken);
        var nowLocal = tz is null
            ? clock.UtcNow.UtcDateTime
            : TimeZoneInfo.ConvertTime(clock.UtcNow, tz).DateTime;

        var shifts = await svc.GetCallSheetAsync(workAreaCtrlNbr, targetDate, context.CancellationToken);
        var response = new GetVacancyResolutionResponse();

        foreach (var shift in shifts)
        {
            if (!request.IncludeClosed && shift.IsComplete)
                continue;

            if (request.HasDepartmentCtrlNbr
                && (shift.DepartmentCtrlNbr?.Value ?? 0) != request.DepartmentCtrlNbr)
            {
                continue;
            }

            var mapped = await MapShiftToResponseAsync(shift, employeeNameSvc, targetDate, nowLocal);
            var relevantSlots = mapped.PositionSlots.Where(IsVacancyResolutionSlot).ToList();
            if (relevantSlots.Count == 0)
                continue;

            var card = new VacancyResolutionShiftCard
            {
                ShiftInstanceCtrlNbr = mapped.CtrlNbr,
                ShiftCode = mapped.ShiftCode,
                ShiftDisplayName = mapped.ShiftDisplayName,
                Status = mapped.Status,
                DepartmentName = mapped.DepartmentName,
            };

            if (mapped.HasDepartmentCtrlNbr)
                card.DepartmentCtrlNbr = mapped.DepartmentCtrlNbr;

            card.PositionSlots.AddRange(relevantSlots);
            response.Shifts.Add(card);
        }

        return response;
    }

    public override async Task<GenerateCallSheetResponse> GenerateCallSheet(
        GenerateCallSheetRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CallSheetGenerationService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();
        var callSheetSignal = serviceProvider.GetRequiredService<IDailyCallSheetScheduleSignal>();
        var manualOverrideStore = serviceProvider.GetRequiredService<IDailyCallSheetManualOverrideStore>();

        var workAreaGroupCtrlNbr = ControlNumber.Create(request.WorkAreaGroupCtrlNbr);
        var shiftDefinitionCtrlNbr = ControlNumber.Create(request.ShiftDefinitionCtrlNbr);
        if (!request.HasDepartmentCtrlNbr || request.DepartmentCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Department is required."));

        var departmentCtrlNbr = ControlNumber.Create(request.DepartmentCtrlNbr);

        if (!DateOnly.TryParse(request.TargetDate, out var targetDate))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid target_date format. Use yyyy-MM-dd."));

        if (!string.IsNullOrWhiteSpace(request.ScheduledCreateLocal))
        {
            if (!DateTime.TryParse(request.ScheduledCreateLocal, out var scheduledLocal))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid scheduled_create_local format. Use yyyy-MM-ddTHH:mm."));

            var tz = await clock.GetWorkAreaTimeZoneAsync(workAreaGroupCtrlNbr, context.CancellationToken);
            var scheduledUtc = clock.CombineLocalToUtc(
                DateOnly.FromDateTime(scheduledLocal),
                TimeOnly.FromDateTime(scheduledLocal),
                tz).UtcDateTime;

            var scheduledItem = new DailyCallSheetDueWorkItem(
                workAreaGroupCtrlNbr,
                shiftDefinitionCtrlNbr,
                targetDate,
                departmentCtrlNbr);
            manualOverrideStore.Schedule(scheduledUtc, scheduledItem);
            callSheetSignal.Notify(scheduledUtc);

            return new GenerateCallSheetResponse
            {
                Shift = new DailyShiftInstanceResponse
                {
                    CtrlNbr = 0,
                    ShiftCode = string.Empty,
                    ShiftDisplayName = "Scheduled",
                    Status = "Scheduled"
                }
            };
        }

        try
        {
            var shiftInstance = await svc.GenerateForShiftAsync(
                workAreaGroupCtrlNbr, shiftDefinitionCtrlNbr, targetDate, departmentCtrlNbr, context.CancellationToken);

            return new GenerateCallSheetResponse
            {
                Shift = await MapShiftToResponseAsync(shiftInstance, employeeNameSvc)
            };
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    public override async Task<OnDutyRecordResponse> PlaceOnDuty(
        PlaceOnDutyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<OnDutyPlacementService>();
        var record = await svc.ExecuteAsync(
            ControlNumber.Create(request.PositionSlotCtrlNbr),
            ControlNumber.Create(request.EmployeeCtrlNbr),
            request.OnDutyTime.ToDateTime(),
            request.ScheduledOnDutyTime.ToDateTime(),
            request.IsAssigned,
            ct: context.CancellationToken);

        return new OnDutyRecordResponse
        {
            CtrlNbr = record.CtrlNbr.Value,
            EmployeeCtrlNbr = record.EmployeeCtrlNbr.Value,
            OnDutyTime = Timestamp.FromDateTime(DateTime.SpecifyKind(record.OnDutyTimeUtc, DateTimeKind.Utc)),
            IsLateCall = record.IsLateCall,
            ConsecutiveDays = record.ConsecutiveDays,
            Status = record.Status.Value,
        };
    }

    public override async Task<OffDutyRecordResponse> TieUp(
        TieUpRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<TieUpService>();
        var record = await svc.ExecuteAsync(
            ControlNumber.Create(request.OnDutyRecordCtrlNbr),
            request.OffDutyTime.ToDateTime(),
            request.ReleaseReason,
            ControlNumber.Create(request.CraftCtrlNbr),
            request.OffDutyTimeConfirmed,
            context.CancellationToken);

        return new OffDutyRecordResponse
        {
            CtrlNbr = record.CtrlNbr.Value,
            EmployeeCtrlNbr = record.EmployeeCtrlNbr.Value,
            OffDutyTime = Timestamp.FromDateTime(DateTime.SpecifyKind(record.OffDutyTimeUtc, DateTimeKind.Utc)),
            TotalTimeOnDutyMinutes = record.TotalTimeOnDutyMinutes,
            ReleaseReason = record.ReleaseReason,
            OffDutyTimeConfirmed = record.OffDutyTimeConfirmed,
            OffDutyTimeConfirmedAt = record.OffDutyTimeConfirmedAtUtc.HasValue
                ? Timestamp.FromDateTime(DateTime.SpecifyKind(record.OffDutyTimeConfirmedAtUtc.Value, DateTimeKind.Utc))
                : null,
            OffDutyTimeConfirmedBy = record.OffDutyTimeConfirmedBy,
        };
    }

    public override async Task<GenerateCallSheetResponse> AnnulPosition(
        AnnulPositionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        try
        {
            var shift = await svc.AnnulPositionAsync(
                ControlNumber.Create(request.ShiftInstanceCtrlNbr),
                ControlNumber.Create(request.PositionSlotCtrlNbr),
                request.Reason,
                request.AnnulmentDateTime.ToDateTime(),
                context.CancellationToken);
            return new GenerateCallSheetResponse { Shift = await MapShiftToResponseAsync(shift, employeeNameSvc) };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GenerateCallSheetResponse> AnnulAssignment(
        AnnulAssignmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        try
        {
            var shift = await svc.AnnulAssignmentAsync(
                ControlNumber.Create(request.ShiftInstanceCtrlNbr),
                ControlNumber.Create(request.AssignmentCtrlNbr),
                request.Reason,
                request.AnnulmentDateTime.ToDateTime(),
                context.CancellationToken);
            return new GenerateCallSheetResponse { Shift = await MapShiftToResponseAsync(shift, employeeNameSvc) };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<GenerateCallSheetResponse> DoNotFillPosition(
        DoNotFillPositionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        try
        {
            var shift = await svc.DoNotFillPositionAsync(
                ControlNumber.Create(request.ShiftInstanceCtrlNbr),
                ControlNumber.Create(request.PositionSlotCtrlNbr),
                context.CancellationToken);
            return new GenerateCallSheetResponse { Shift = await MapShiftToResponseAsync(shift, employeeNameSvc) };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GenerateCallSheetResponse> RestorePositionSlot(
        RestorePositionSlotRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        try
        {
            var shift = await svc.RestorePositionSlotAsync(
                ControlNumber.Create(request.ShiftInstanceCtrlNbr),
                ControlNumber.Create(request.PositionSlotCtrlNbr),
                context.CancellationToken);
            return new GenerateCallSheetResponse { Shift = await MapShiftToResponseAsync(shift, employeeNameSvc) };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GenerateCallSheetResponse> RestoreAssignment(
        RestoreAssignmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        try
        {
            var shift = await svc.RestoreAssignmentAsync(
                ControlNumber.Create(request.ShiftInstanceCtrlNbr),
                ControlNumber.Create(request.AssignmentCtrlNbr),
                context.CancellationToken);
            return new GenerateCallSheetResponse { Shift = await MapShiftToResponseAsync(shift, employeeNameSvc) };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<GenerateCallSheetResponse> SaveAssignmentNote(
        SaveAssignmentNoteRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        try
        {
            var shift = await svc.SaveAssignmentNoteAsync(
                ControlNumber.Create(request.ShiftInstanceCtrlNbr),
                ControlNumber.Create(request.AssignmentCtrlNbr),
                request.NoteText,
                context.CancellationToken);
            return new GenerateCallSheetResponse { Shift = await MapShiftToResponseAsync(shift, employeeNameSvc) };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GenerateCallSheetResponse> ManageAssignmentPositions(
        ManageAssignmentPositionsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        try
        {
            var orders = request.PositionSlotOrders
                .Select(o => (ControlNumber.Create(o.CtrlNbr), o.DisplayOrder));
            var shift = await svc.ManageAssignmentPositionsAsync(
                ControlNumber.Create(request.ShiftInstanceCtrlNbr),
                ControlNumber.Create(request.AssignmentCtrlNbr),
                request.RemovedPositionSlotCtrlNbrs.Select(ControlNumber.Create),
                request.AddedCraftRoleNames,
                orders,
                context.CancellationToken);
            return new GenerateCallSheetResponse { Shift = await MapShiftToResponseAsync(shift, employeeNameSvc) };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<GenerateCallSheetResponse> RefreshShiftInstance(
        RefreshShiftInstanceRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CallSheetGenerationService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        try
        {
            var newShift = await svc.RegenerateShiftAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new GenerateCallSheetResponse { Shift = await MapShiftToResponseAsync(newShift, employeeNameSvc) };
        }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<DeleteResponse> CloseShiftInstance(
        CloseShiftInstanceRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        try
        {
            await svc.CloseShiftInstanceAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GenerateCallSheetResponse> ReopenShiftInstance(
        ReopenShiftInstanceRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        try
        {
            var shift = await svc.ReopenShiftInstanceAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new GenerateCallSheetResponse { Shift = await MapShiftToResponseAsync(shift, employeeNameSvc) };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GetAvailableExtraAssignmentsResponse> GetAvailableExtraAssignments(
        GetAvailableExtraAssignmentsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var assignmentQuery = serviceProvider.GetRequiredService<IAssignmentQueryService>();
        try
        {
            var (extras, existing) = await svc.GetAvailableExtraAssignmentsAsync(
                ControlNumber.Create(request.ShiftInstanceCtrlNbr), assignmentQuery, context.CancellationToken);

            var response = new GetAvailableExtraAssignmentsResponse();
            foreach (var a in extras)
            {
                if (existing.Contains(a.AssignmentCtrlNbr))
                    continue;
                response.Assignments.Add(new AvailableAssignmentResponse
                {
                    AssignmentCtrlNbr = a.AssignmentCtrlNbr.Value,
                    AssignmentCode = a.AssignmentCode,
                    AssignmentName = a.AssignmentName,
                    OnDutyTime = ScheduleTimeFormat.Format(a.OnDutyTime),
                    OffDutyTime = ScheduleTimeFormat.Format(a.OffDutyTime),
                    GroupName = a.GroupName,
                    GroupCode = a.GroupCode,
                    PositionCount = a.Positions.Count
                });
            }
            return response;
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GenerateCallSheetResponse> AddAssignmentFromTemplate(
        AddAssignmentFromTemplateRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var assignmentQuery = serviceProvider.GetRequiredService<IAssignmentQueryService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        try
        {
            var shift = await svc.AddAssignmentFromTemplateAsync(
                ControlNumber.Create(request.ShiftInstanceCtrlNbr),
                ControlNumber.Create(request.AssignmentCtrlNbr),
                assignmentQuery,
                request.OnDutyTime,
                request.OffDutyTime,
                context.CancellationToken);
            return new GenerateCallSheetResponse { Shift = await MapShiftToResponseAsync(shift, employeeNameSvc) };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<GenerateCallSheetResponse> AddAdHocAssignment(
        AddAdHocAssignmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();

        if (!TimeOnly.TryParse(request.OnDutyTime, out var onDutyTime))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid on_duty_time format."));
        if (!TimeOnly.TryParse(request.OffDutyTime, out var offDutyTime))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid off_duty_time format."));

        try
        {
            var shift = await svc.AddAdHocAssignmentAsync(
                ControlNumber.Create(request.ShiftInstanceCtrlNbr),
                request.AssignmentCode, request.AssignmentName,
                request.GroupName, request.GroupCode,
                onDutyTime, offDutyTime,
                request.CraftRoleNames.ToList(),
                context.CancellationToken);
            return new GenerateCallSheetResponse { Shift = await MapShiftToResponseAsync(shift, employeeNameSvc) };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<GenerateCallSheetResponse> RemoveAssignment(
        RemoveAssignmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        try
        {
            var shift = await svc.RemoveAssignmentAsync(
                ControlNumber.Create(request.ShiftInstanceCtrlNbr),
                ControlNumber.Create(request.AssignmentCtrlNbr),
                context.CancellationToken);
            return new GenerateCallSheetResponse { Shift = await MapShiftToResponseAsync(shift, employeeNameSvc) };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    private async Task<DailyShiftInstanceResponse> MapShiftToResponseAsync(
        ShiftInstance shift,
        EmployeeNameService employeeNameSvc,
        DateOnly? targetDate = null,
        DateTime? nowLocal = null)
    {
        var vacancyEvaluationService = serviceProvider.GetRequiredService<CallSheetSlotVacancyEvaluationService>();
        var workAreaClock = serviceProvider.GetRequiredService<IWorkAreaClock>();

        var slotCtrlNbrs = shift.PositionSlots.Select(s => s.CtrlNbr).ToList();
        IReadOnlyList<CrewService.Domain.Modules.Dispatching.OnDutyRecord> onDutyRecords;
        IReadOnlyDictionary<ControlNumber, SlotVacancyEvaluation> vacancyEvaluations = new Dictionary<ControlNumber, SlotVacancyEvaluation>();
        Dictionary<ControlNumber, DateTime?> vacancyImpactStartLocalBySlot = [];
        var projectedEmployeeBySlot = new Dictionary<ControlNumber, ControlNumber>();
        var incumbentEmployeeCtrlNbrs = shift.PositionSlots
            .Where(s => s.IncumbentEmployeeCtrlNbr is not null)
            .Select(s => s.IncumbentEmployeeCtrlNbr!)
            .Distinct()
            .ToList();
        var projectedEmployeeCtrlNbrs = new HashSet<ControlNumber>();
        Dictionary<ControlNumber, (string FullNameLnf, string EmployeeNumber)> employeeInfoMap;
        Dictionary<ControlNumber, (string FullNameLnf, string EmployeeNumber)> projectedEmployeeInfoMap;

        await using (var uow = await serviceProvider.GetRequiredService<IOrchestrationUnitOfWorkFactory>()
            .CreateAsync())
        {
            onDutyRecords = await uow.OnDutyRecords.GetByPositionSlotsAsync(slotCtrlNbrs);

            var workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr)
                ?? throw new KeyNotFoundException($"Work instance {shift.WorkInstanceCtrlNbr.Value} not found for shift {shift.CtrlNbr.Value}.");

            var evaluationDate = targetDate ?? DateOnly.FromDateTime(workInstance.StartUtc);
            vacancyEvaluations = await vacancyEvaluationService.EvaluateShiftAsync(
                uow,
                shift,
                workInstance.WorkAreaGroupCtrlNbr,
                evaluationDate);

            var workAreaTimeZone = await workAreaClock.GetWorkAreaTimeZoneAsync(uow, workInstance.WorkAreaGroupCtrlNbr);
            foreach (var slotCtrlNbr in slotCtrlNbrs)
            {
                var impacts = await uow.VacancyImpacts.GetByPositionSlotAsync(slotCtrlNbr);
                var openImpact = impacts
                    .Where(i => i.ImpactEndUtc is null)
                    .OrderByDescending(i => i.ImpactStartUtc)
                    .FirstOrDefault();

                vacancyImpactStartLocalBySlot[slotCtrlNbr] = openImpact is null
                    ? null
                    : (workAreaTimeZone is null
                        ? DateTime.SpecifyKind(openImpact.ImpactStartUtc, DateTimeKind.Utc)
                        : TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(openImpact.ImpactStartUtc, DateTimeKind.Utc), workAreaTimeZone));
            }

            foreach (var slot in shift.PositionSlots)
            {
                var projections = await uow.DispatchProjections.GetByPositionSlotAsync(slot.CtrlNbr);
                var latestProjection = projections.FirstOrDefault();
                if (latestProjection?.ProjectedEmployeeCtrlNbr is not { } projectedEmployeeCtrlNbr)
                    continue;

                projectedEmployeeBySlot[slot.CtrlNbr] = projectedEmployeeCtrlNbr;
                projectedEmployeeCtrlNbrs.Add(projectedEmployeeCtrlNbr);
            }

            employeeInfoMap = await employeeNameSvc.GetEmployeeInfoBatchAsync(uow, incumbentEmployeeCtrlNbrs);
            projectedEmployeeInfoMap = projectedEmployeeCtrlNbrs.Count == 0
                ? []
                : await employeeNameSvc.GetEmployeeInfoBatchAsync(uow, projectedEmployeeCtrlNbrs);
        }

        var slotOnDutyMap = onDutyRecords
            .GroupBy(r => r.PositionSlotCtrlNbr)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.OnDutyTimeUtc).First().CtrlNbr);

        var slotOnDutyCompletionMap = onDutyRecords
            .GroupBy(r => r.PositionSlotCtrlNbr)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.OnDutyTimeUtc).First().CompletionStatus == OnDutyCompletionStatus.Completed);

        var effectiveShiftStatus = targetDate.HasValue && nowLocal.HasValue
            ? ResolveShiftDisplayStatus(shift, targetDate.Value, nowLocal.Value)
            : shift.Status;

        var shiftResp = new DailyShiftInstanceResponse
        {
            CtrlNbr = shift.CtrlNbr.Value,
            ShiftCode = shift.ShiftCode,
            ShiftDisplayName = shift.ShiftDisplayName,
            Status = effectiveShiftStatus,
            DepartmentName = shift.DepartmentName ?? string.Empty,
        };

        if (shift.DepartmentCtrlNbr is not null)
            shiftResp.DepartmentCtrlNbr = shift.DepartmentCtrlNbr.Value;

        foreach (var slot in shift.PositionSlots)
        {
            var evaluatedStatus = vacancyEvaluations.TryGetValue(slot.CtrlNbr, out var evaluation)
                ? evaluation.EffectiveStatus
                : slot.Status;

            var effectiveSlotStatus = (targetDate.HasValue && nowLocal.HasValue)
                ? ResolveSlotDisplayStatus(slot, targetDate.Value, nowLocal.Value, evaluatedStatus)
                : evaluatedStatus;

            var slotResp = new DailyPositionSlotResponse
            {
                CtrlNbr = slot.CtrlNbr.Value,
                CrewPositionCtrlNbr = slot.CrewPositionCtrlNbr?.Value ?? 0,
                Status = MapSlotStatus(effectiveSlotStatus),
                IsAnnulled = slot.IsAnnulled,
                IsDoNotFill = slot.IsDoNotFill,
                IsSkipped = slot.IsSkipped,
                IsAdHoc = slot.IsAdHoc,
                DisplayOrder = slot.DisplayOrder,
                AssignmentCtrlNbr = slot.AssignmentCtrlNbr.Value,
                AssignmentCode = slot.AssignmentCode,
                AssignmentName = slot.AssignmentName,
                CraftRoleName = slot.CraftRoleName,
                OnDutyTime = ScheduleTimeFormat.Format(slot.OnDutyTime),
                OffDutyTime = ScheduleTimeFormat.Format(slot.OffDutyTime),
                GroupName = slot.GroupName,
                GroupCode = slot.GroupCode,
                IsIncumbent = slot.IsIncumbent,
                VacancyReason = evaluation?.Display.Reason.ToString() ?? SlotVacancyDisplayReason.None.ToString(),
                VacancyActionability = evaluation?.Display.Actionability.ToString() ?? SlotVacancyActionability.None.ToString(),
                VacancyDisplayCode = evaluation?.Display.DisplayCode ?? string.Empty,
                UseLegacyMarkedOffStyling = evaluation?.Display.UseLegacyMarkedOffStyling ?? false,
                VacancyImpactStartLocal = vacancyImpactStartLocalBySlot.TryGetValue(slot.CtrlNbr, out var localImpactStart)
                    ? localImpactStart?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty
                    : string.Empty
            };
            if (slotOnDutyMap.TryGetValue(slot.CtrlNbr, out var onDutyCtrlNbr))
                slotResp.OnDutyRecordCtrlNbr = onDutyCtrlNbr.Value;

            if (slotOnDutyCompletionMap.TryGetValue(slot.CtrlNbr, out var isOnDutyRecordCompleted))
                slotResp.IsOnDutyRecordCompleted = isOnDutyRecordCompleted;

            slotResp.CrewName = slot.CrewName;
            slotResp.CrewType = slot.CrewType;

            if (projectedEmployeeBySlot.TryGetValue(slot.CtrlNbr, out var projectedEmployeeCtrlNbr))
            {
                slotResp.ProjectedEmployeeCtrlNbr = projectedEmployeeCtrlNbr.Value;
                if (projectedEmployeeInfoMap.TryGetValue(projectedEmployeeCtrlNbr, out var projectedInfo))
                {
                    slotResp.ProjectedEmployeeNumber = projectedInfo.EmployeeNumber;
                    slotResp.ProjectedEmployeeName = projectedInfo.FullNameLnf;
                }
            }

            if (slot.IncumbentEmployeeCtrlNbr is not null)
            {
                slotResp.IncumbentEmployeeCtrlNbr = slot.IncumbentEmployeeCtrlNbr.Value;
                if (employeeInfoMap.TryGetValue(slot.IncumbentEmployeeCtrlNbr, out var info))
                {
                    slotResp.IncumbentEmployeeNumber = info.EmployeeNumber;
                    slotResp.IncumbentEmployeeName = info.FullNameLnf;
                }
            }
            shiftResp.PositionSlots.Add(slotResp);
        }

        foreach (var note in shift.AssignmentNotes)
        {
            shiftResp.AssignmentNotes.Add(new AssignmentNoteResponse
            {
                AssignmentCtrlNbr = note.AssignmentCtrlNbr.Value,
                NoteText = note.NoteText
            });
        }

        return shiftResp;
    }

    private static string ResolveShiftDisplayStatus(ShiftInstance shift, DateOnly targetDate, DateTime nowLocal)
    {
        if (shift.IsComplete || string.Equals(shift.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            return "Completed";

        if (string.Equals(shift.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return "Cancelled";

        var firstOnDuty = shift.PositionSlots
            .Select(s => (TimeOnly?)s.OnDutyTime)
            .OrderBy(t => t)
            .FirstOrDefault();

        if (!firstOnDuty.HasValue)
            return shift.Status;

        var nowDate = DateOnly.FromDateTime(nowLocal);
        if (nowDate > targetDate)
            return "Active";

        if (nowDate < targetDate)
            return "Planned";

        return nowLocal.TimeOfDay >= firstOnDuty.Value.ToTimeSpan()
            ? "Active"
            : "Planned";
    }

    private static PositionSlotStatus ResolveSlotDisplayStatus(
        PositionSlotInstance slot,
        DateOnly targetDate,
        DateTime nowLocal,
        PositionSlotStatus? statusOverride = null)
    {
        var status = statusOverride ?? slot.Status;

        if (slot.IncumbentEmployeeCtrlNbr is null)
            return status;

        if (status is not (PositionSlotStatus.Filled or PositionSlotStatus.OnDuty or PositionSlotStatus.OnDutyOvertime))
            return status;

        var onDutyLocal = targetDate.ToDateTime(slot.OnDutyTime);
        var offDutyDate = slot.OffDutyTime <= slot.OnDutyTime ? targetDate.AddDays(1) : targetDate;
        var offDutyLocal = offDutyDate.ToDateTime(slot.OffDutyTime);

        if (nowLocal < onDutyLocal)
            return PositionSlotStatus.Filled;

        if (nowLocal >= offDutyLocal)
            return PositionSlotStatus.OnDutyOvertime;

        return PositionSlotStatus.OnDuty;
    }

    private static PositionSlotStatusEnum MapSlotStatus(PositionSlotStatus status) => status switch
    {
        PositionSlotStatus.Open => PositionSlotStatusEnum.PositionSlotStatusOpen,
        PositionSlotStatus.Filled => PositionSlotStatusEnum.PositionSlotStatusFilled,
        PositionSlotStatus.OnDuty => PositionSlotStatusEnum.PositionSlotStatusOnDuty,
        PositionSlotStatus.OnDutyOvertime => PositionSlotStatusEnum.PositionSlotStatusOnDutyOvertime,
        PositionSlotStatus.TiedUp => PositionSlotStatusEnum.PositionSlotStatusTiedUp,
        PositionSlotStatus.MarkedOff => PositionSlotStatusEnum.PositionSlotStatusMarkedOff,
        PositionSlotStatus.Unavailable => PositionSlotStatusEnum.PositionSlotStatusUnavailable,
        PositionSlotStatus.Annulled => PositionSlotStatusEnum.PositionSlotStatusAnnulled,
        PositionSlotStatus.DoNotFill => PositionSlotStatusEnum.PositionSlotStatusDoNotFill,
        PositionSlotStatus.ExtraBoard => PositionSlotStatusEnum.PositionSlotStatusExtraBoard,
        PositionSlotStatus.NoBids => PositionSlotStatusEnum.PositionSlotStatusNoBids,
        PositionSlotStatus.Skipped => PositionSlotStatusEnum.PositionSlotStatusSkipped,
        _ => PositionSlotStatusEnum.PositionSlotStatusOpen
    };

    private static bool IsVacancyResolutionSlot(DailyPositionSlotResponse slot)
    {
        return !string.Equals(slot.VacancyReason, SlotVacancyDisplayReason.None.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
