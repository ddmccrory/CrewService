using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Safety;

public sealed class SafetyObservationResolution : Entity
{
    public ControlNumber ObservationCtrlNbr { get; private set; }
    public string ResolutionDescription { get; private set; } = string.Empty;
    public ControlNumber ResolvedByCtrlNbr { get; private set; }
    public DateTime ResolvedAtUtc { get; private set; }

    private SafetyObservationResolution()
    {
        ObservationCtrlNbr = null!;
        ResolvedByCtrlNbr = null!;
    }

    internal static SafetyObservationResolution Create(
        ControlNumber observationCtrlNbr, ControlNumber resolvedByCtrlNbr, string resolutionDescription)
    {
        return new SafetyObservationResolution
        {
            ObservationCtrlNbr = observationCtrlNbr,
            ResolvedByCtrlNbr = resolvedByCtrlNbr,
            ResolutionDescription = resolutionDescription,
            ResolvedAtUtc = DateTime.UtcNow
        };
    }
}
