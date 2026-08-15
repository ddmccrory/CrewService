using CrewService.Domain.DomainEvents.Workflows;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Workflows;

public sealed class WorkflowTemplate : Entity
{
    public ControlNumber RailroadCtrlNbr { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ControlNumber? TriggerTypeCtrlNbr { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    private WorkflowTemplate()
    {
        RailroadCtrlNbr = null!;
    }

    public static WorkflowTemplate Create(ControlNumber railroadCtrlNbr, string name, ControlNumber? triggerTypeCtrlNbr, bool isEnabled = true)
    {
        var template = new WorkflowTemplate
        {
            RailroadCtrlNbr = railroadCtrlNbr,
            Name = name,
            TriggerTypeCtrlNbr = triggerTypeCtrlNbr,
            IsEnabled = isEnabled
        };

        template.Raise(new WorkflowTemplateCreatedDomainEvent(
            template.CtrlNbr,
            payload: new
            {
                RailroadCtrlNbr = template.RailroadCtrlNbr.Value,
                template.Name,
                TriggerTypeCtrlNbr = template.TriggerTypeCtrlNbr?.Value,
                template.IsEnabled
            }));

        return template;
    }

    public void UpdateDefinition(string name, ControlNumber? triggerTypeCtrlNbr, bool isEnabled)
    {
        var previousName = Name;
        var previousTriggerTypeCtrlNbr = TriggerTypeCtrlNbr;
        var previousIsEnabled = IsEnabled;

        Name = name;
        TriggerTypeCtrlNbr = triggerTypeCtrlNbr;
        IsEnabled = isEnabled;

        if (!string.Equals(previousName, Name, StringComparison.Ordinal)
            || previousTriggerTypeCtrlNbr != TriggerTypeCtrlNbr
            || previousIsEnabled != IsEnabled)
        {
            Raise(new WorkflowTemplateUpdatedDomainEvent(
                CtrlNbr,
                payload: new
                {
                    Previous = new
                    {
                        Name = previousName,
                        TriggerTypeCtrlNbr = previousTriggerTypeCtrlNbr?.Value,
                        IsEnabled = previousIsEnabled
                    },
                    Current = new
                    {
                        Name,
                        TriggerTypeCtrlNbr = TriggerTypeCtrlNbr?.Value,
                        IsEnabled
                    }
                }));
        }
    }
}

public sealed class WorkflowVersion : Entity
{
    public ControlNumber WorkflowTemplateCtrlNbr { get; private set; }
    public int VersionNumber { get; private set; }
    public string Status { get; private set; } = WorkflowVersionStatus.Draft;
    public string DefinitionJson { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public DateTime SavedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; private set; }

    private WorkflowVersion()
    {
        WorkflowTemplateCtrlNbr = null!;
    }

    public static WorkflowVersion Create(ControlNumber workflowTemplateCtrlNbr, int versionNumber, string definitionJson, string notes, string status)
    {
        var version = new WorkflowVersion
        {
            WorkflowTemplateCtrlNbr = workflowTemplateCtrlNbr,
            VersionNumber = versionNumber,
            DefinitionJson = definitionJson,
            Notes = notes,
            Status = status,
            SavedAtUtc = DateTime.UtcNow,
            PublishedAtUtc = status == WorkflowVersionStatus.Published ? DateTime.UtcNow : null
        };

        version.Raise(new WorkflowVersionCreatedDomainEvent(
            version.CtrlNbr,
            payload: new
            {
                WorkflowTemplateCtrlNbr = version.WorkflowTemplateCtrlNbr.Value,
                version.VersionNumber,
                version.Status,
                version.Notes,
                version.SavedAtUtc,
                version.PublishedAtUtc
            }));

        return version;
    }

    public void SaveDraft(string definitionJson, string notes)
    {
        DefinitionJson = definitionJson;
        Notes = notes;
        Status = WorkflowVersionStatus.Draft;
        SavedAtUtc = DateTime.UtcNow;
        PublishedAtUtc = null;
    }

    public void Publish()
    {
        if (Status == WorkflowVersionStatus.Published)
            return;

        Status = WorkflowVersionStatus.Published;
        PublishedAtUtc = DateTime.UtcNow;

        Raise(new WorkflowVersionPublishedDomainEvent(
            CtrlNbr,
            payload: new
            {
                WorkflowTemplateCtrlNbr = WorkflowTemplateCtrlNbr.Value,
                VersionNumber,
                Status,
                PublishedAtUtc
            }));
    }
}

public sealed class WorkflowExecutionHistory : Entity
{
    public ControlNumber WorkflowTemplateCtrlNbr { get; private set; } = null!;
    public ControlNumber WorkflowVersionCtrlNbr { get; private set; } = null!;
    public int WorkflowVersionNumber { get; private set; }
    public ControlNumber RailroadCtrlNbr { get; private set; } = null!;
    public ControlNumber? TriggerTypeCtrlNbr { get; private set; }
    public ControlNumber? AggregateCtrlNbr { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime StartedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; private set; }
    public string Status { get; private set; } = WorkflowExecutionStatus.Running;
    public string? DetailsJson { get; private set; }

    private WorkflowExecutionHistory() { }

    public static WorkflowExecutionHistory Start(
        ControlNumber workflowTemplateCtrlNbr,
        ControlNumber workflowVersionCtrlNbr,
        int workflowVersionNumber,
        ControlNumber railroadCtrlNbr,
        ControlNumber? triggerTypeCtrlNbr,
        ControlNumber? aggregateCtrlNbr,
        string? correlationId)
    {
        return new WorkflowExecutionHistory
        {
            WorkflowTemplateCtrlNbr = workflowTemplateCtrlNbr,
            WorkflowVersionCtrlNbr = workflowVersionCtrlNbr,
            WorkflowVersionNumber = workflowVersionNumber,
            RailroadCtrlNbr = railroadCtrlNbr,
            TriggerTypeCtrlNbr = triggerTypeCtrlNbr,
            AggregateCtrlNbr = aggregateCtrlNbr,
            CorrelationId = correlationId,
            StartedAtUtc = DateTime.UtcNow,
            Status = WorkflowExecutionStatus.Running
        };
    }

    public void Complete(string status, string? detailsJson)
    {
        Status = status;
        DetailsJson = detailsJson;
        CompletedAtUtc = DateTime.UtcNow;
    }
}

public sealed class WorkflowTriggerType : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private WorkflowTriggerType() { }

    public static WorkflowTriggerType Create(string code, string name)
    {
        return new WorkflowTriggerType
        {
            Code = code.Trim(),
            Name = name.Trim(),
            IsActive = true
        };
    }

    public void Update(string code, string name, bool isActive)
    {
        Code = code.Trim();
        Name = name.Trim();
        IsActive = isActive;
    }
}

public sealed class WorkflowEffectType : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private WorkflowEffectType() { }

