using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.TenantConfig;

public sealed class GroupAttributeDefinition : Entity
{
    public ControlNumber GroupTypeCtrlNbr { get; private set; }
    public string AttributeName { get; private set; } = string.Empty;
    public string DataType { get; private set; } = string.Empty;
    public bool IsRequired { get; private set; }
    public string? DefaultValue { get; private set; }

    private GroupAttributeDefinition()
    {
        GroupTypeCtrlNbr = null!;
    }

    private GroupAttributeDefinition(
        ControlNumber groupTypeCtrlNbr,
        string attributeName,
        string dataType,
        bool isRequired,
        string? defaultValue)
    {
        GroupTypeCtrlNbr = groupTypeCtrlNbr;
        AttributeName = attributeName;
        DataType = dataType;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
    }

    public static GroupAttributeDefinition Create(
        long groupTypeCtrlNbr,
        string attributeName,
        string dataType,
        bool isRequired,
        string? defaultValue = null)
    {
        return new GroupAttributeDefinition(
            ControlNumber.Create(groupTypeCtrlNbr),
            attributeName,
            dataType,
            isRequired,
            defaultValue);
    }

    public void Update(string attributeName, string dataType, bool isRequired, string? defaultValue)
    {
        AttributeName = attributeName;
        DataType = dataType;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
    }
}
