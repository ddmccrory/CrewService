using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Policies;

public sealed class IncumbentAssignmentPath(SeniorityMoveCancellationPath seniorityMoveCancellationPath)
{
    public const string DefaultCancellationReason = "Cancelled because employee was assigned to a different position.";

    public async Task<(PositionAssignment Assignment, IReadOnlyList<SeniorityMove> CancelledMoves)> AssignAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber staffablePositionCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string assignmentType,
        ControlNumber? assignmentSourceCtrlNbr = null,
        DateTime? assignedDateUtc = null,
        string? cancellationReason = null,
        ControlNumber? excludeMoveCtrlNbr = null,
        CancellationToken ct = default)
    {
        var assignment = PositionAssignment.Create(
            staffablePositionCtrlNbr,
            employeeCtrlNbr,
            assignmentType,
            assignmentSourceCtrlNbr,
            assignedDateUtc);

        uow.PositionAssignments.Add(assignment);

        var cancelledMoves = await seniorityMoveCancellationPath.CancelSupersededMovesAsync(
            uow,
            employeeCtrlNbr,
            cancellationReason ?? DefaultCancellationReason,
            excludeMoveCtrlNbr,
            ct);

        return (assignment, cancelledMoves);
    }
}
