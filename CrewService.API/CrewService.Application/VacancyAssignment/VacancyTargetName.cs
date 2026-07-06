using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.WorkManagement;

namespace CrewService.Application.VacancyAssignment;

/// <summary>
/// Single source of truth for the human-readable name stored on a <c>PositionVacancy</c>
/// (and surfaced on the bulletin as its "Position"). Centralized so the create-position and
/// vacate/repost paths always compose the same string — previously they drifted, and a
/// vacated crew position lost its craft role on the bulletin.
/// </summary>
public static class VacancyTargetName
{
    /// <summary>
    /// Composes the vacancy target name for a crew position as "{Crew} - {CraftRole}"
    /// (e.g. "130 - Engineer").
    /// </summary>
    public static string ForCrewPosition(Crew crew, CraftRole craftRole) =>
        $"{crew.Name} - {craftRole.Name}";
}
