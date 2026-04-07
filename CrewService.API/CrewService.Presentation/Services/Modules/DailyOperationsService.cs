using CrewService.Application.DailyOperations;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class DailyOperationsService(
    IShiftInstanceRepository shiftInstanceRepo,
    IWorkInstanceRepository workInstanceRepo,
    CallSheetGenerationService callSheetGeneration,
    OnDutyPlacementService onDutyPlacement,
    TieUpService tieUpService,
    IAssignmentQueryService assignmentQuery)
    : DailyOperationsSrvc.DailyOperationsSrvcBase
{
    public override async Task<GetCallSheetResponse> GetCallSheet(
        GetCallSheetRequest request, ServerCallContext context)
    {
        var workAreaGroupCtrlNbr = ControlNumber.Create(request.WorkAreaGroupCtrlNbr);

        if (!DateOnly.TryParse(request.TargetDate, out var targetDate))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid target_date format. Use yyyy-MM-dd."));

        var dayStartUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEndUtc = targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var workInstances = await workInstanceRepo.GetByWorkAreaAndDateRangeAsync(
            workAreaGroupCtrlNbr, dayStartUtc, dayEndUtc);

        var response = new GetCallSheetResponse();

        if (workInstances.Count == 0)
            return response;

        var shifts = await shiftInstanceRepo.GetByWorkInstanceAsync(
            workInstances[0].CtrlNbr, context.CancellationToken);

        foreach (var shift in shifts)
        {
            if (!request.IncludeClosed && shift.IsComplete)
                continue;

            response.Shifts.Add(MapShiftToResponse(shift));
        }
        return response;
    }

    public override async Task<GenerateCallSheetResponse> GenerateCallSheet(
        GenerateCallSheetRequest request, ServerCallContext context)
    {
        var workAreaGroupCtrlNbr = ControlNumber.Create(request.WorkAreaGroupCtrlNbr);
        var shiftDefinitionCtrlNbr = ControlNumber.Create(request.ShiftDefinitionCtrlNbr);
        var departmentCtrlNbr = request.HasDepartmentCtrlNbr ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;

        if (!DateOnly.TryParse(request.TargetDate, out var targetDate))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid target_date format. Use yyyy-MM-dd."));

        try
        {
            var shiftInstance = await callSheetGeneration.GenerateForShiftAsync(
                workAreaGroupCtrlNbr, shiftDefinitionCtrlNbr, targetDate, departmentCtrlNbr, context.CancellationToken);

            return new GenerateCallSheetResponse
            {
                Shift = MapShiftToResponse(shiftInstance)
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
        var record = await onDutyPlacement.ExecuteAsync(
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
            Status = record.Status,
        };
    }

    public override async Task<OffDutyRecordResponse> TieUp(
        TieUpRequest request, ServerCallContext context)
    {
        var record = await tieUpService.ExecuteAsync(
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
        var shiftCtrlNbr = ControlNumber.Create(request.ShiftInstanceCtrlNbr);
        var slotCtrlNbr = ControlNumber.Create(request.PositionSlotCtrlNbr);

        var shift = await shiftInstanceRepo.GetByCtrlNbrAsync(shiftCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shift instance {request.ShiftInstanceCtrlNbr} not found."));

        var slot = shift.PositionSlots.SingleOrDefault(s => s.CtrlNbr == slotCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Position slot {request.PositionSlotCtrlNbr} not found on shift."));

        slot.Annul(request.Reason, request.AnnulmentDateTime.ToDateTime());
        await shiftInstanceRepo.UpdateAsync(shift, context.CancellationToken);

        return new GenerateCallSheetResponse { Shift = MapShiftToResponse(shift) };
    }

    public override async Task<GenerateCallSheetResponse> AnnulAssignment(
        AnnulAssignmentRequest request, ServerCallContext context)
    {
        var shiftCtrlNbr = ControlNumber.Create(request.ShiftInstanceCtrlNbr);
        var assignmentCtrlNbr = ControlNumber.Create(request.AssignmentCtrlNbr);

        var shift = await shiftInstanceRepo.GetByCtrlNbrAsync(shiftCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shift instance {request.ShiftInstanceCtrlNbr} not found."));

        var slots = shift.PositionSlots
            .Where(s => s.AssignmentCtrlNbr == assignmentCtrlNbr
                && s.Status != PositionSlotStatus.Annulled)
            .ToList();

        if (slots.Count == 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "No annullable positions found for this assignment."));

        var annulmentDateTime = request.AnnulmentDateTime.ToDateTime();
        foreach (var slot in slots)
            slot.Annul(request.Reason, annulmentDateTime);

        await shiftInstanceRepo.UpdateAsync(shift, context.CancellationToken);

        return new GenerateCallSheetResponse { Shift = MapShiftToResponse(shift) };
    }

    public override async Task<GenerateCallSheetResponse> DoNotFillPosition(
        DoNotFillPositionRequest request, ServerCallContext context)
    {
        var shiftCtrlNbr = ControlNumber.Create(request.ShiftInstanceCtrlNbr);
        var slotCtrlNbr = ControlNumber.Create(request.PositionSlotCtrlNbr);

        var shift = await shiftInstanceRepo.GetByCtrlNbrAsync(shiftCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shift instance {request.ShiftInstanceCtrlNbr} not found."));

        var slot = shift.PositionSlots.SingleOrDefault(s => s.CtrlNbr == slotCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Position slot {request.PositionSlotCtrlNbr} not found on shift."));

        slot.MarkDoNotFill();
        await shiftInstanceRepo.UpdateAsync(shift, context.CancellationToken);

        return new GenerateCallSheetResponse { Shift = MapShiftToResponse(shift) };
    }

    public override async Task<GenerateCallSheetResponse> RestorePositionSlot(
        RestorePositionSlotRequest request, ServerCallContext context)
    {
        var shiftCtrlNbr = ControlNumber.Create(request.ShiftInstanceCtrlNbr);
        var slotCtrlNbr = ControlNumber.Create(request.PositionSlotCtrlNbr);

        var shift = await shiftInstanceRepo.GetByCtrlNbrAsync(shiftCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shift instance {request.ShiftInstanceCtrlNbr} not found."));

        var slot = shift.PositionSlots.SingleOrDefault(s => s.CtrlNbr == slotCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Position slot {request.PositionSlotCtrlNbr} not found on shift."));

        slot.RestoreSlot();
        await shiftInstanceRepo.UpdateAsync(shift, context.CancellationToken);

        return new GenerateCallSheetResponse { Shift = MapShiftToResponse(shift) };
    }

    public override async Task<GenerateCallSheetResponse> RestoreAssignment(
        RestoreAssignmentRequest request, ServerCallContext context)
    {
        var shiftCtrlNbr = ControlNumber.Create(request.ShiftInstanceCtrlNbr);
        var assignmentCtrlNbr = ControlNumber.Create(request.AssignmentCtrlNbr);

        var shift = await shiftInstanceRepo.GetByCtrlNbrAsync(shiftCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shift instance {request.ShiftInstanceCtrlNbr} not found."));

        var slots = shift.PositionSlots
            .Where(s => s.AssignmentCtrlNbr == assignmentCtrlNbr && s.IsAnnulled)
            .ToList();

        if (slots.Count == 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "No annulled positions found for this assignment."));

        foreach (var slot in slots)
            slot.RestoreSlot();

        await shiftInstanceRepo.UpdateAsync(shift, context.CancellationToken);

        return new GenerateCallSheetResponse { Shift = MapShiftToResponse(shift) };
    }

    public override async Task<GenerateCallSheetResponse> SaveAssignmentNote(
        SaveAssignmentNoteRequest request, ServerCallContext context)
    {
        var shiftCtrlNbr = ControlNumber.Create(request.ShiftInstanceCtrlNbr);
        var assignmentCtrlNbr = ControlNumber.Create(request.AssignmentCtrlNbr);

        var shift = await shiftInstanceRepo.GetByCtrlNbrAsync(shiftCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shift instance {request.ShiftInstanceCtrlNbr} not found."));

        shift.SetAssignmentNote(assignmentCtrlNbr, request.NoteText);
        await shiftInstanceRepo.UpdateAsync(shift, context.CancellationToken);

        return new GenerateCallSheetResponse { Shift = MapShiftToResponse(shift) };
    }


    public override async Task<GenerateCallSheetResponse> ManageAssignmentPositions(
        ManageAssignmentPositionsRequest request, ServerCallContext context)
    {
        var shiftCtrlNbr = ControlNumber.Create(request.ShiftInstanceCtrlNbr);
        var assignmentCtrlNbr = ControlNumber.Create(request.AssignmentCtrlNbr);

        var shift = await shiftInstanceRepo.GetByCtrlNbrAsync(shiftCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shift instance {request.ShiftInstanceCtrlNbr} not found."));

        try
        {
            foreach (var slotCtrlNbr in request.RemovedPositionSlotCtrlNbrs)
                shift.RemovePositionSlot(ControlNumber.Create(slotCtrlNbr));

            foreach (var craftRoleName in request.AddedCraftRoleNames)
                shift.AddAdHocPositionSlot(assignmentCtrlNbr, craftRoleName);

            if (request.PositionSlotOrders.Count > 0)
            {
                var orders = request.PositionSlotOrders
                    .Select(o => (ControlNumber.Create(o.CtrlNbr), o.DisplayOrder));
                shift.ReorderPositionSlots(orders);
            }

            await shiftInstanceRepo.UpdateAsync(shift, context.CancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }

        return new GenerateCallSheetResponse { Shift = MapShiftToResponse(shift) };
    }


    public override async Task<GenerateCallSheetResponse> RefreshShiftInstance(
        RefreshShiftInstanceRequest request, ServerCallContext context)
    {
        var newShift = await callSheetGeneration.RegenerateShiftAsync(
            ControlNumber.Create(request.CtrlNbr), context.CancellationToken);

        return new GenerateCallSheetResponse { Shift = MapShiftToResponse(newShift) };
    }

    public override async Task<DeleteResponse> CloseShiftInstance(
        CloseShiftInstanceRequest request, ServerCallContext context)
    {
        var ctrlNbr = ControlNumber.Create(request.CtrlNbr);
        var shift = await shiftInstanceRepo.GetByCtrlNbrAsync(ctrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shift instance {request.CtrlNbr} not found."));

        shift.Complete();
        await shiftInstanceRepo.UpdateAsync(shift, context.CancellationToken);
        return new DeleteResponse { Success = true };
    }

    public override async Task<GenerateCallSheetResponse> ReopenShiftInstance(
        ReopenShiftInstanceRequest request, ServerCallContext context)
    {
        var ctrlNbr = ControlNumber.Create(request.CtrlNbr);
        var shift = await shiftInstanceRepo.GetByCtrlNbrAsync(ctrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shift instance {request.CtrlNbr} not found."));

        shift.Reopen();
        await shiftInstanceRepo.UpdateAsync(shift, context.CancellationToken);
        return new GenerateCallSheetResponse { Shift = MapShiftToResponse(shift) };
    }

    public override async Task<GetAvailableExtraAssignmentsResponse> GetAvailableExtraAssignments(
        GetAvailableExtraAssignmentsRequest request, ServerCallContext context)
    {
        var shiftCtrlNbr = ControlNumber.Create(request.ShiftInstanceCtrlNbr);
        var shift = await shiftInstanceRepo.GetByCtrlNbrAsync(shiftCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shift instance {request.ShiftInstanceCtrlNbr} not found."));

        var workInstance = await workInstanceRepo.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Work instance {shift.WorkInstanceCtrlNbr} not found."));

        var targetDate = DateOnly.FromDateTime(workInstance.StartUtc);

        var extras = await assignmentQuery.GetExtraAssignmentsForShiftAsync(
            workInstance.WorkAreaGroupCtrlNbr, shift.ShiftDefinitionCtrlNbr, targetDate, shift.DepartmentCtrlNbr, context.CancellationToken);

        var existingAssignmentCtrlNbrs = shift.PositionSlots
            .Select(s => s.AssignmentCtrlNbr)
            .Distinct()
            .ToHashSet();

        var response = new GetAvailableExtraAssignmentsResponse();
        foreach (var a in extras)
        {
            if (existingAssignmentCtrlNbrs.Contains(a.AssignmentCtrlNbr))
                continue;

            response.Assignments.Add(new AvailableAssignmentResponse
            {
                AssignmentCtrlNbr = a.AssignmentCtrlNbr.Value,
                AssignmentCode = a.AssignmentCode,
                AssignmentName = a.AssignmentName,
                OnDutyTime = a.OnDutyTime.ToString("hh\\:mm tt"),
                OffDutyTime = a.OffDutyTime.ToString("hh\\:mm tt"),
                GroupName = a.GroupName,
                GroupCode = a.GroupCode,
                PositionCount = a.Positions.Count
            });
        }

        return response;
    }

    public override async Task<GenerateCallSheetResponse> AddAssignmentFromTemplate(
        AddAssignmentFromTemplateRequest request, ServerCallContext context)
    {
        var shiftCtrlNbr = ControlNumber.Create(request.ShiftInstanceCtrlNbr);
        var assignmentCtrlNbr = ControlNumber.Create(request.AssignmentCtrlNbr);

        var shift = await shiftInstanceRepo.GetByCtrlNbrAsync(shiftCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shift instance {request.ShiftInstanceCtrlNbr} not found."));

        var workInstance = await workInstanceRepo.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Work instance {shift.WorkInstanceCtrlNbr} not found."));

        var targetDate = DateOnly.FromDateTime(workInstance.StartUtc);

        var extras = await assignmentQuery.GetExtraAssignmentsForShiftAsync(
            workInstance.WorkAreaGroupCtrlNbr, shift.ShiftDefinitionCtrlNbr, targetDate, shift.DepartmentCtrlNbr, context.CancellationToken);

        var template = extras.FirstOrDefault(a => a.AssignmentCtrlNbr == assignmentCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Extra assignment {request.AssignmentCtrlNbr} not found or not eligible."));

        try
        {
            var onDutyTime = !string.IsNullOrEmpty(request.OnDutyTime) && TimeOnly.TryParse(request.OnDutyTime, out var parsedOn)
                ? parsedOn : template.OnDutyTime;
            var offDutyTime = !string.IsNullOrEmpty(request.OffDutyTime) && TimeOnly.TryParse(request.OffDutyTime, out var parsedOff)
                ? parsedOff : template.OffDutyTime;

            var positions = template.Positions
                .Select(p => (p.PositionCtrlNbr, p.IncumbentEmployeeCtrlNbr, p.DisplayOrder, p.CraftRoleName))
                .ToList();

            shift.AddTemplateAssignment(
                template.AssignmentCtrlNbr, template.AssignmentCode, template.AssignmentName,
                template.GroupName, template.GroupCode, onDutyTime, offDutyTime,
                positions);

            await shiftInstanceRepo.UpdateAsync(shift, context.CancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }

        return new GenerateCallSheetResponse { Shift = MapShiftToResponse(shift) };
    }

    public override async Task<GenerateCallSheetResponse> AddAdHocAssignment(
        AddAdHocAssignmentRequest request, ServerCallContext context)
    {
        var shiftCtrlNbr = ControlNumber.Create(request.ShiftInstanceCtrlNbr);

        var shift = await shiftInstanceRepo.GetByCtrlNbrAsync(shiftCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shift instance {request.ShiftInstanceCtrlNbr} not found."));

        if (!TimeOnly.TryParse(request.OnDutyTime, out var onDutyTime))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid on_duty_time format."));

        if (!TimeOnly.TryParse(request.OffDutyTime, out var offDutyTime))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid off_duty_time format."));

        try
        {
            shift.AddAdHocAssignment(
                request.AssignmentCode, request.AssignmentName,
                request.GroupName, request.GroupCode,
                onDutyTime, offDutyTime,
                request.CraftRoleNames.ToList());

            await shiftInstanceRepo.UpdateAsync(shift, context.CancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }

        return new GenerateCallSheetResponse { Shift = MapShiftToResponse(shift) };
    }

    public override async Task<GenerateCallSheetResponse> RemoveAssignment(
        RemoveAssignmentRequest request, ServerCallContext context)
    {
        var shiftCtrlNbr = ControlNumber.Create(request.ShiftInstanceCtrlNbr);
        var assignmentCtrlNbr = ControlNumber.Create(request.AssignmentCtrlNbr);

        var shift = await shiftInstanceRepo.GetByCtrlNbrAsync(shiftCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shift instance {request.ShiftInstanceCtrlNbr} not found."));

        try
        {
            shift.RemoveAssignment(assignmentCtrlNbr);
            await shiftInstanceRepo.UpdateAsync(shift, context.CancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }

        return new GenerateCallSheetResponse { Shift = MapShiftToResponse(shift) };
    }

    private static DailyShiftInstanceResponse MapShiftToResponse(ShiftInstance shift)
    {
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
                OnDutyTime = slot.OnDutyTime.ToString("hh\\:mm tt"),
                OffDutyTime = slot.OffDutyTime.ToString("hh\\:mm tt"),
                GroupName = slot.GroupName,
                GroupCode = slot.GroupCode,
                IsIncumbent = slot.IsIncumbent,
            };
            if (slot.IncumbentEmployeeCtrlNbr is not null)
                slotResp.IncumbentEmployeeCtrlNbr = slot.IncumbentEmployeeCtrlNbr.Value;
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