using System.Text.Json;
using CrewService.Application.Boards;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Boards;

public class RequiredPositionsStrategyEntityTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var strategy = RequiredPositionsStrategy.Create(
            "STATIC", "Static", "Fixed count", "Static", "{}");

        Assert.Equal("STATIC", strategy.Code);
        Assert.Equal("Static", strategy.Name);
        Assert.Equal("Fixed count", strategy.Description);
        Assert.Equal("Static", strategy.FormulaType);
        Assert.Equal("{}", strategy.ParametersJson);
    }

    [Fact]
    public void Create_NormalizesCodeToUpperInvariant()
    {
        var strategy = RequiredPositionsStrategy.Create("annualized_avg", "Annualized", "", "AnnualizedAverage");

        Assert.Equal("ANNUALIZED_AVG", strategy.Code);
    }

    [Fact]
    public void Create_EmptyParametersJson_DefaultsToEmptyObject()
    {
        var strategy = RequiredPositionsStrategy.Create("S", "S", "", "Static", "   ");

        Assert.Equal("{}", strategy.ParametersJson);
    }

    [Fact]
    public void Create_NullOrWhitespaceCode_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RequiredPositionsStrategy.Create("", "Name", "", "Static"));
    }

    [Fact]
    public void Create_NullOrWhitespaceName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RequiredPositionsStrategy.Create("CODE", "", "", "Static"));
    }

    [Fact]
    public void Create_NullOrWhitespaceFormulaType_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RequiredPositionsStrategy.Create("CODE", "Name", "", ""));
    }

    [Fact]
    public void Update_ChangesProperties()
    {
        var strategy = RequiredPositionsStrategy.Create("CODE", "Old", "Old desc", "Static");

        strategy.Update("New Name", "New desc", "AnnualizedAverage", "{\"daysPerYear\":365}");

        Assert.Equal("New Name", strategy.Name);
        Assert.Equal("New desc", strategy.Description);
        Assert.Equal("AnnualizedAverage", strategy.FormulaType);
        Assert.Equal("{\"daysPerYear\":365}", strategy.ParametersJson);
    }

    [Fact]
    public void GetParameters_ReturnsValidJsonElement()
    {
        var strategy = RequiredPositionsStrategy.Create("S", "S", "", "Static", "{\"daysPerYear\":365}");

        var el = strategy.GetParameters();

        Assert.Equal(JsonValueKind.Object, el.ValueKind);
        Assert.True(el.TryGetProperty("daysPerYear", out var prop));
        Assert.Equal(365, prop.GetDouble());
    }
}

public class CraftRequiredPositionsStrategyTests
{
    [Fact]
    public void Create_SetsCraftAndStrategy()
    {
        var craftCtrlNbr = ControlNumber.Create(10);
        var strategyCtrlNbr = ControlNumber.Create(20);

        var assignment = CraftRequiredPositionsStrategy.Create(craftCtrlNbr, strategyCtrlNbr);

        Assert.Equal(10, assignment.CraftCtrlNbr.Value);
        Assert.Equal(20, assignment.StrategyCtrlNbr.Value);
    }

    [Fact]
    public void Reassign_UpdatesStrategyCtrlNbr()
    {
        var assignment = CraftRequiredPositionsStrategy.Create(
            ControlNumber.Create(10), ControlNumber.Create(20));

        assignment.Reassign(ControlNumber.Create(99));

        Assert.Equal(99, assignment.StrategyCtrlNbr.Value);
    }
}

public class AnnualizedAverageFormulaTests
{
    private readonly AnnualizedAverageFormula _sut = new();

    [Fact]
    public void FormulaType_IsAnnualizedAverage()
    {
        Assert.Equal(FormulaTypes.AnnualizedAverage, _sut.FormulaType);
    }

    [Fact]
    public void Calculate_DefaultParameters_UsesLegacyPtraValues()
    {
        // averageDailyVacancies=2, defaults: daysPerYear=365, payPeriodsPerYear=24, daysPerPayPeriod=12
        // (2 * 365) / 24 / 12 = 730/288 = 2.534... → ceiling = 3
        var parameters = JsonSerializer.Deserialize<JsonElement>("{}");

        var result = _sut.Calculate(2.0, parameters);

        Assert.Equal(3, result);
    }

