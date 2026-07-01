using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.Presentation.Formatting;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class AssignmentsService(IServiceProvider serviceProvider) : AssignmentsSrvc.AssignmentsSrvcBase
{
    public override async Task<GetAssignmentsResponse> GetAssignments(GetAssignmentsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Assignments.AssignmentsService>();
        var workAreaCtrlNbr = request.WorkAreaGroupCtrlNbr > 0 ? ControlNumber.Create(request.WorkAreaGroupCtrlNbr) : null;
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        var railroadCtrlNbr = request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null;

        var (assignments, daysMasks, onDutyTimes, groupLookup, workAreaLookup) =
            await svc.GetAssignmentsWithDetailsAsync(workAreaCtrlNbr, departmentCtrlNbr, railroadCtrlNbr, context.CancellationToken);

        var response = new GetAssignmentsResponse { TotalCount = assignments.Count };
        foreach (var a in assignments)
        {
            groupLookup.TryGetValue(a.GroupCtrlNbr, out var group);
            workAreaLookup.TryGetValue(a.GroupCtrlNbr, out var waVal);

            var mapped = MapAssignment(a, group, waVal);
            daysMasks.TryGetValue(a.CtrlNbr, out var daysMask);
            mapped.WorkDaysMask = daysMask;
            if (onDutyTimes.TryGetValue(a.CtrlNbr, out var onDutyTime))
                mapped.OnDutyTime = onDutyTime;
            response.Assignments.Add(mapped);
        }
        return response;
    }

    public override async Task<StaffingAssignmentResponse> GetAssignment(GetStaffingAssignmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Assignments.AssignmentsService>();
        try
        {
            var assignment = await svc.GetAssignmentAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapAssignment(assignment, null, 0);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<StaffingAssignmentResponse> CreateAssignment(CreateStaffingAssignmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Assignments.AssignmentsService>();
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        try
        {
            var (assignment, group, waVal) = await svc.CreateAssignmentAsync(
                ControlNumber.Create(request.GroupCtrlNbr), request.Code, request.Name,
                request.IsExtra, request.IsActive, departmentCtrlNbr, context.CancellationToken);
            return MapAssignment(assignment, group, waVal);
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
    }

    public override async Task<StaffingAssignmentResponse> UpdateAssignment(UpdateStaffingAssignmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Assignments.AssignmentsService>();
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        var groupCtrlNbr = request.GroupCtrlNbr > 0 ? ControlNumber.Create(request.GroupCtrlNbr) : null;
        try
        {
            var (assignment, group, waVal) = await svc.UpdateAssignmentAsync(
                ControlNumber.Create(request.CtrlNbr), request.Code, request.Name,
                request.IsExtra, request.IsActive, departmentCtrlNbr, groupCtrlNbr, context.CancellationToken);
            return MapAssignment(assignment, group, waVal);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAssignment(DeleteStaffingAssignmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Assignments.AssignmentsService>();
        try
        {
            await svc.DeleteAssignmentAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    // Assignment Schedules
    public override async Task<GetAssignmentSchedulesResponse> GetAssignmentSchedules(GetAssignmentSchedulesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Assignments.AssignmentsService>();
        var (schedules, shiftNames) = await svc.GetAssignmentSchedulesAsync(
            ControlNumber.Create(request.AssignmentCtrlNbr), context.CancellationToken);

        var response = new GetAssignmentSchedulesResponse { TotalCount = schedules.Count };
        foreach (var s in schedules)
        {
            var mapped = MapSchedule(s);
            if (shiftNames.TryGetValue(s.ShiftDefinitionCtrlNbr.Value, out var name))
                mapped.ShiftDisplayName = name;
            response.Schedules.Add(mapped);
        }
        return response;
    }

    public override async Task<AssignmentScheduleResponse> CreateAssignmentSchedule(CreateAssignmentScheduleRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Assignments.AssignmentsService>();
        var schedule = await svc.CreateAssignmentScheduleAsync(
            ControlNumber.Create(request.AssignmentCtrlNbr),
            ControlNumber.Create(request.ShiftDefinitionCtrlNbr),
            request.OperatingDaysMask,
            TimeOnly.Parse(request.OnDutyTime),
            TimeOnly.Parse(request.OffDutyTime),
            context.CancellationToken);
        return MapSchedule(schedule);
    }

    public override async Task<AssignmentScheduleResponse> UpdateAssignmentSchedule(UpdateAssignmentScheduleRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Assignments.AssignmentsService>();
        try
        {
            var schedule = await svc.UpdateAssignmentScheduleAsync(
                ControlNumber.Create(request.CtrlNbr),
                request.OperatingDaysMask,
                TimeOnly.Parse(request.OnDutyTime),
                TimeOnly.Parse(request.OffDutyTime),
                context.CancellationToken);
            return MapSchedule(schedule);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAssignmentSchedule(DeleteAssignmentScheduleRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Assignments.AssignmentsService>();
        try
        {
            await svc.DeleteAssignmentScheduleAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static StaffingAssignmentResponse MapAssignment(Assignment a, DynamicGroup? group, long workAreaCtrlNbr) => new()
    {
        CtrlNbr = a.CtrlNbr.Value,
        GroupCtrlNbr = a.GroupCtrlNbr.Value,
        DepartmentCtrlNbr = a.DepartmentCtrlNbr?.Value ?? 0,
        Code = a.Code,
        Name = a.Name,
        IsExtra = a.IsExtra,
        IsActive = a.IsActive,
        GroupName = group?.Name ?? string.Empty,
        GroupCode = group?.Code ?? string.Empty,
        WorkAreaGroupCtrlNbr = workAreaCtrlNbr
    };

    private static AssignmentScheduleResponse MapSchedule(AssignmentSchedule s) => new()
    {
        CtrlNbr = s.CtrlNbr.Value,
        AssignmentCtrlNbr = s.AssignmentCtrlNbr.Value,
        ShiftDefinitionCtrlNbr = s.ShiftDefinitionCtrlNbr.Value,
        OperatingDaysMask = s.OperatingDaysMask,
        OnDutyTime = ScheduleTimeFormat.Format(s.OnDutyTime),
        OffDutyTime = ScheduleTimeFormat.Format(s.OffDutyTime)
    };
}
