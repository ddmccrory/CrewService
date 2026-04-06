using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class DailyOperationsClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<DailyOperationsClient> logger)
    : BaseGrpcClient<DailyOperationsSrvc.DailyOperationsSrvcClient>(channelProvider, tokenProvider, callInvoker => new DailyOperationsSrvc.DailyOperationsSrvcClient(callInvoker), logger)
{
    public async Task<GetCallSheetResponse> GetCallSheetAsync(long workAreaGroupCtrlNbr, string targetDate, bool includeClosed = false)
    {
        try
        {
            return await _client.GetCallSheetAsync(new GetCallSheetRequest
            {
                WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
                TargetDate = targetDate,
                IncludeClosed = includeClosed
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> GenerateCallSheetAsync(long workAreaGroupCtrlNbr, long shiftDefinitionCtrlNbr, string targetDate, long departmentCtrlNbr = 0)
    {
        try
        {
            var req = new GenerateCallSheetRequest
            {
                WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
                ShiftDefinitionCtrlNbr = shiftDefinitionCtrlNbr,
                TargetDate = targetDate
            };
            if (departmentCtrlNbr > 0) req.DepartmentCtrlNbr = departmentCtrlNbr;
            return await _client.GenerateCallSheetAsync(req);
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> RefreshShiftInstanceAsync(long ctrlNbr)
    {
        try
        {
            return await _client.RefreshShiftInstanceAsync(new RefreshShiftInstanceRequest
            {
                CtrlNbr = ctrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<DeleteResponse> CloseShiftInstanceAsync(long ctrlNbr)
    {
        try
        {
            return await _client.CloseShiftInstanceAsync(new CloseShiftInstanceRequest
            {
                CtrlNbr = ctrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> ReopenShiftInstanceAsync(long ctrlNbr)
    {
        try
        {
            return await _client.ReopenShiftInstanceAsync(new ReopenShiftInstanceRequest
            {
                CtrlNbr = ctrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> AnnulPositionAsync(long shiftInstanceCtrlNbr, long positionSlotCtrlNbr, string reason, DateTime annulmentDateTimeUtc)
    {
        try
        {
            return await _client.AnnulPositionAsync(new AnnulPositionRequest
            {
                ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
                PositionSlotCtrlNbr = positionSlotCtrlNbr,
                Reason = reason,
                AnnulmentDateTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annulmentDateTimeUtc)
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> AnnulAssignmentAsync(long shiftInstanceCtrlNbr, long assignmentCtrlNbr, string reason, DateTime annulmentDateTimeUtc)
    {
        try
        {
            return await _client.AnnulAssignmentAsync(new AnnulAssignmentRequest
            {
                ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
                AssignmentCtrlNbr = assignmentCtrlNbr,
                Reason = reason,
                AnnulmentDateTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annulmentDateTimeUtc)
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> DoNotFillPositionAsync(long shiftInstanceCtrlNbr, long positionSlotCtrlNbr)
    {
        try
        {
            return await _client.DoNotFillPositionAsync(new DoNotFillPositionRequest
            {
                ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
                PositionSlotCtrlNbr = positionSlotCtrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> RestorePositionAsync(long shiftInstanceCtrlNbr, long positionSlotCtrlNbr)
    {
        try
        {
            return await _client.RestorePositionSlotAsync(new RestorePositionSlotRequest
            {
                ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
                PositionSlotCtrlNbr = positionSlotCtrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> RestoreAssignmentAsync(long shiftInstanceCtrlNbr, long assignmentCtrlNbr)
    {
        try
        {
            return await _client.RestoreAssignmentAsync(new RestoreAssignmentRequest
            {
                ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
                AssignmentCtrlNbr = assignmentCtrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> SaveAssignmentNoteAsync(long shiftInstanceCtrlNbr, long assignmentCtrlNbr, string noteText)
    {
        try
        {
            return await _client.SaveAssignmentNoteAsync(new SaveAssignmentNoteRequest
            {
                ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
                AssignmentCtrlNbr = assignmentCtrlNbr,
                NoteText = noteText
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GenerateCallSheetResponse> ManageAssignmentPositionsAsync(
        long shiftInstanceCtrlNbr,
        long assignmentCtrlNbr,
        IEnumerable<string> addedCraftRoleNames,
        IEnumerable<long> removedPositionSlotCtrlNbrs,
        IEnumerable<(long CtrlNbr, int DisplayOrder)> positionSlotOrders)
    {
        try
        {
            var req = new ManageAssignmentPositionsRequest
            {
                ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
                AssignmentCtrlNbr = assignmentCtrlNbr
            };
            req.AddedCraftRoleNames.AddRange(addedCraftRoleNames);
            req.RemovedPositionSlotCtrlNbrs.AddRange(removedPositionSlotCtrlNbrs);
            req.PositionSlotOrders.AddRange(positionSlotOrders.Select(o => new PositionSlotOrderEntry
            {
                CtrlNbr = o.CtrlNbr,
                DisplayOrder = o.DisplayOrder
            }));
            return await _client.ManageAssignmentPositionsAsync(req);
        }
        catch (Exception ex) { LogException(ex); throw; }
    }
}
