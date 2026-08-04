using CrewService.Application.Crews;
using CrewService.Application.RosterBoardOps;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Workflows.Effects;

public sealed class SeniorityWorkflowAssignmentPath(
    CrewsAppService crewsAppService,
    RosterBoardAppService rosterBoardAppService)
{
    public async Task<IReadOnlyList<VacatedAssignmentResult>> VacateEmployeeAssignmentsAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct = default)
    {
        var vacateResults = new List<VacatedAssignmentResult>();
        var assignmentActions = new List<AssignmentVacateAction>();

        var assignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        foreach (var assignment in assignments)
        {
            var staffablePosition = await uow.StaffablePositions.GetByCtrlNbrAsync(assignment.StaffablePositionCtrlNbr, ct);
            if (staffablePosition is null)
                continue;

            if (staffablePosition.PositionType == StaffablePositionType.Crew)
            {
                if (assignment.AssignmentSourceCtrlNbr is null)
                {
                    throw new InvalidOperationException(
                        $"Crew assignment source is missing for employee {employeeCtrlNbr.Value} on staffable position {assignment.StaffablePositionCtrlNbr.Value}.");
                }

                var crewPosition = await uow.CrewPositions.GetByCtrlNbrAsync(assignment.AssignmentSourceCtrlNbr, ct)
                    ?? throw new InvalidOperationException(
                        $"Crew position {assignment.AssignmentSourceCtrlNbr.Value} was not found for employee {employeeCtrlNbr.Value} assignment.");

                var incumbency = await uow.CrewIncumbencies.GetActiveByPositionAsync(crewPosition.CtrlNbr, DateTime.UtcNow);
                if (incumbency is null)
                    continue;

                assignmentActions.Add(new AssignmentVacateAction(
                    StaffablePositionType.Crew,
                    incumbency.CtrlNbr));
                continue;
            }

            if (staffablePosition.PositionType == StaffablePositionType.Board && assignment.AssignmentSourceCtrlNbr is not null)
            {
                assignmentActions.Add(new AssignmentVacateAction(
                    StaffablePositionType.Board,
                    assignment.AssignmentSourceCtrlNbr));
                continue;
            }

            if (staffablePosition.PositionType == StaffablePositionType.Board)
            {
                throw new InvalidOperationException(
                    $"Board assignment source is missing for employee {employeeCtrlNbr.Value} on staffable position {assignment.StaffablePositionCtrlNbr.Value}.");
            }
        }

        foreach (var action in assignmentActions)
        {
            if (action.PositionType == StaffablePositionType.Crew)
            {
                var crewResult = await crewsAppService.EndCrewIncumbencyInOrchestrationAsync(
                    uow,
                    action.SourceCtrlNbr,
                    DateTime.UtcNow,
                    reassignEmployee: false,
                    ct);

                if (crewResult is not null)
                {
                    vacateResults.Add(new VacatedAssignmentResult(
                        StaffablePositionType.Crew,
                        BoardCtrlNbr: null,
                        crewResult.VacatedStaffablePositionCtrlNbr,
                        crewResult.PreviousIncumbentCtrlNbr,
                        IsExtraBoard: false));
                }

                continue;
            }

            var boardResult = await rosterBoardAppService.RemoveRosterBoardPositionInOrchestrationAsync(
                uow,
                action.SourceCtrlNbr,
                reassignEmployee: false,
                ct);

            vacateResults.Add(new VacatedAssignmentResult(
                StaffablePositionType.Board,
                boardResult.BoardCtrlNbr,
                boardResult.VacatedStaffablePositionCtrlNbr,
                boardResult.PreviousIncumbentCtrlNbr,
                boardResult.IsExtraBoard));
        }

        return vacateResults;
    }

    private sealed record AssignmentVacateAction(
        string PositionType,
        ControlNumber SourceCtrlNbr);
}

public sealed record VacatedAssignmentResult(
    string PositionType,
    ControlNumber? BoardCtrlNbr,
    ControlNumber VacatedStaffablePositionCtrlNbr,
    ControlNumber? PreviousIncumbentCtrlNbr,
    bool IsExtraBoard);