using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class AssignmentsService(
    IAssignmentRepository assignmentRepository,
    IAssignmentScheduleRepository scheduleRepository,
    IShiftDefinitionRepository shiftDefinitionRepository,
    IDynamicGroupRepository dynamicGroupRepository,
    IOrchestrationUnitOfWorkFactory uowFactory) : AssignmentsSrvc.AssignmentsSrvcBase
{
    public override async Task<GetAssignmentsResponse> GetAssignments(GetAssignmentsRequest request, ServerCallContext context)
    {
        List<Assignment> assignments;
        if (request.WorkAreaGroupCtrlNbr > 0 && request.DepartmentCtrlNbr > 0)
            assignments = await assignmentRepository.GetByWorkAreaAndDepartmentAsync(ControlNumber.Create(request.WorkAreaGroupCtrlNbr), ControlNumber.Create(request.DepartmentCtrlNbr));
        else if (request.WorkAreaGroupCtrlNbr > 0)
            assignments = await assignmentRepository.GetByWorkAreaAsync(ControlNumber.Create(request.WorkAreaGroupCtrlNbr));
        else if (request.RailroadCtrlNbr > 0)
            assignments = await assignmentRepository.GetAllByRailroadAsync(ControlNumber.Create(request.RailroadCtrlNbr));
        else
            assignments = await assignmentRepository.GetAllAsync();

        var assignmentIds = assignments.Select(a => a.CtrlNbr).ToList();
        var allSchedules = await scheduleRepository.GetByAssignmentsAsync(assignmentIds);
        var daysMasks = allSchedules.GroupBy(s => s.AssignmentCtrlNbr)
            .ToDictionary(g => g.Key, g => g.Aggregate(0, (mask, s) => mask | s.OperatingDaysMask));

        var response = new GetAssignmentsResponse { TotalCount = assignments.Count };
        foreach (var a in assignments)
        {
            var mapped = await MapAssignmentAsync(a);
            daysMasks.TryGetValue(a.CtrlNbr, out var daysMask);
            mapped.WorkDaysMask = daysMask;
            response.Assignments.Add(mapped);
        }
        return response;
    }

    public override async Task<StaffingAssignmentResponse> GetAssignment(GetStaffingAssignmentRequest request, ServerCallContext context)
    {
        var assignment = await assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Assignment {request.CtrlNbr} not found."));
        return await MapAssignmentAsync(assignment);
    }

    public override async Task<StaffingAssignmentResponse> CreateAssignment(CreateStaffingAssignmentRequest request, ServerCallContext context)
    {
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        var assignment = Assignment.Create(
            ControlNumber.Create(request.GroupCtrlNbr),
            request.Code,
            request.Name,
            request.IsExtra,
            request.IsActive,
            departmentCtrlNbr);

        await using var uow = await uowFactory.CreateAsync();
        uow.Assignments.Add(assignment);
        await uow.CommitAsync();

        return await MapAssignmentAsync(assignment);
    }

    public override async Task<StaffingAssignmentResponse> UpdateAssignment(UpdateStaffingAssignmentRequest request, ServerCallContext context)
    {
        var assignment = await assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Assignment {request.CtrlNbr} not found."));
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        var groupCtrlNbr = request.GroupCtrlNbr > 0 ? ControlNumber.Create(request.GroupCtrlNbr) : null;
        assignment.Update(request.Code, request.Name, request.IsExtra, request.IsActive, departmentCtrlNbr, groupCtrlNbr);

        await using var uow = await uowFactory.CreateAsync();
        uow.Assignments.Update(assignment);
        await uow.CommitAsync();

        return await MapAssignmentAsync(assignment);
    }

    public override async Task<DeleteResponse> DeleteAssignment(DeleteStaffingAssignmentRequest request, ServerCallContext context)
    {
        var assignment = await assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Assignment {request.CtrlNbr} not found."));

        await using var uow = await uowFactory.CreateAsync();
        uow.Assignments.Remove(assignment);
        await uow.CommitAsync();

        return new DeleteResponse { Success = true };
    }

    // Assignment Schedules
    public override async Task<GetAssignmentSchedulesResponse> GetAssignmentSchedules(GetAssignmentSchedulesRequest request, ServerCallContext context)
    {
        var schedules = await scheduleRepository.GetByAssignmentAsync(ControlNumber.Create(request.AssignmentCtrlNbr));

        // Resolve shift display names
        var shiftCtrlNbrs = schedules.Select(s => s.ShiftDefinitionCtrlNbr).Distinct().ToList();
        var assignment = await assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.AssignmentCtrlNbr));
        var shiftNames = new Dictionary<long, string>();
        if (assignment is not null)
        {
            var workAreaCtrlNbr = await ResolveWorkAreaCtrlNbrAsync(assignment.GroupCtrlNbr);
            var shifts = workAreaCtrlNbr is not null ? await shiftDefinitionRepository.GetByWorkAreaAsync(workAreaCtrlNbr) : [];
            foreach (var sd in shifts)
                shiftNames[sd.CtrlNbr.Value] = $"{sd.ShiftCode} \u2014 {sd.DisplayName}";
        }

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
        var schedule = AssignmentSchedule.Create(
            ControlNumber.Create(request.AssignmentCtrlNbr),
            ControlNumber.Create(request.ShiftDefinitionCtrlNbr),
            request.OperatingDaysMask,
            TimeOnly.Parse(request.OnDutyTime),
            TimeOnly.Parse(request.OffDutyTime));

        await using var uow = await uowFactory.CreateAsync();
        uow.AssignmentSchedules.Add(schedule);
        await uow.CommitAsync();

        return MapSchedule(schedule);
    }

    public override async Task<AssignmentScheduleResponse> UpdateAssignmentSchedule(UpdateAssignmentScheduleRequest request, ServerCallContext context)
    {
        var schedule = await scheduleRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"AssignmentSchedule {request.CtrlNbr} not found."));
        schedule.Update(request.OperatingDaysMask, TimeOnly.Parse(request.OnDutyTime), TimeOnly.Parse(request.OffDutyTime));

        await using var uow = await uowFactory.CreateAsync();
        uow.AssignmentSchedules.Update(schedule);
        await uow.CommitAsync();

        return MapSchedule(schedule);
    }

    public override async Task<DeleteResponse> DeleteAssignmentSchedule(DeleteAssignmentScheduleRequest request, ServerCallContext context)
    {
        var schedule = await scheduleRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"AssignmentSchedule {request.CtrlNbr} not found."));

        await using var uow = await uowFactory.CreateAsync();
        uow.AssignmentSchedules.Remove(schedule);
        await uow.CommitAsync();

        return new DeleteResponse { Success = true };
    }

    private async Task<StaffingAssignmentResponse> MapAssignmentAsync(Assignment a)
    {
        var group = await dynamicGroupRepository.GetByCtrlNbrAsync(a.GroupCtrlNbr);
        var workAreaCtrlNbr = 0L;
        if (group is not null)
        {
            if (group.IsWorkArea)
                workAreaCtrlNbr = group.CtrlNbr.Value;
            else
            {
                var ancestors = await dynamicGroupRepository.GetAncestorsAsync(a.GroupCtrlNbr);
                workAreaCtrlNbr = ancestors.FirstOrDefault(g => g.IsWorkArea)?.CtrlNbr.Value ?? 0;
            }
        }
        return new StaffingAssignmentResponse
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
    }

    private async Task<ControlNumber?> ResolveWorkAreaCtrlNbrAsync(ControlNumber groupCtrlNbr)
    {
        var group = await dynamicGroupRepository.GetByCtrlNbrAsync(groupCtrlNbr);
        if (group is null) return null;
        if (group.IsWorkArea) return group.CtrlNbr;
        var ancestors = await dynamicGroupRepository.GetAncestorsAsync(groupCtrlNbr);
        return ancestors.FirstOrDefault(g => g.IsWorkArea)?.CtrlNbr;
    }

    private static AssignmentScheduleResponse MapSchedule(AssignmentSchedule s) => new()
    {
        CtrlNbr = s.CtrlNbr.Value,
        AssignmentCtrlNbr = s.AssignmentCtrlNbr.Value,
        ShiftDefinitionCtrlNbr = s.ShiftDefinitionCtrlNbr.Value,
        OperatingDaysMask = s.OperatingDaysMask,
        OnDutyTime = s.OnDutyTime.ToString("HH:mm"),
        OffDutyTime = s.OffDutyTime.ToString("HH:mm")
    };
}
