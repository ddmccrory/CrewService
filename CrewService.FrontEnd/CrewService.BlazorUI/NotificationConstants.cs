namespace CrewService.BlazorUI;

/// <summary>
/// Mirror of the server-side notification category string constants (kept local because the
/// BlazorUI does not reference the domain assembly) plus friendly display labels for the UI.
/// </summary>
public static class NotificationCategories
{
    public const string PositionChange = "PositionChange";
    public const string BulletinAward = "BulletinAward";
    public const string BulletinCancellation = "BulletinCancellation";
    public const string BulletinNoBid = "BulletinNoBid";
    public const string SeniorityMove = "SeniorityMove";
    public const string ForceAssign = "ForceAssign";
    public const string SafetyBulletin = "SafetyBulletin";
    public const string GeneralInformation = "GeneralInformation";
    public const string WorkAreaChange = "WorkAreaChange";
    public const string TieUp = "TieUp";

    private static readonly Dictionary<string, string> Labels = new(StringComparer.Ordinal)
    {
        [PositionChange] = "Position Change",
        [BulletinAward] = "Bulletin Award",
        [BulletinCancellation] = "Bulletin Cancellation",
        [BulletinNoBid] = "Bulletin No-Bid",
        [SeniorityMove] = "Seniority Move",
        [ForceAssign] = "Force Assignment",
        [SafetyBulletin] = "Safety Bulletin",
        [GeneralInformation] = "General Information",
        [WorkAreaChange] = "Work Area Change",
        [TieUp] = "Tie-Up",
    };

    /// <summary>Returns a human-friendly label for a category, falling back to the raw value.</summary>
    public static string Label(string category)
        => Labels.GetValueOrDefault(category, category);
}
