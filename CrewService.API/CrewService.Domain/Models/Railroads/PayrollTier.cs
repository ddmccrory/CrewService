using CrewService.Domain.DomainEvents.Railroads;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Railroads;

/// <summary>
/// Represents a payroll tier for a dynamic group (pool).
/// </summary>
public sealed class PayrollTier : Entity
{
    public ControlNumber DynamicGroupCtrlNbr { get; private set; }
    public int NumberOfDays { get; private set; }
    public int TypeOfDay { get; private set; } // 1=Calendar, 2=Working
    public int RatePercentage { get; private set; }

    private PayrollTier()
    {
        DynamicGroupCtrlNbr = null!;
    }

    private PayrollTier(ControlNumber dynamicGroupCtrlNbr, int numberOfDays, int typeOfDay, int ratePercentage)
    {
        DynamicGroupCtrlNbr = dynamicGroupCtrlNbr;
        NumberOfDays = numberOfDays;
        TypeOfDay = typeOfDay;
        RatePercentage = ratePercentage;
    }

    public static PayrollTier Create(long dynamicGroupCtrlNbr, int numberOfDays, int typeOfDay, int ratePercentage)
    {
        var entity = new PayrollTier(
            ControlNumber.Create(dynamicGroupCtrlNbr),
            numberOfDays,
            typeOfDay,
            ratePercentage);
        entity.Raise(new PayrollTierCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public PayrollTier Update(int? numberOfDays = null, int? typeOfDay = null, int? ratePercentage = null)
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
            Raise(new PayrollTierUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));
        }

        return this;
    }

    public void Delete()
    {
        Raise(new PayrollTierDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}
