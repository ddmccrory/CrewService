using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class AssignmentsService(
    IAssignmentRepository assignmentRepository,
    IAssignmentScheduleRepository scheduleRepository,
    IShiftDefinitionRepository shiftDefinitionRepository,
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

        var response = new GetAssignmentsResponse { TotalCount = assignments.Count };
        foreach (var a in assignments)
            response.Assignments.Add(MapAssignment(a));
        return response;
    }

    public override async Task<StaffingAssignmentResponse> GetAssignment(GetStaffingAssignmentRequest request, ServerCallContext context)
    {
        var assignment = await assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Assignment {request.CtrlNbr} not found."));
        return MapAssignment(assignment);
    }

    public override async Task<StaffingAssignmentResponse> CreateAssignment(CreateStaffingAssignmentRequest request, ServerCallContext context)
    {
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        var assignment = Assignment.Create(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
            request.Code,
            request.Name,
            request.IsExtra,
            request.IsActive,
            departmentCtrlNbr);

        await using var uow = await uowFactory.CreateAsync();
        uow.Assignments.Add(assignment);
        await uow.CommitAsync();

        return MapAssignment(assignment);
    }

    public override async Task<StaffingAssignmentResponse> UpdateAssignment(UpdateStaffingAssignmentRequest request, ServerCallContext context)
    {
        var assignment = await assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Assignment {request.CtrlNbr} not found."));
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        assignment.Update(request.Code, request.Name, request.IsExtra, request.IsActive, departmentCtrlNbr);

        await using var uow = await uowFactory.CreateAsync();
        uow.Assignments.Update(assignment);
        await uow.CommitAsync();

        return MapAssignment(assignment);
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
            var shifts = await shiftDefinitionRepository.GetByWorkAreaAsync(assignment.WorkAreaGroupCtrlNbr);
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

    private static StaffingAssignmentResponse MapAssignment(Assignment a) => new()
    {
        CtrlNbr = a.CtrlNbr.Value,
        WorkAreaGroupCtrlNbr = a.WorkAreaGroupCtrlNbr.Value,
        DepartmentCtrlNbr = a.DepartmentCtrlNbr?.Value ?? 0,
        Code = a.Code,
        Name = a.Name,
        IsExtra = a.IsExtra,
        IsActive = a.IsActive
    };

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
