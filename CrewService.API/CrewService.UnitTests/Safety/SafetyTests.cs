using CrewService.Domain.Modules.Safety;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Safety;

public class SafetyObservationTests
{
    [Fact]
    public void Create_DefaultsToOpen()
    {
        var obs = SafetyObservation.Create(1, 100, "Track", "Yard", "Broken rail detected");

        Assert.Equal("Open", obs.Status);
        Assert.Equal("Track", obs.CategoryCode);
        Assert.Equal("Yard", obs.AreaCode);
        Assert.Empty(obs.Actions);
        Assert.True(obs.DomainEvents.Count > 0);
    }

    [Fact]
    public void AddAction_FromOpen_TransitionsToActionTaken()
    {
        var obs = SafetyObservation.Create(1, 100, "Track", "Yard", "Issue");

        obs.AddAction(ControlNumber.Create(200), "Flagged area for repair");

        Assert.Equal("ActionTaken", obs.Status);
        Assert.Single(obs.Actions);
    }

    [Fact]
    public void AddAction_FromActionTaken_StaysActionTaken()
    {
        var obs = SafetyObservation.Create(1, 100, "Track", "Yard", "Issue");
        obs.AddAction(ControlNumber.Create(200), "First action");

        obs.AddAction(ControlNumber.Create(300), "Second action");

        Assert.Equal("ActionTaken", obs.Status);
        Assert.Equal(2, obs.Actions.Count);
    }

    [Fact]
    public void AddAction_FromResolved_Throws()
    {
        var obs = SafetyObservation.Create(1, 100, "Track", "Yard", "Issue");
        obs.Resolve(ControlNumber.Create(200), "Fixed");

        Assert.Throws<InvalidOperationException>(() =>
            obs.AddAction(ControlNumber.Create(300), "Too late"));
    }

    [Fact]
    public void Resolve_FromOpen_SetsResolved()
    {
        var obs = SafetyObservation.Create(1, 100, "Signal", "MainLine", "Signal malfunction");

        var resolution = obs.Resolve(ControlNumber.Create(200), "Signal replaced");

        Assert.Equal("Resolved", obs.Status);
        Assert.Equal(obs.CtrlNbr, resolution.ObservationCtrlNbr);
        Assert.Equal("Signal replaced", resolution.ResolutionDescription);
    }

    [Fact]
    public void Resolve_FromActionTaken_SetsResolved()
    {
        var obs = SafetyObservation.Create(1, 100, "Track", "Yard", "Issue");
        obs.AddAction(ControlNumber.Create(200), "Temporary fix");

        var resolution = obs.Resolve(ControlNumber.Create(300), "Permanent repair done");

        Assert.Equal("Resolved", obs.Status);
    }

    [Fact]
    public void Resolve_AlreadyResolved_Throws()
    {
        var obs = SafetyObservation.Create(1, 100, "Track", "Yard", "Issue");
        obs.Resolve(ControlNumber.Create(200), "Fixed");

        Assert.Throws<InvalidOperationException>(() =>
            obs.Resolve(ControlNumber.Create(300), "Fixed again"));
    }

    [Fact]
    public void Create_WithSubdivision_SetsValue()
    {
        var obs = SafetyObservation.Create(1, 100, "Equipment", "Shop", "Broken tool", "NorthDiv");

        Assert.Equal("NorthDiv", obs.SubdivisionCode);
    }
}

public class SafetyReferenceDataTests
{
    [Fact]
    public void SafetyCategory_Create_SetsProperties()
    {
        var cat = SafetyCategory.Create(1, "TRACK", "Track Safety");

        Assert.Equal("TRACK", cat.Code);
        Assert.Equal("Track Safety", cat.DisplayName);
        Assert.True(cat.IsActive);
    }

    [Fact]
    public void SafetyCategory_Deactivate_SetsInactive()
    {
        var cat = SafetyCategory.Create(1, "TRACK", "Track Safety");

        cat.Deactivate();

        Assert.False(cat.IsActive);
    }
}
