using CrewService.Application.DailyOperations;
using CrewService.Application.Time;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Presentation.Formatting;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class DailyOperationsService(IServiceProvider serviceProvider) : DailyOperationsSrvc.DailyOperationsSrvcBase
{
    public override async Task<GetNextCallSheetEventResponse> GetNextCallSheetEvent(
        GetNextCallSheetEventRequest request, ServerCallContext context)
    {
        var scheduler = serviceProvider.GetRequiredService<IDailyCallSheetSchedulerService>();
        var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();

        var workAreaCtrlNbr = ControlNumber.Create(request.WorkAreaGroupCtrlNbr);
        var nextCandidate = await scheduler.GetNextCallSheetEventCandidateAsync(workAreaCtrlNbr, context.CancellationToken);
        if (nextCandidate is null)
            return new GetNextCallSheetEventResponse { NextEventLocal = string.Empty };

        var tz = await clock.GetWorkAreaTimeZoneAsync(workAreaCtrlNbr, context.CancellationToken);
        return new GetNextCallSheetEventResponse
        {
            NextEventLocal = clock.FormatLocalIso(DateTime.SpecifyKind(nextCandidate.EventUtc, DateTimeKind.Utc), tz),
            ShiftCode = nextCandidate.ShiftCode,
            ShiftDisplayName = nextCandidate.ShiftDisplayName,
            TargetDate = nextCandidate.Item.TargetDate.ToString("yyyy-MM-dd"),
            DepartmentName = nextCandidate.DepartmentName ?? string.Empty
        };
    }

    public override async Task<GetCallSheetResponse> GetCallSheet(
        GetCallSheetRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();

        if (!DateOnly.TryParse(request.TargetDate, out var targetDate))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid target_date format. Use yyyy-MM-dd."));

        var shifts = await svc.GetCallSheetAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), targetDate, context.CancellationToken);

        var response = new GetCallSheetResponse();
        foreach (var shift in shifts)
        {
            if (!request.IncludeClosed && shift.IsComplete)
                continue;
            response.Shifts.Add(await MapShiftToResponseAsync(shift, employeeNameSvc));
        }
        return response;
    }

    public override async Task<GenerateCallSheetResponse> GenerateCallSheet(
        GenerateCallSheetRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<CallSheetGenerationService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();

        var workAreaGroupCtrlNbr = ControlNumber.Create(request.WorkAreaGroupCtrlNbr);
        var shiftDefinitionCtrlNbr = ControlNumber.Create(request.ShiftDefinitionCtrlNbr);
        var departmentCtrlNbr = request.HasDepartmentCtrlNbr ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;

        if (!DateOnly.TryParse(request.TargetDate, out var targetDate))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid target_date format. Use yyyy-MM-dd."));

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
            context.CancellationToken);

        return new OffDutyRecordResponse
        {
            CtrlNbr = record.CtrlNbr.Value,
            EmployeeCtrlNbr = record.EmployeeCtrlNbr.Value,
            OffDutyTime = Timestamp.FromDateTime(DateTime.SpecifyKind(record.OffDutyTimeUtc, DateTimeKind.Utc)),
            TotalTimeOnDutyMinutes = record.TotalTimeOnDutyMinutes,
            ReleaseReason = record.ReleaseReason,
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

    private static async Task<DailyShiftInstanceResponse> MapShiftToResponseAsync(
        ShiftInstance shift, EmployeeNameService employeeNameSvc)
    {
        var employeeCtrlNbrs = shift.PositionSlots
            .Where(s => s.IncumbentEmployeeCtrlNbr is not null)
            .Select(s => s.IncumbentEmployeeCtrlNbr!)
            .Distinct()
            .ToList();
        var employeeInfoMap = await employeeNameSvc.GetEmployeeInfoBatchAsync(employeeCtrlNbrs);

        var shiftResp = new DailyShiftInstanceResponse
        {
            CtrlNbr = shift.CtrlNbr.Value,
            ShiftCode = shift.ShiftCode,
            ShiftDisplayName = shift.ShiftDisplayName,
            Status = shift.Status,
            DepartmentName = shift.DepartmentName ?? string.Empty,
        };

        if (shift.DepartmentCtrlNbr is not null)
            shiftResp.DepartmentCtrlNbr = shift.DepartmentCtrlNbr.Value;

        foreach (var slot in shift.PositionSlots)
        {
            var slotResp = new DailyPositionSlotResponse
            {
                CtrlNbr = slot.CtrlNbr.Value,
                CrewPositionCtrlNbr = slot.CrewPositionCtrlNbr?.Value ?? 0,
                Status = MapSlotStatus(slot.Status),
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
            };
            slotResp.CrewName = slot.CrewName;
            slotResp.CrewType = slot.CrewType;
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
}
