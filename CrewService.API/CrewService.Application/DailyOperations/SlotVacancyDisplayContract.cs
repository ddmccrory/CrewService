using CrewService.Domain.Modules.WorkManagement;

namespace CrewService.Application.DailyOperations;

public enum SlotVacancyDisplayReason
{
    None = 0,
    Annulled = 1,
    DoNotFill = 2,
    MarkedOffAbsence = 3,
    Vacancy = 4
}

public enum SlotVacancyActionability
{
    None = 0,
    ActionRequired = 1,
    NoWorkRequired = 2
}

public sealed record SlotVacancyDisplayContract(
    SlotVacancyDisplayReason Reason,
    SlotVacancyActionability Actionability,
    bool UseLegacyMarkedOffStyling,
    string? DisplayCode);

public static class SlotVacancyDisplayContractResolver
{
    public static SlotVacancyDisplayContract Resolve(
        PositionSlotStatus status,
        string? displayCode)
    {
        var normalizedCode = string.IsNullOrWhiteSpace(displayCode)
            ? null
            : displayCode.Trim().ToUpperInvariant();

        // Precedence mirrors legacy visual/operational expectations:
        // Annulled > DoNotFill > MarkedOff/Unavailable > Open vacancy > everything else.
        if (status == PositionSlotStatus.Annulled)
        {
            return new SlotVacancyDisplayContract(
                SlotVacancyDisplayReason.Annulled,
                SlotVacancyActionability.NoWorkRequired,
                UseLegacyMarkedOffStyling: false,
                DisplayCode: null);
        }

        if (status == PositionSlotStatus.DoNotFill)
        {
            return new SlotVacancyDisplayContract(
                SlotVacancyDisplayReason.DoNotFill,
                SlotVacancyActionability.NoWorkRequired,
                UseLegacyMarkedOffStyling: true,
                DisplayCode: normalizedCode);
        }

        if (status is PositionSlotStatus.MarkedOff or PositionSlotStatus.Unavailable)
        {
            return new SlotVacancyDisplayContract(
                SlotVacancyDisplayReason.MarkedOffAbsence,
                SlotVacancyActionability.ActionRequired,
                UseLegacyMarkedOffStyling: true,
                DisplayCode: normalizedCode);
        }

        if (status == PositionSlotStatus.Open)
        {
            return new SlotVacancyDisplayContract(
                SlotVacancyDisplayReason.Vacancy,
                SlotVacancyActionability.ActionRequired,
                UseLegacyMarkedOffStyling: false,
                DisplayCode: null);
        }

        return new SlotVacancyDisplayContract(
            SlotVacancyDisplayReason.None,
            SlotVacancyActionability.None,
            UseLegacyMarkedOffStyling: false,
            DisplayCode: null);
    }
}