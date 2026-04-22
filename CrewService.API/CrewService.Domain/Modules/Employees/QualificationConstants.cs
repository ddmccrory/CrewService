namespace CrewService.Domain.Modules.Employees;

public static class RequirementKinds
{
    public const string Manual = "Manual";
    public const string TimeFromEvent = "TimeFromEvent";
    public const string ActivityCount = "ActivityCount";
    public const string TimeInRole = "TimeInRole";
    public const string QualificationHeld = "QualificationHeld";
    public const string FraCertificationHeld = "FraCertificationHeld";
}

public static class ThresholdUnits
{
    public const string Count = "Count";
    public const string Days = "Days";
    public const string Months = "Months";
}

public static class EventSources
{
    public const string EmploymentDate = "EmploymentDate";
    public const string SeniorityDate = "SeniorityDate";
    public const string CertificationDate = "CertificationDate";
}

public static class ActivityFilters
{
    public const string Any = "Any";
    public const string AssignedOnly = "AssignedOnly";
    public const string CalledFromBoard = "CalledFromBoard";
}

public static class EvaluationStrategies
{
    public const string Manual = "Manual";
    public const string TimeFromEvent = "TimeFromEvent";
    public const string ActivityCount = "ActivityCount";
    public const string TimeInRole = "TimeInRole";
    public const string QualificationHeld = "QualificationHeld";
    public const string FraCertification = "FraCertification";
}

public static class EvidenceTypes
{
    public const string ManualCompletion = "ManualCompletion";
    public const string TimeThresholdMet = "TimeThresholdMet";
    public const string ActivityCountMet = "ActivityCountMet";
    public const string QualificationHeld = "QualificationHeld";
    public const string FraCertificationHeld = "FraCertificationHeld";
    public const string CertificationHeld = "CertificationHeld";
}

public static class QualificationStatuses
{
    public const string Active = "Active";
    public const string Pending = "Pending";
    public const string Revoked = "Revoked";
    public const string Expired = "Expired";
}

public static class SystemActors
{
    public const string System = "SYSTEM";
}
