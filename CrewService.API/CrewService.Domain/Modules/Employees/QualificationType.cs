using CrewService.Domain.DomainEvents.Qualifications;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Employees;

public sealed class QualificationType : Entity
{
    private readonly List<QualificationRequirement> _requirements = [];

    public ControlNumber ParentCtrlNbr { get; private set; }
    public ControlNumber? ScopeGroupCtrlNbr { get; private set; }
    public ControlNumber? CraftCtrlNbr { get; private set; }
    public ControlNumber? RegulatoryQualificationCtrlNbr { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string EvaluationStrategy { get; private set; } = EvaluationStrategies.Manual;
    public int? ExpirationMonths { get; private set; }
    public bool CalendarYearExpiry { get; private set; }
    public int GraceDays { get; private set; }
    public int RenewalLeadDays { get; private set; }
    public bool IsBlocking { get; private set; }
    public bool IsSystemSeeded { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? RestrictionLabel { get; private set; }

    public IReadOnlyList<QualificationRequirement> Requirements => _requirements.AsReadOnly();

    private QualificationType()
    {
        ParentCtrlNbr = null!;
    }

    public static QualificationType Create(
        ControlNumber parentCtrlNbr,
        string code,
        string name,
        string evaluationStrategy = EvaluationStrategies.Manual,
        ControlNumber? scopeGroupCtrlNbr = null,
        ControlNumber? craftCtrlNbr = null,
        ControlNumber? regulatoryQualificationCtrlNbr = null,
        string? description = null,
        int? expirationMonths = null,
        bool calendarYearExpiry = false,
        int graceDays = 0,
        int renewalLeadDays = 0,
        bool isBlocking = false,
        bool isSystemSeeded = false,
        string? restrictionLabel = null)
    {
        var qt = new QualificationType
        {
            ParentCtrlNbr = parentCtrlNbr,
            Code = code.ToUpperInvariant(),
            Name = name,
            EvaluationStrategy = evaluationStrategy,
            ScopeGroupCtrlNbr = scopeGroupCtrlNbr,
            CraftCtrlNbr = craftCtrlNbr,
            RegulatoryQualificationCtrlNbr = regulatoryQualificationCtrlNbr,
            Description = description,
            ExpirationMonths = expirationMonths,
            CalendarYearExpiry = calendarYearExpiry,
            GraceDays = graceDays,
            RenewalLeadDays = renewalLeadDays,
            IsBlocking = isBlocking,
            IsSystemSeeded = isSystemSeeded,
            IsActive = true,
            RestrictionLabel = restrictionLabel
        };

        qt.Raise(new QualificationTypeCreatedDomainEvent(qt));
        return qt;
    }

    public void Update(
        string name,
        string? description,
        string evaluationStrategy,
        ControlNumber? scopeGroupCtrlNbr,
        ControlNumber? craftCtrlNbr,
        int? expirationMonths,
        bool calendarYearExpiry,
        int graceDays,
        int renewalLeadDays,
        bool isBlocking,
        string? restrictionLabel = null)
    {
        Name = name;
        Description = description;
        EvaluationStrategy = evaluationStrategy;
        ScopeGroupCtrlNbr = scopeGroupCtrlNbr;
        CraftCtrlNbr = craftCtrlNbr;
        ExpirationMonths = expirationMonths;
        CalendarYearExpiry = calendarYearExpiry;
        GraceDays = graceDays;
        RenewalLeadDays = renewalLeadDays;
        IsBlocking = isBlocking;
        RestrictionLabel = restrictionLabel;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public QualificationRequirement AddRequirement(
        string requirementKind,
        int threshold,
        string thresholdUnit,
        string description,
        string? eventSource = null,
        string? activityFilter = null,
        ControlNumber? requiredQualTypeCtrlNbr = null,
        ControlNumber? requiredRegulatoryQualCtrlNbr = null)
    {
        var prerequisite = QualificationRequirement.Create(
            CtrlNbr,
            requirementKind,
            threshold,
            thresholdUnit,
            description,
            eventSource,
            activityFilter,
            requiredQualTypeCtrlNbr,
            requiredRegulatoryQualCtrlNbr);

        _requirements.Add(prerequisite);
        return prerequisite;
    }

    public void RemoveRequirement(ControlNumber requirementCtrlNbr)
    {
        var prerequisite = _requirements.Find(p => p.CtrlNbr == requirementCtrlNbr);
        if (prerequisite is not null)
            _requirements.Remove(prerequisite);
    }
}
