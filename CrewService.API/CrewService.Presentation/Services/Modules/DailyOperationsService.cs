using CrewService.Application.DailyOperations;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class DailyOperationsService(
    IShiftInstanceRepository shiftInstanceRepo,
    OnDutyPlacementService onDutyPlacement,
    TieUpService tieUpService)
    : DailyOperationsSrvc.DailyOperationsSrvcBase
{
    public override async Task<GetCallSheetResponse> GetCallSheet(
        GetCallSheetRequest request, ServerCallContext context)
    {
        var workInstanceCtrlNbr = ControlNumber.Create(request.WorkAreaGroupCtrlNbr);
        var shifts = await shiftInstanceRepo.GetByWorkInstanceAsync(workInstanceCtrlNbr, context.CancellationToken);

        var response = new GetCallSheetResponse();
        foreach (var shift in shifts)
        {
            var shiftResp = new DailyShiftInstanceResponse
            {
                CtrlNbr = shift.CtrlNbr.Value,
                ShiftCode = shift.ShiftCode,
                ShiftStart = Timestamp.FromDateTime(DateTime.SpecifyKind(shift.ShiftStartUtc, DateTimeKind.Utc)),
                ShiftEnd = Timestamp.FromDateTime(DateTime.SpecifyKind(shift.ShiftEndUtc, DateTimeKind.Utc)),
                Status = shift.Status,
            };

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
                };
                if (slot.IncumbentEmployeeCtrlNbr is not null)
                    slotResp.IncumbentEmployeeCtrlNbr = slot.IncumbentEmployeeCtrlNbr.Value;
                shiftResp.PositionSlots.Add(slotResp);
            }

            response.Shifts.Add(shiftResp);
        }
        return response;
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
}
