using CrewService.Domain.DomainEvents.Qualifications;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Employees;

public sealed class QualificationType : Entity
{
    private readonly List<QualificationPrerequisite> _prerequisites = [];

    public ControlNumber ParentCtrlNbr { get; private set; }
    public ControlNumber? ScopeGroupCtrlNbr { get; private set; }
    public ControlNumber? CraftCtrlNbr { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string EvaluationStrategy { get; private set; } = "Manual";
    public int? ExpirationMonths { get; private set; }
    public bool CalendarYearExpiry { get; private set; }
    public int GraceDays { get; private set; }
    public int RenewalLeadDays { get; private set; }
    public bool IsBlocking { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<QualificationPrerequisite> Prerequisites => _prerequisites.AsReadOnly();

    private QualificationType()
    {
        ParentCtrlNbr = null!;
    }

    public static QualificationType Create(
        ControlNumber parentCtrlNbr,
        string code,
        string name,
        string evaluationStrategy = "Manual",
        ControlNumber? scopeGroupCtrlNbr = null,
        ControlNumber? craftCtrlNbr = null,
        string? description = null,
        int? expirationMonths = null,
        bool calendarYearExpiry = false,
        int graceDays = 0,
        int renewalLeadDays = 0,
        bool isBlocking = false)
    {
        var qt = new QualificationType
        {
            ParentCtrlNbr = parentCtrlNbr,
            Code = code.ToUpperInvariant(),
            Name = name,
            EvaluationStrategy = evaluationStrategy,
            ScopeGroupCtrlNbr = scopeGroupCtrlNbr,
            CraftCtrlNbr = craftCtrlNbr,
            Description = description,
            ExpirationMonths = expirationMonths,
            CalendarYearExpiry = calendarYearExpiry,
            GraceDays = graceDays,
            RenewalLeadDays = renewalLeadDays,
            IsBlocking = isBlocking,
            IsActive = true
        };

        qt.Raise(new QualificationTypeCreatedDomainEvent(qt));
        return qt;
    }

    public void Update(
        string name,
        string? description,
        ControlNumber? scopeGroupCtrlNbr,
        ControlNumber? craftCtrlNbr,
        int? expirationMonths,
        bool calendarYearExpiry,
        int graceDays,
        int renewalLeadDays,
        bool isBlocking)
    {
        Name = name;
        Description = description;
        ScopeGroupCtrlNbr = scopeGroupCtrlNbr;
        CraftCtrlNbr = craftCtrlNbr;
        ExpirationMonths = expirationMonths;
        CalendarYearExpiry = calendarYearExpiry;
        GraceDays = graceDays;
        RenewalLeadDays = renewalLeadDays;
        IsBlocking = isBlocking;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public QualificationPrerequisite AddPrerequisite(
        string prerequisiteKind,
        int threshold,
        string thresholdUnit,
        string description,
        string? eventSource = null,
        string? activityFilter = null,
        ControlNumber? requiredQualTypeCtrlNbr = null)
    {
        var prerequisite = QualificationPrerequisite.Create(
            CtrlNbr,
            prerequisiteKind,
            threshold,
            thresholdUnit,
            description,
            eventSource,
            activityFilter,
            requiredQualTypeCtrlNbr);

        _prerequisites.Add(prerequisite);
        return prerequisite;
    }

    public void RemovePrerequisite(ControlNumber prerequisiteCtrlNbr)
    {
        var prerequisite = _prerequisites.Find(p => p.CtrlNbr == prerequisiteCtrlNbr);
        if (prerequisite is not null)
            _prerequisites.Remove(prerequisite);
    }
}
