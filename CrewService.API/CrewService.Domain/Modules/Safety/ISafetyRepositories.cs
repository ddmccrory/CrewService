using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Safety;

public interface ISafetyObservationRepository : IRepository<SafetyObservation>
{
    Task<IReadOnlyList<SafetyObservation>> GetByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);

    Task<IReadOnlyList<SafetyObservation>> GetOpenByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
}

public interface ISafetyObservationResolutionRepository : IRepository<SafetyObservationResolution>
{
    Task<SafetyObservationResolution?> GetByObservationAsync(
        ControlNumber observationCtrlNbr, CancellationToken ct = default);
}

public interface ISafetyCategoryRepository : IRepository<SafetyCategory>
{
    Task<IReadOnlyList<SafetyCategory>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
}
