using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Notifications;

/// <summary>
/// Optional polymorphic subject of an <see cref="EmployeeNotification"/>. Replaces the
/// legacy required RailroadPositionControlNumber and the ChangeMoveOrBulletin /
/// RailroadInformationRecord junction tables with a single owned value object.
/// </summary>
public sealed record NotificationSubject
{
    public string SubjectType { get; private set; }
    public ControlNumber SubjectCtrlNbr { get; private set; }

    private NotificationSubject(string subjectType, ControlNumber subjectCtrlNbr)
    {
        SubjectType = subjectType;
        SubjectCtrlNbr = subjectCtrlNbr;
    }

    public static NotificationSubject Create(string subjectType, ControlNumber subjectCtrlNbr)
    {
        if (string.IsNullOrWhiteSpace(subjectType))
            throw new ArgumentException("Subject type is required.", nameof(subjectType));

        return new NotificationSubject(subjectType, subjectCtrlNbr);
    }
}

/// <summary>
/// Well-known <see cref="NotificationSubject.SubjectType"/> values.
/// </summary>
public static class NotificationSubjectTypes
{
    public const string StaffablePosition = "StaffablePosition";
    public const string Bulletin = "Bulletin";
    public const string SeniorityMove = "SeniorityMove";
    public const string InformationRecord = "InformationRecord";
    public const string WorkAreaGroup = "WorkAreaGroup";
    public const string RosterBoard = "RosterBoard";
}
