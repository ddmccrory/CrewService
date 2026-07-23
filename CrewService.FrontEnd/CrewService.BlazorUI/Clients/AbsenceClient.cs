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
        string? notes,
        bool isSystemGenerated,
        long? approvedByCtrlNbr = null,
        bool autoMarkOffOnApproval = false)
    {
        try
        {
            var request = new CreateAbsenceRequestMsg
            {
                EmployeeCtrlNbr = employeeCtrlNbr,
                AbsenceCodeCtrlNbr = absenceCodeCtrlNbr,
                StartUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc)),
                IsSystemGenerated = isSystemGenerated,
                AutoMarkOffOnApproval = autoMarkOffOnApproval
            };

            if (endUtc.HasValue)
                request.EndUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(endUtc.Value, DateTimeKind.Utc));

            if (!string.IsNullOrWhiteSpace(notes))
                request.Notes = notes;

            if (approvedByCtrlNbr is > 0)
                request.ApprovedByCtrlNbr = approvedByCtrlNbr.Value;

            return await _client.CreateAbsenceRequestAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetAbsenceApprovalContextResponse> GetCreateAbsenceApprovalContextAsync(long employeeCtrlNbr, long absenceCodeCtrlNbr)
    {
        try
        {
            return await _client.GetCreateAbsenceApprovalContextAsync(new GetCreateAbsenceApprovalContextMsg
            {
                EmployeeCtrlNbr = employeeCtrlNbr,
                AbsenceCodeCtrlNbr = absenceCodeCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetAbsenceApprovalContextResponse> GetAbsenceApprovalContextAsync(long absenceRequestCtrlNbr)
    {
        try
        {
            return await _client.GetAbsenceApprovalContextAsync(new GetAbsenceApprovalContextMsg
            {
                AbsenceRequestCtrlNbr = absenceRequestCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<AbsenceApprovalResponse> MarkOffAbsenceRequestAsync(long absenceRequestCtrlNbr)
    {
        try
        {
            var request = new MarkOffAbsenceMsg
            {
                AbsenceRequestCtrlNbr = absenceRequestCtrlNbr
            };

            return await _client.MarkOffAbsenceAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetMarkOffAbsenceRequestsResponse> GetScheduledAbsencesAsync(
        long? workAreaGroupCtrlNbr = null,
        long? craftCtrlNbr = null,
        long? departmentCtrlNbr = null,
        long? employeeCtrlNbr = null,
        bool currentMonthOnly = false)
    {
        try
        {
            var request = new GetScheduledAbsencesMsg
            {
                CurrentMonthOnly = currentMonthOnly
            };

            if (workAreaGroupCtrlNbr.HasValue && workAreaGroupCtrlNbr.Value > 0)
                request.WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr.Value;

            if (craftCtrlNbr.HasValue && craftCtrlNbr.Value > 0)
                request.CraftCtrlNbr = craftCtrlNbr.Value;

            if (departmentCtrlNbr.HasValue && departmentCtrlNbr.Value > 0)
                request.DepartmentCtrlNbr = departmentCtrlNbr.Value;

            if (employeeCtrlNbr.HasValue && employeeCtrlNbr.Value > 0)
                request.EmployeeCtrlNbr = employeeCtrlNbr.Value;

            return await _client.GetScheduledAbsencesAsync(request);
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
        long? workAreaGroupCtrlNbr = null,
        long? craftCtrlNbr = null,
        long? departmentCtrlNbr = null,
        long? employeeCtrlNbr = null)
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

            if (craftCtrlNbr.HasValue && craftCtrlNbr.Value > 0)
                request.CraftCtrlNbr = craftCtrlNbr.Value;

            if (departmentCtrlNbr.HasValue && departmentCtrlNbr.Value > 0)
                request.DepartmentCtrlNbr = departmentCtrlNbr.Value;

            if (employeeCtrlNbr.HasValue && employeeCtrlNbr.Value > 0)
                request.EmployeeCtrlNbr = employeeCtrlNbr.Value;

            return await _client.GetAbsenceRequestsAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetOpenAbsencesResponse> GetOpenAbsencesAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        long? workAreaGroupCtrlNbr = null,
        long? craftCtrlNbr = null,
        long? departmentCtrlNbr = null,
        long? employeeCtrlNbr = null)
    {
        try
        {
            var request = new GetOpenAbsencesMsg
            {
                RangeStartUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(rangeStartUtc, DateTimeKind.Utc)),
                RangeEndUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(rangeEndUtc, DateTimeKind.Utc))
            };

            if (workAreaGroupCtrlNbr.HasValue && workAreaGroupCtrlNbr.Value > 0)
                request.WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr.Value;

            if (craftCtrlNbr.HasValue && craftCtrlNbr.Value > 0)
                request.CraftCtrlNbr = craftCtrlNbr.Value;

            if (departmentCtrlNbr.HasValue && departmentCtrlNbr.Value > 0)
                request.DepartmentCtrlNbr = departmentCtrlNbr.Value;

            if (employeeCtrlNbr.HasValue && employeeCtrlNbr.Value > 0)
                request.EmployeeCtrlNbr = employeeCtrlNbr.Value;

            return await _client.GetOpenAbsencesAsync(request);
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
