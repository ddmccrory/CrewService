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
    TieUpService tieUpService)
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

    public override Task<DailyPositionSlotResponse> AnnulPosition(
        AnnulPositionRequest request, ServerCallContext context)
    {
        return Task.FromResult(new DailyPositionSlotResponse());
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
                CrewPositionCtrlNbr = slot.CrewPositionCtrlNbr.Value,
                Status = slot.Status,
                IsAnnulled = slot.IsAnnulled,
                IsDoNotFill = slot.IsDoNotFill,
                IsSkipped = slot.IsSkipped,
                DisplayOrder = slot.DisplayOrder,
                AssignmentCtrlNbr = slot.AssignmentCtrlNbr.Value,
                AssignmentCode = slot.AssignmentCode,
                AssignmentName = slot.AssignmentName,
                CraftRoleName = slot.CraftRoleName,
            };
            if (slot.IncumbentEmployeeCtrlNbr is not null)
                slotResp.IncumbentEmployeeCtrlNbr = slot.IncumbentEmployeeCtrlNbr.Value;
            shiftResp.PositionSlots.Add(slotResp);
        }

        return shiftResp;
    }
}
