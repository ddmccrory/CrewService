namespace CrewService.Domain.Modules.Staffing;

/// <summary>
/// Canonical codes for staffable position types.
/// "C" = crew position (regular assignment slot).
/// "B" = board position (extra board slot).
/// </summary>
public static class StaffablePositionType
{
    public const string Crew  = "C";
    public const string Board = "B";

    public static bool IsValid(string value) =>
        value == Crew || value == Board;
}

/// <summary>
/// Canonical codes written to <see cref="PositionAssignment.AssignmentType"/>
/// and <see cref="Bulletins.Bulletin.AwardType"/> to record how a position was filled.
/// </summary>
public static class PositionAssignmentType
{
    public const string BulletinAssignment = "BA";
    public const string ForceAssignment    = "FA";
    public const string SeniorityMove      = "SM";
    /// <summary>Direct board/crew assignment (no bulletin).</summary>
    public const string Direct             = "C";
    /// <summary>Board placement.</summary>
    public const string Board              = "B";
}
