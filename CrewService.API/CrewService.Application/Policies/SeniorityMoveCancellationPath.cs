using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Policies;

public sealed class SeniorityMoveCancellationPath
{
    public async Task<IReadOnlyList<SeniorityMove>> CancelSupersededMovesAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        string reason,
        ControlNumber? excludeMoveCtrlNbr = null,
        CancellationToken ct = default)
    {
        var employeeMoves = await uow.SeniorityMoves.GetByEmployeeAsync(employeeCtrlNbr, ct);
        var cancelled = new List<SeniorityMove>();

        foreach (var move in employeeMoves)
        {
            if (excludeMoveCtrlNbr is not null && move.CtrlNbr == excludeMoveCtrlNbr)
                continue;
            if (move.Status != SeniorityMoveStatus.Pending && move.Status != SeniorityMoveStatus.Approved)
                continue;
            if (move.MoveType != SeniorityMoveType.Voluntary && move.MoveType != SeniorityMoveType.Hangout)
                continue;

            move.Cancel(reason);
            await uow.SeniorityMoves.UpdateAsync(move, ct);
            cancelled.Add(move);
        }

        return cancelled;
    }
}
