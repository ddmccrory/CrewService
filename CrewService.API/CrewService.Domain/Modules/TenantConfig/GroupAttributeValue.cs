using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.TenantConfig;

public sealed class GroupAttributeValue : Entity
{
    public ControlNumber GroupCtrlNbr { get; private set; }
    public ControlNumber AttributeDefinitionCtrlNbr { get; private set; }
    public string? Value { get; private set; }

    private GroupAttributeValue()
    {
        GroupCtrlNbr = null!;
        AttributeDefinitionCtrlNbr = null!;
    }

    private GroupAttributeValue(
        ControlNumber groupCtrlNbr,
        ControlNumber attributeDefinitionCtrlNbr,
        string? value)
    {
        GroupCtrlNbr = groupCtrlNbr;
        AttributeDefinitionCtrlNbr = attributeDefinitionCtrlNbr;
        Value = value;
    }

    public static GroupAttributeValue Create(
        long groupCtrlNbr,
        long attributeDefinitionCtrlNbr,
        string? value)
    {
        return new GroupAttributeValue(
            ControlNumber.Create(groupCtrlNbr),
            ControlNumber.Create(attributeDefinitionCtrlNbr),
            value);
    }

    public void Update(string? value)
    {
        Value = value;
    }
}