    [Fact]
    public void Calculate_CustomParameters_OverrideDefaults()
    {
        // daysPerYear=100, payPeriodsPerYear=10, daysPerPayPeriod=10
        // (4 * 100) / 10 / 10 = 400/100 = 4.0 → ceiling = 4
        var parameters = JsonSerializer.Deserialize<JsonElement>(
            "{\"daysPerYear\":100,\"payPeriodsPerYear\":10,\"daysPerPayPeriod\":10}");

        var result = _sut.Calculate(4.0, parameters);

        Assert.Equal(4, result);
    }

    [Fact]
    public void Calculate_ZeroVacancies_ReturnsZero()
    {
        var parameters = JsonSerializer.Deserialize<JsonElement>("{}");

        var result = _sut.Calculate(0.0, parameters);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Calculate_ExactWholeNumber_NoCeilingBump()
    {
        // (24 * 365) / 24 / 365 = 1.0 exactly
        var parameters = JsonSerializer.Deserialize<JsonElement>(
            "{\"daysPerYear\":365,\"payPeriodsPerYear\":24,\"daysPerPayPeriod\":365}");

        var result = _sut.Calculate(24.0, parameters);

        Assert.Equal(1, result);
    }

    [Fact]
    public void Calculate_ZeroPayPeriodsPerYear_ReturnsZero()
    {
        var parameters = JsonSerializer.Deserialize<JsonElement>("{\"payPeriodsPerYear\":0}");

        var result = _sut.Calculate(10.0, parameters);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Calculate_ZeroDaysPerPayPeriod_ReturnsZero()
    {
        var parameters = JsonSerializer.Deserialize<JsonElement>("{\"daysPerPayPeriod\":0}");

        var result = _sut.Calculate(10.0, parameters);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Calculate_FractionalVacancies_CeilsUp()
    {
        // (0.1 * 365) / 24 / 12 = 36.5/288 = 0.1267 → ceiling = 1
        var parameters = JsonSerializer.Deserialize<JsonElement>("{}");

        var result = _sut.Calculate(0.1, parameters);

        Assert.Equal(1, result);
    }
}

public class StaticFormulaTests
{
    private readonly StaticFormula _sut = new();

    [Fact]
    public void FormulaType_IsStatic()
    {
        Assert.Equal(FormulaTypes.Static, _sut.FormulaType);
    }

    [Fact]
    public void Calculate_AlwaysReturnsZero_BecauseStaticIsANoOp()
    {
        var parameters = JsonSerializer.Deserialize<JsonElement>("{}");

        Assert.Equal(0, _sut.Calculate(0, parameters));
        Assert.Equal(0, _sut.Calculate(100, parameters));
    }
}

public class RequiredPositionsFormulaRegistryTests
{
    private static RequiredPositionsFormulaRegistry BuildRegistry() =>
        new([new StaticFormula(), new AnnualizedAverageFormula()]);

    [Fact]
    public void GetFormula_KnownType_ReturnsFormula()
    {
        var registry = BuildRegistry();

        var formula = registry.GetFormula(FormulaTypes.AnnualizedAverage);

        Assert.IsType<AnnualizedAverageFormula>(formula);
    }

    [Fact]
    public void GetFormula_IsCaseInsensitive()
    {
        var registry = BuildRegistry();

        var formula = registry.GetFormula("annualizedaverage");

        Assert.IsType<AnnualizedAverageFormula>(formula);
    }

    [Fact]
    public void GetFormula_UnknownType_Throws()
    {
        var registry = BuildRegistry();

        Assert.Throws<InvalidOperationException>(() => registry.GetFormula("UNKNOWN_TYPE"));
    }

    [Fact]
    public void GetFormula_StaticType_ReturnsStaticFormula()
    {
        var registry = BuildRegistry();

        var formula = registry.GetFormula(FormulaTypes.Static);

        Assert.IsType<StaticFormula>(formula);
    }
}
