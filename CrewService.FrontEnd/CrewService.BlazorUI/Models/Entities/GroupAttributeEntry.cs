namespace CrewService.BlazorUI.Models.Entities;

/// <summary>
/// Pairs a group attribute definition with its current value for a specific group.
/// Used in group create/edit workflows and anywhere attribute values are displayed or processed.
/// </summary>
public sealed class GroupAttributeEntry
{
    public long AttributeDefinitionCtrlNbr { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public bool IsRequired { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
