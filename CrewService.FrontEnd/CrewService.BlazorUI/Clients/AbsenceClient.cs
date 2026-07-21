using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Google.Protobuf.WellKnownTypes;

namespace CrewService.BlazorUI.Clients;

public sealed class AbsenceClient(
    GrpcChannelProvider channelProvider,
    CircuitTokenProvider tokenProvider,
    AppContextService appContext,
    ILogger<AbsenceClient> logger)
    : BaseGrpcClient<MarkOffSrvc.MarkOffSrvcClient>(
        channelProvider,
        tokenProvider,
        appContext,
        callInvoker => new MarkOffSrvc.MarkOffSrvcClient(callInvoker),
        logger)
{
    public async Task<MarkOffAbsenceResponse> CreateAbsenceRequestAsync(
        long employeeCtrlNbr,
        long absenceCodeCtrlNbr,
        DateTime startUtc,
        DateTime? endUtc,
        long? positionSlotCtrlNbr,
        string? notes,
        bool isSystemGenerated)
    {
        try
        {
            var request = new CreateAbsenceRequestMsg
            {
                EmployeeCtrlNbr = employeeCtrlNbr,
                AbsenceCodeCtrlNbr = absenceCodeCtrlNbr,
                StartUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc)),
                IsSystemGenerated = isSystemGenerated
            };

            if (endUtc.HasValue)
                request.EndUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(endUtc.Value, DateTimeKind.Utc));

            if (positionSlotCtrlNbr.HasValue)
                request.PositionSlotCtrlNbr = positionSlotCtrlNbr.Value;

            if (!string.IsNullOrWhiteSpace(notes))
                request.Notes = notes;

            return await _client.CreateAbsenceRequestAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<AbsenceApprovalResponse> ApproveAbsenceRequestAsync(long absenceRequestCtrlNbr, long officerCtrlNbr, string? notes = null)
    {
        try
        {
            var request = new ApproveAbsenceMsg
            {
                AbsenceRequestCtrlNbr = absenceRequestCtrlNbr,
                OfficerCtrlNbr = officerCtrlNbr
            };

            if (!string.IsNullOrWhiteSpace(notes))
                request.Notes = notes;

            return await _client.ApproveAbsenceAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<AbsenceApprovalResponse> DeclineAbsenceRequestAsync(long absenceRequestCtrlNbr, long officerCtrlNbr, string? notes = null)
    {
        try
        {
            var request = new DeclineAbsenceMsg
            {
                AbsenceRequestCtrlNbr = absenceRequestCtrlNbr,
                OfficerCtrlNbr = officerCtrlNbr
            };

            if (!string.IsNullOrWhiteSpace(notes))
                request.Notes = notes;

            return await _client.DeclineAbsenceAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetMarkOffAbsenceRequestsResponse> GetAbsenceRequestsAsync(
        DateTime requestDateUtc,
        bool includeAllStatuses,
        long? workAreaGroupCtrlNbr = null)
    {
        try
        {
            var request = new GetAbsenceRequestsMsg
            {
                RequestDateUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(requestDateUtc.Date, DateTimeKind.Utc)),
                IncludeAllStatuses = includeAllStatuses
            };

            if (workAreaGroupCtrlNbr.HasValue && workAreaGroupCtrlNbr.Value > 0)
                request.WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr.Value;

            return await _client.GetAbsenceRequestsAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetOpenAbsencesResponse> GetOpenAbsencesAsync(DateTime rangeStartUtc, DateTime rangeEndUtc)
    {
        try
        {
            return await _client.GetOpenAbsencesAsync(new GetOpenAbsencesMsg
            {
                RangeStartUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(rangeStartUtc, DateTimeKind.Utc)),
                RangeEndUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(rangeEndUtc, DateTimeKind.Utc))
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetMarkOffCodesResponse> GetAbsenceCodesAsync(bool activeOnly)
    {
        try
        {
            return await _client.GetMarkOffCodesAsync(new GetMarkOffCodesMsg
            {
                ActiveOnly = activeOnly
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<MarkOffCodeResponse> CreateAbsenceCodeAsync(
        string code,
        string description,
        bool isExcused,
        bool isCompensated,
        bool requiresApproval,
        bool isSystemOnly,
        bool isHolidayExempt,
        decimal? defaultAutoMarkUpHours,
        bool isActive)
    {
        try
        {
            var request = new CreateMarkOffCodeMsg
            {
                Code = code,
                Description = description,
                IsExcused = isExcused,
                IsCompensated = isCompensated,
                RequiresApproval = requiresApproval,
                IsSystemOnly = isSystemOnly,
                IsHolidayExempt = isHolidayExempt,
                IsActive = isActive
            };

            if (defaultAutoMarkUpHours.HasValue)
                request.DefaultAutoMarkUpHours = Convert.ToDouble(defaultAutoMarkUpHours.Value);

            return await _client.CreateMarkOffCodeAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<MarkOffCodeResponse> UpdateAbsenceCodeAsync(
        long ctrlNbr,
        string code,
        string description,
        bool isExcused,
        bool isCompensated,
        bool requiresApproval,
        bool isSystemOnly,
        bool isHolidayExempt,
        decimal? defaultAutoMarkUpHours,
        bool isActive)
    {
        try
        {
            var request = new UpdateMarkOffCodeMsg
            {
                CtrlNbr = ctrlNbr,
                Code = code,
                Description = description,
                IsExcused = isExcused,
                IsCompensated = isCompensated,
                RequiresApproval = requiresApproval,
                IsSystemOnly = isSystemOnly,
                IsHolidayExempt = isHolidayExempt,
                IsActive = isActive
            };

            if (defaultAutoMarkUpHours.HasValue)
                request.DefaultAutoMarkUpHours = Convert.ToDouble(defaultAutoMarkUpHours.Value);

            return await _client.UpdateMarkOffCodeAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteResponse> DeleteAbsenceCodeAsync(long ctrlNbr)
    {
        try
        {
            return await _client.DeleteMarkOffCodeAsync(new DeleteMarkOffCodeMsg
            {
                CtrlNbr = ctrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
