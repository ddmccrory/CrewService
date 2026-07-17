namespace CrewService.Application.Authorization;

/// <summary>
/// Normalized per-request actor context used to evaluate subject-scoped authorization
/// decisions consistently across modules.
/// </summary>
public sealed record RequestActorContext(
    string? CurrentUserId,
    long? CurrentEmployeeCtrlNbr,
    long? RequestedEmployeeCtrlNbr,
    bool IsLinkedEmployee,
    bool IsSelfEmployeeContext,
    bool IsActingOnBehalfOfEmployee,
    long? ParentCtrlNbr,
    long? RailroadCtrlNbr,
    long? WorkAreaCtrlNbr);
