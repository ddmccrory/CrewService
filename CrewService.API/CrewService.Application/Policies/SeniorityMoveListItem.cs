using CrewService.Domain.Modules.Policies;

namespace CrewService.Application.Policies;

/// <summary>
/// A <see cref="SeniorityMove"/> paired with the server-computed
/// <see cref="AutoApprove"/> flag and the resolved
/// <see cref="TargetPositionName"/> for the admin Seniority Moves list.
/// <para>
/// <see cref="AutoApprove"/> mirrors the <c>SeniorityMoveWorker</c> predicate
/// (NoAccess bumps, or a craft policy with AutoApprove enabled). The admin UI
/// uses it to hide the manual Approve/Reject actions, since those moves are
/// approved automatically by the background worker.
/// </para>
/// <para>
/// <see cref="TargetPositionName"/> is the display name of the position the
/// employee is moving to (e.g. "350 / Engineer"); empty when unresolved.
/// </para>
/// <para>
/// <see cref="WorkAreaTimeZoneId"/> is the IANA/Windows timezone id of the work
/// area that owns the target position, used by the presentation layer to render
/// the move's UTC instants as work-area-local times. <c>null</c> = treat as UTC.
/// </para>
/// </summary>
public sealed record SeniorityMoveListItem(
    SeniorityMove Move, bool AutoApprove, string TargetPositionName, string? WorkAreaTimeZoneId = null);
