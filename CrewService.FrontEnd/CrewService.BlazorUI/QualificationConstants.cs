namespace CrewService.BlazorUI;

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
