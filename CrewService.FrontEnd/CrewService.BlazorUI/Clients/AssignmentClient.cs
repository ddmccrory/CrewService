using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class AssignmentClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<AssignmentClient> logger)
    : BaseGrpcClient<AssignmentsSrvc.AssignmentsSrvcClient>(channelProvider, tokenProvider, callInvoker => new AssignmentsSrvc.AssignmentsSrvcClient(callInvoker), logger)
{
    // ── Assignments ──

    public async Task<GetAssignmentsResponse> GetAllAsync(long workAreaGroupCtrlNbr = 0, long railroadCtrlNbr = 0, long departmentCtrlNbr = 0)
    {
        try
        {
            return await _client.GetAssignmentsAsync(new GetAssignmentsRequest
            {
                WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
                RailroadCtrlNbr = railroadCtrlNbr,
                DepartmentCtrlNbr = departmentCtrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<StaffingAssignmentResponse> GetAsync(long ctrlNbr)
    {
        try
        {
            return await _client.GetAssignmentAsync(new GetStaffingAssignmentRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<StaffingAssignmentResponse> CreateAsync(long groupCtrlNbr, string code, string name, bool isExtra, bool isActive, long departmentCtrlNbr = 0)
    {
        try
        {
            return await _client.CreateAssignmentAsync(new CreateStaffingAssignmentRequest
            {
                GroupCtrlNbr = groupCtrlNbr,
                DepartmentCtrlNbr = departmentCtrlNbr,
                Code = code,
                Name = name,
                IsExtra = isExtra,
                IsActive = isActive
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<StaffingAssignmentResponse> UpdateAsync(long ctrlNbr, string code, string name, bool isExtra, bool isActive, long departmentCtrlNbr = 0, long groupCtrlNbr = 0)
    {
        try
        {
            return await _client.UpdateAssignmentAsync(new UpdateStaffingAssignmentRequest
            {
                CtrlNbr = ctrlNbr,
                DepartmentCtrlNbr = departmentCtrlNbr,
                Code = code,
                Name = name,
                IsExtra = isExtra,
                IsActive = isActive,
                GroupCtrlNbr = groupCtrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<DeleteResponse> DeleteAsync(long ctrlNbr)
    {
        try
        {
            return await _client.DeleteAssignmentAsync(new DeleteStaffingAssignmentRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    // ── Assignment Schedules ──

    public async Task<GetAssignmentSchedulesResponse> GetSchedulesAsync(long assignmentCtrlNbr)
    {
        try
        {
            return await _client.GetAssignmentSchedulesAsync(new GetAssignmentSchedulesRequest { AssignmentCtrlNbr = assignmentCtrlNbr });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<AssignmentScheduleResponse> CreateScheduleAsync(long assignmentCtrlNbr, long shiftDefinitionCtrlNbr, int operatingDaysMask, string onDutyTime, string offDutyTime)
    {
        try
        {
            return await _client.CreateAssignmentScheduleAsync(new CreateAssignmentScheduleRequest
            {
                AssignmentCtrlNbr = assignmentCtrlNbr,
                ShiftDefinitionCtrlNbr = shiftDefinitionCtrlNbr,
                OperatingDaysMask = operatingDaysMask,
                OnDutyTime = onDutyTime,
                OffDutyTime = offDutyTime
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<AssignmentScheduleResponse> UpdateScheduleAsync(long ctrlNbr, int operatingDaysMask, string onDutyTime, string offDutyTime)
    {
        try
        {
            return await _client.UpdateAssignmentScheduleAsync(new UpdateAssignmentScheduleRequest
            {
                CtrlNbr = ctrlNbr,
                OperatingDaysMask = operatingDaysMask,
                OnDutyTime = onDutyTime,
                OffDutyTime = offDutyTime
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<DeleteResponse> DeleteScheduleAsync(long ctrlNbr)
    {
        try
        {
            return await _client.DeleteAssignmentScheduleAsync(new DeleteAssignmentScheduleRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }
}
