using System.Text.Json;
using CrewService.Domain.Modules.Boards;

namespace CrewService.Application.Boards;

/// <summary>
/// Formula type discriminator constants. Add a new constant here and a matching
/// <see cref="IRequiredPositionsFormula"/> implementation to extend the system.
/// </summary>
public static class FormulaTypes
{
    public const string Static = "Static";
    public const string AnnualizedAverage = "AnnualizedAverage";
}

/// <summary>
/// Static (manual) strategy — always returns the board's currently persisted
/// <c>RequiredPositions</c> value unchanged. The Calculate method is a no-op;
/// the application service skips recalculation entirely for this formula type.
/// </summary>
public sealed class StaticFormula : IRequiredPositionsFormula
{
    public string FormulaType           => FormulaTypes.Static;
    public string DisplayName           => "Static";
    public string ParametersTemplate    => """{"count":1}""";
    public string ParametersDescription => "count: fixed number of required extra board positions.";

    public int Calculate(double averageDailyVacancies, JsonElement parameters) => 0;
}

/// <summary>
/// Legacy annualized average formula (matches PTRA legacy system):
///   RequiredPositions = ceiling((avgDailyVacancies × daysPerYear) / payPeriodsPerYear / daysPerPayPeriod)
///
/// Default parameters: daysPerYear=365, payPeriodsPerYear=24 (bi-monthly), daysPerPayPeriod=12.
/// Railroads with different pay structures override these via the craft assignment's ParametersJson.
/// </summary>
public sealed class AnnualizedAverageFormula : IRequiredPositionsFormula
{
    public string FormulaType           => FormulaTypes.AnnualizedAverage;
    public string DisplayName           => "Annualized Average";
    public string ParametersTemplate    => """{"daysPerYear":365,"payPeriodsPerYear":24,"daysPerPayPeriod":12}""";
    public string ParametersDescription => "daysPerYear: operating days per year (default 365). payPeriodsPerYear: number of pay periods per year (default 24 = bi-monthly). daysPerPayPeriod: working days in one pay period (default 12). Formula: ceiling((avgDailyVacancies × daysPerYear) / payPeriodsPerYear / daysPerPayPeriod).";

    public int Calculate(double averageDailyVacancies, JsonElement parameters)
    {
        var daysPerYear       = GetDouble(parameters, "daysPerYear",       365);
        var payPeriodsPerYear = GetDouble(parameters, "payPeriodsPerYear", 24);
        var daysPerPayPeriod  = GetDouble(parameters, "daysPerPayPeriod",  12);

        if (payPeriodsPerYear <= 0 || daysPerPayPeriod <= 0)
            return 0;

        var result = (averageDailyVacancies * daysPerYear) / payPeriodsPerYear / daysPerPayPeriod;
        return (int)Math.Ceiling(result);
    }

    private static double GetDouble(JsonElement el, string property, double defaultValue)
    {
        if (el.ValueKind == JsonValueKind.Object &&
            el.TryGetProperty(property, out var prop) &&
            prop.TryGetDouble(out var value))
            return value;
        return defaultValue;
    }
}

/// <summary>
/// Resolves <see cref="IRequiredPositionsFormula"/> implementations by
/// <see cref="RequiredPositionsStrategy.FormulaType"/> discriminator.
/// Register new formula implementations in DI to make them available.
/// </summary>
public sealed class RequiredPositionsFormulaRegistry : IRequiredPositionsFormulaRegistry
{
    private readonly IReadOnlyDictionary<string, IRequiredPositionsFormula> _formulas;

    public RequiredPositionsFormulaRegistry(IEnumerable<IRequiredPositionsFormula> formulas)
    {
        _formulas = formulas.ToDictionary(f => f.FormulaType, StringComparer.OrdinalIgnoreCase);
    }

    public IRequiredPositionsFormula GetFormula(string formulaType)
    {
        if (_formulas.TryGetValue(formulaType, out var formula))
            return formula;

        throw new InvalidOperationException(
            $"No IRequiredPositionsFormula registered for FormulaType '{formulaType}'. " +
            $"Registered types: {string.Join(", ", _formulas.Keys)}");
    }
}
