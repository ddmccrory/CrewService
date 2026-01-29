using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Railroads;

/// <summary>
/// Represents a payroll tier for a railroad pool.
/// </summary>
public sealed class RailroadPoolPayrollTier : Entity
{
    public ControlNumber RailroadPoolCtrlNbr { get; private set; }
    public int NumberOfDays { get; private set; }
    public int TypeOfDay { get; private set; } // 1=Calendar, 2=Working
    public int RatePercentage { get; private set; }

    private RailroadPoolPayrollTier()
    {
        RailroadPoolCtrlNbr = null!;
    }

    private RailroadPoolPayrollTier(ControlNumber railroadPoolCtrlNbr, int numberOfDays, int typeOfDay, int ratePercentage)
    {
        RailroadPoolCtrlNbr = railroadPoolCtrlNbr;
        NumberOfDays = numberOfDays;
        TypeOfDay = typeOfDay;
        RatePercentage = ratePercentage;
    }

    public static RailroadPoolPayrollTier Create(long railroadPoolCtrlNbr, int numberOfDays, int typeOfDay, int ratePercentage)
    {
        return new RailroadPoolPayrollTier(
            ControlNumber.Create(railroadPoolCtrlNbr),
            numberOfDays,
            typeOfDay,
            ratePercentage);
    }

    public RailroadPoolPayrollTier Update(int? numberOfDays = null, int? typeOfDay = null, int? ratePercentage = null)
    {
        if (numberOfDays.HasValue) NumberOfDays = numberOfDays.Value;
        if (typeOfDay.HasValue) TypeOfDay = typeOfDay.Value;
        if (ratePercentage.HasValue) RatePercentage = ratePercentage.Value;
        return this;
    }
}