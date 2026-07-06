namespace CrewService.Domain.Modules.Notifications;

/// <summary>
/// String-backed categories for <see cref="EmployeeNotification"/>. Adding a new
/// category requires no schema change. Mirrors the existing Status/TemplateType style.
/// </summary>
public static class NotificationCategories
{
    public const string PositionChange = "PositionChange";
    public const string BulletinAward = "BulletinAward";
    public const string BulletinCancellation = "BulletinCancellation";
    public const string SeniorityMove = "SeniorityMove";
    public const string ForceAssign = "ForceAssign";
    public const string BoardPlacement = "BoardPlacement";
    public const string SafetyBulletin = "SafetyBulletin";
    public const string GeneralInformation = "GeneralInformation";
    public const string WorkAreaChange = "WorkAreaChange";
    public const string TieUp = "TieUp";
}
