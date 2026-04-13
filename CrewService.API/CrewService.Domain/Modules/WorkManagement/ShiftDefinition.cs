using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class ShiftDefinition : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public string ShiftCode { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    private ShiftDefinition()
    {
        WorkAreaGroupCtrlNbr = null!;
    }

    public static ShiftDefinition Create(
        ControlNumber workAreaGroupCtrlNbr,
        string shiftCode,
        string displayName,
        int displayOrder,
        bool isActive)
    {
        var def = new ShiftDefinition
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            ShiftCode = shiftCode,
            DisplayName = displayName,
            DisplayOrder = displayOrder,
            IsActive = isActive
        };
        def.Raise(new ShiftDefinitionCreatedDomainEvent(def));
        return def;
    }

    public void Update(
        string? shiftCode = null,
        string? displayName = null,
        int? displayOrder = null,
        bool? isActive = null)
    {
        if (shiftCode is not null) ShiftCode = shiftCode;
        if (displayName is not null) DisplayName = displayName;
        if (displayOrder.HasValue) DisplayOrder = displayOrder.Value;
        if (isActive.HasValue) IsActive = isActive.Value;
    }
}

public sealed record ShiftDefinitionCreatedDomainEvent : DomainEvent
{
    public ShiftDefinitionCreatedDomainEvent(ShiftDefinition d)
        : base(nameof(ShiftDefinition), d.CtrlNbr.Value, new { d.ShiftCode, d.DisplayName, WorkAreaGroupCtrlNbr = d.WorkAreaGroupCtrlNbr.Value }) { }
}