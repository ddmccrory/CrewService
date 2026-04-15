using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Employees;

public sealed class QualificationPrerequisite : Entity
{
    public ControlNumber QualificationTypeCtrlNbr { get; private set; }
    public string PrerequisiteKind { get; private set; } = string.Empty;
    public int Threshold { get; private set; }
    public string ThresholdUnit { get; private set; } = string.Empty;
    public string? EventSource { get; private set; }
    public string? ActivityFilter { get; private set; }
    public ControlNumber? RequiredQualTypeCtrlNbr { get; private set; }
    public string Description { get; private set; } = string.Empty;

    private QualificationPrerequisite()
    {
        QualificationTypeCtrlNbr = null!;
    }

    internal static QualificationPrerequisite Create(
        ControlNumber qualificationTypeCtrlNbr,
        string prerequisiteKind,
        int threshold,
        string thresholdUnit,
        string description,
        string? eventSource = null,
        string? activityFilter = null,
        ControlNumber? requiredQualTypeCtrlNbr = null)
    {
        return new QualificationPrerequisite
        {
            QualificationTypeCtrlNbr = qualificationTypeCtrlNbr,
            PrerequisiteKind = prerequisiteKind,
            Threshold = threshold,
            ThresholdUnit = thresholdUnit,
            Description = description,
            EventSource = eventSource,
            ActivityFilter = activityFilter,
            RequiredQualTypeCtrlNbr = requiredQualTypeCtrlNbr
        };
    }

    public void Update(
        int threshold,
        string thresholdUnit,
        string description,
        string? eventSource,
        string? activityFilter,
        ControlNumber? requiredQualTypeCtrlNbr)
    {
        Threshold = threshold;
        ThresholdUnit = thresholdUnit;
        Description = description;
        EventSource = eventSource;
        ActivityFilter = activityFilter;
        RequiredQualTypeCtrlNbr = requiredQualTypeCtrlNbr;
    }
}