    public static WorkflowEffectType Create(string code, string name)
    {
        return new WorkflowEffectType
        {
            Code = code.Trim(),
            Name = name.Trim(),
            IsActive = true
        };
    }

    public void Update(string code, string name, bool isActive)
    {
        Code = code.Trim();
        Name = name.Trim();
        IsActive = isActive;
    }
}

public sealed class WorkflowOperatorType : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private WorkflowOperatorType() { }

    public static WorkflowOperatorType Create(string code, string name)
    {
        return new WorkflowOperatorType
        {
            Code = code.Trim(),
            Name = name.Trim(),
            IsActive = true
        };
    }

    public void Update(string code, string name, bool isActive)
    {
        Code = code.Trim();
        Name = name.Trim();
        IsActive = isActive;
    }
}

public sealed class WorkflowMetadataFieldType : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private WorkflowMetadataFieldType() { }

    public static WorkflowMetadataFieldType Create(string code, string name)
    {
        return new WorkflowMetadataFieldType
        {
            Code = code.Trim(),
            Name = name.Trim(),
            IsActive = true
        };
    }

    public void Update(string code, string name, bool isActive)
    {
        Code = code.Trim();
        Name = name.Trim();
        IsActive = isActive;
    }
}

public static class WorkflowVersionStatus
{
    public const string Draft = "Draft";
    public const string Published = "Published";
}

public static class WorkflowExecutionStatus
{
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Skipped = "Skipped";
}

public sealed record WorkflowDefinition(
    ControlNumber? TriggerTypeCtrlNbr,
    string TriggerConditionGroupOperator,
    List<WorkflowConditionDefinition> TriggerConditions,
    List<WorkflowStepDefinition> Steps);

public sealed record WorkflowStepDefinition(
    ControlNumber CtrlNbr,
    int Order,
    string Name,
    bool IsEnabled,
    string FailurePolicy,
    string ConditionGroupOperator,
    List<WorkflowConditionDefinition> Conditions,
    List<WorkflowEffectDefinition> Effects);

public sealed record WorkflowConditionDefinition(
    ControlNumber CtrlNbr,
    ControlNumber FieldTypeCtrlNbr,
    ControlNumber OperatorTypeCtrlNbr,
    string Value);

public sealed record WorkflowEffectDefinition(
    ControlNumber CtrlNbr,
    int Order,
    bool IsEnabled,
    ControlNumber EffectTypeCtrlNbr,
    Dictionary<string, string> Options);
