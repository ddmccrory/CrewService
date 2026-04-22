using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Employees;

public sealed class QualificationRequirement : Entity
{
    public ControlNumber QualificationTypeCtrlNbr { get; private set; }
    public string RequirementKind { get; private set; } = string.Empty;
    public int Threshold { get; private set; }
    public string ThresholdUnit { get; private set; } = string.Empty;
    public string? EventSource { get; private set; }
    public string? ActivityFilter { get; private set; }
    public ControlNumber? RequiredQualTypeCtrlNbr { get; private set; }
    public ControlNumber? RequiredRegulatoryQualCtrlNbr { get; private set; }
    public string Description { get; private set; } = string.Empty;

    private QualificationRequirement()
    {
        QualificationTypeCtrlNbr = null!;
    }

    internal static QualificationRequirement Create(
        ControlNumber qualificationTypeCtrlNbr,
        string requirementKind,
        int threshold,
        string thresholdUnit,
        string description,
        string? eventSource = null,
        string? activityFilter = null,
        ControlNumber? requiredQualTypeCtrlNbr = null,
        ControlNumber? requiredRegulatoryQualCtrlNbr = null)
    {
        return new QualificationRequirement
        {
            QualificationTypeCtrlNbr = qualificationTypeCtrlNbr,
            RequirementKind = requirementKind,
            Threshold = threshold,
            ThresholdUnit = requirementKind == RequirementKinds.ActivityCount ? ThresholdUnits.Count : thresholdUnit,
            Description = description,
            EventSource = eventSource,
            ActivityFilter = activityFilter,
            RequiredQualTypeCtrlNbr = requiredQualTypeCtrlNbr,
            RequiredRegulatoryQualCtrlNbr = requiredRegulatoryQualCtrlNbr
        };
    }

    public void Update(
        int threshold,
        string thresholdUnit,
        string description,
        string? eventSource,
        string? activityFilter,
        ControlNumber? requiredQualTypeCtrlNbr,
        ControlNumber? requiredRegulatoryQualCtrlNbr)
    {
        Threshold = threshold;
        ThresholdUnit = RequirementKind == RequirementKinds.ActivityCount ? ThresholdUnits.Count : thresholdUnit;
        Description = description;
        EventSource = eventSource;
        ActivityFilter = activityFilter;
        RequiredQualTypeCtrlNbr = requiredQualTypeCtrlNbr;
        RequiredRegulatoryQualCtrlNbr = requiredRegulatoryQualCtrlNbr;
    }
}
