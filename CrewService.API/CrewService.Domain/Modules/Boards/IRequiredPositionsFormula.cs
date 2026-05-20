using System.Text.Json;

namespace CrewService.Domain.Modules.Boards;

/// <summary>
/// Computes the required number of Extra Board positions given historical vacancy data
/// and strategy-specific parameters. Implement this interface and register it in DI to
/// add a new formula type — no schema changes required.
/// </summary>
public interface IRequiredPositionsFormula
{
    /// <summary>
    /// Matches <see cref="RequiredPositionsStrategy.FormulaType"/>.
    /// e.g. "Static", "AnnualizedAverage".
    /// </summary>
    string FormulaType { get; }

    /// <summary>Human-readable label shown in the admin UI.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Default JSON parameters for this formula type.
    /// Used to pre-populate the parameters field when creating a new strategy.
    /// </summary>
    string ParametersTemplate { get; }

    /// <summary>
    /// Short description of each parameter, shown as help text in the admin UI.
    /// </summary>
    string ParametersDescription { get; }

    /// <summary>
    /// Calculate the required positions count.
    /// </summary>
    /// <param name="averageDailyVacancies">
    /// Rolling 30-day average of Board-type vacancies per day for the craft/work-area.
    /// Zero for Static strategies.
    /// </param>
    /// <param name="parameters">
    /// Deserialized <see cref="RequiredPositionsStrategy.ParametersJson"/>.
    /// </param>
    int Calculate(double averageDailyVacancies, JsonElement parameters);
}

/// <summary>
/// Resolves the correct <see cref="IRequiredPositionsFormula"/> implementation for a given
/// <see cref="RequiredPositionsStrategy.FormulaType"/> string.
/// </summary>
public interface IRequiredPositionsFormulaRegistry
{
    IRequiredPositionsFormula GetFormula(string formulaType);
}
