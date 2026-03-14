using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class RegulatoryStandard : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int MaxOnDutyMinutes { get; private set; }
    public int MinRestMinutes { get; private set; }
    public bool Min8hRestInPreceding24h { get; private set; }
    public int ConsecutiveDayLimit6 { get; private set; }
    public int ConsecutiveDayLimit7 { get; private set; }
    public int RestAfter6DaysMinutes { get; private set; }
    public int RestAfter7DaysMinutes { get; private set; }
    public int MonthlyCapMinutes { get; private set; }
    public int DeadheadAfter12hMonthlyCapMinutes { get; private set; }
    public int WreckReliefExtraMinutes { get; private set; }
    public DateOnly EffectiveDate { get; private set; }

    private RegulatoryStandard() { }

    private RegulatoryStandard(
        string code,
        string description,
        int maxOnDutyMinutes,
        int minRestMinutes,
        bool min8hRestInPreceding24h,
        int consecutiveDayLimit6,
        int consecutiveDayLimit7,
        int restAfter6DaysMinutes,
        int restAfter7DaysMinutes,
        int monthlyCapMinutes,
        int deadheadAfter12hMonthlyCapMinutes,
        int wreckReliefExtraMinutes,
        DateOnly effectiveDate)
    {
        Code = code;
        Description = description;
        MaxOnDutyMinutes = maxOnDutyMinutes;
        MinRestMinutes = minRestMinutes;
        Min8hRestInPreceding24h = min8hRestInPreceding24h;
        ConsecutiveDayLimit6 = consecutiveDayLimit6;
        ConsecutiveDayLimit7 = consecutiveDayLimit7;
        RestAfter6DaysMinutes = restAfter6DaysMinutes;
        RestAfter7DaysMinutes = restAfter7DaysMinutes;
        MonthlyCapMinutes = monthlyCapMinutes;
        DeadheadAfter12hMonthlyCapMinutes = deadheadAfter12hMonthlyCapMinutes;
        WreckReliefExtraMinutes = wreckReliefExtraMinutes;
        EffectiveDate = effectiveDate;
    }

    public static RegulatoryStandard Create(
        string code,
        string description,
        int maxOnDutyMinutes,
        int minRestMinutes,
        bool min8hRestInPreceding24h,
        int consecutiveDayLimit6,
        int consecutiveDayLimit7,
        int restAfter6DaysMinutes,
        int restAfter7DaysMinutes,
        int monthlyCapMinutes,
        int deadheadAfter12hMonthlyCapMinutes,
        int wreckReliefExtraMinutes,
        DateOnly effectiveDate)
    {
        return new RegulatoryStandard(
            code, description, maxOnDutyMinutes, minRestMinutes,
            min8hRestInPreceding24h, consecutiveDayLimit6, consecutiveDayLimit7,
            restAfter6DaysMinutes, restAfter7DaysMinutes, monthlyCapMinutes,
            deadheadAfter12hMonthlyCapMinutes, wreckReliefExtraMinutes, effectiveDate);
    }
}
