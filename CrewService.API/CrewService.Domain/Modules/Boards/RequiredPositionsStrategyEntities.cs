using System.Text.Json;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Boards;

/// <summary>
/// Defines how the required number of Extra Board positions is calculated for crafts
/// that reference this strategy. Strategies are system-wide; railroads customize
/// behavior by supplying parameter overrides on the craft assignment.
/// </summary>
public sealed class RequiredPositionsStrategy : Entity
{
    /// <summary>Short machine-readable code, unique system-wide.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Discriminator that maps to a registered <see cref="IRequiredPositionsFormula"/> implementation.
    /// e.g. "Static", "AnnualizedAverage".
    /// </summary>
    public string FormulaType { get; private set; } = string.Empty;

    /// <summary>
    /// Default JSON parameters for this formula. Railroads may override these
    /// per-craft via <see cref="CraftRequiredPositionsStrategy.ParametersJson"/>.
    /// </summary>
    public string ParametersJson { get; private set; } = "{}";

    private RequiredPositionsStrategy() { }

    public static RequiredPositionsStrategy Create(
        string code,
        string name,
        string description,
        string formulaType,
        string parametersJson = "{}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(formulaType);

        return new RequiredPositionsStrategy
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = description.Trim(),
            FormulaType = formulaType.Trim(),
            ParametersJson = string.IsNullOrWhiteSpace(parametersJson) ? "{}" : parametersJson
        };
    }

    public void Update(string name, string description, string formulaType, string parametersJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(formulaType);

        Name = name.Trim();
        Description = description.Trim();
        FormulaType = formulaType.Trim();
        ParametersJson = string.IsNullOrWhiteSpace(parametersJson) ? "{}" : parametersJson;
    }

    public JsonElement GetParameters() =>
        JsonSerializer.Deserialize<JsonElement>(ParametersJson);
}

/// <summary>
/// Assigns a <see cref="RequiredPositionsStrategy"/> to a craft for a specific railroad.
/// The optional <see cref="ParametersJson"/> overrides the strategy's default parameters
/// for this railroad. When null, the strategy's own parameters are used.
/// </summary>
public sealed class CraftRequiredPositionsStrategy : Entity
{
    public ControlNumber CraftCtrlNbr { get; private set; } = null!;
    public ControlNumber StrategyCtrlNbr { get; private set; } = null!;

    /// <summary>
    /// Railroad-specific parameter overrides. Null means use the strategy's default parameters.
    /// </summary>
    public string? ParametersJson { get; private set; }

    private CraftRequiredPositionsStrategy() { }

    public static CraftRequiredPositionsStrategy Create(
        ControlNumber craftCtrlNbr,
        ControlNumber strategyCtrlNbr,
        string? parametersJson = null)
    {
        return new CraftRequiredPositionsStrategy
        {
            CraftCtrlNbr    = craftCtrlNbr,
            StrategyCtrlNbr = strategyCtrlNbr,
            ParametersJson  = string.IsNullOrWhiteSpace(parametersJson) ? null : parametersJson
        };
    }

    public void Reassign(ControlNumber newStrategyCtrlNbr, string? parametersJson = null)
    {
        StrategyCtrlNbr = newStrategyCtrlNbr;
        ParametersJson  = string.IsNullOrWhiteSpace(parametersJson) ? null : parametersJson;
    }

    public void UpdateParameters(string? parametersJson)
    {
        ParametersJson = string.IsNullOrWhiteSpace(parametersJson) ? null : parametersJson;
    }

    /// <summary>
    /// Returns the effective parameters JSON: the override if set, otherwise the strategy default.
    /// </summary>
    public string GetEffectiveParameters(RequiredPositionsStrategy strategy) =>
        ParametersJson ?? strategy.ParametersJson;
}
