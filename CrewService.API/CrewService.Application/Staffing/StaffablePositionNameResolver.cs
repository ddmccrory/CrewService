using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Staffing;

/// <summary>
/// Resolves the display name of a staffable position (e.g. "350 / Engineer"),
/// shared by every read model that surfaces a position to the UI.
/// A board position resolves to the owning board's name; a crew position
/// resolves to "Crew.Name / CraftRole.Name". Returns an empty string when the
/// position cannot be resolved.
/// </summary>
public static class StaffablePositionNameResolver
{
    public static async Task<string> ResolveAsync(
        IOrchestrationUnitOfWork uow, ControlNumber staffablePositionCtrlNbr, CancellationToken ct = default)
    {
        // Authoritative rule: Hangout auto-moves can persist the target roster-board
        // control number before execution creates a concrete board position. Resolve
        // that board control number directly to the board name.
        var boardByCtrlNbr = await uow.RosterBoards.GetByCtrlNbrAsync(staffablePositionCtrlNbr, ct);
        if (boardByCtrlNbr is not null)
            return boardByCtrlNbr.Name;

        var pos = await uow.StaffablePositions.GetByCtrlNbrAsync(staffablePositionCtrlNbr, ct);
        if (pos is null) return string.Empty;

        if (pos.PositionType == StaffablePositionType.Board)
        {
            var board = await uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(staffablePositionCtrlNbr, ct);
            return board?.Name ?? pos.PositionType;
        }

        var crewPos = await uow.CrewPositions.GetByStaffablePositionAsync(staffablePositionCtrlNbr);
        if (crewPos is null) return pos.PositionType;

        var crew      = await uow.Crews.GetByCtrlNbrAsync(crewPos.CrewCtrlNbr, ct);
        var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPos.CraftRoleCtrlNbr, ct);
        var crewName  = crew?.Name ?? string.Empty;
        var roleName  = craftRole?.Name ?? string.Empty;
        return (crewName, roleName) switch
        {
            ({ Length: > 0 }, { Length: > 0 }) => $"{crewName} / {roleName}",
            ({ Length: > 0 }, _)               => crewName,
            (_, { Length: > 0 })               => roleName,
            _                                  => pos.PositionType
        };
    }
}
