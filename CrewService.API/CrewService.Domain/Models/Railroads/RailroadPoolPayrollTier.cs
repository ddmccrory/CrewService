using CrewService.Domain.DomainEvents.Railroads;
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
        var entity = new RailroadPoolPayrollTier(
            ControlNumber.Create(railroadPoolCtrlNbr),
            numberOfDays,
            typeOfDay,
            ratePercentage);
        entity.Raise(new RailroadPoolPayrollTierCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public RailroadPoolPayrollTier Update(int? numberOfDays = null, int? typeOfDay = null, int? ratePercentage = null)
    {
        var changes = new Dictionary<string, object?>();

        if (numberOfDays.HasValue)
        {
            NumberOfDays = numberOfDays.Value;
            changes["numberOfDays"] = numberOfDays.Value;
        }

        if (typeOfDay.HasValue)
        {
            TypeOfDay = typeOfDay.Value;
            changes["typeOfDay"] = typeOfDay.Value;
        }

        if (ratePercentage.HasValue)
        {
            RatePercentage = ratePercentage.Value;
            changes["ratePercentage"] = ratePercentage.Value;
        }

        if (changes.Count > 0)
        {
            Raise(new RailroadPoolPayrollTierUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));
        }

        return this;
    }

    public void Delete()
    {
        Raise(new RailroadPoolPayrollTierDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}