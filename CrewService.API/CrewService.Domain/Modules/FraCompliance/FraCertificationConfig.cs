using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

/// <summary>
/// Railroad-configurable FRA certification settings per parent or railroad.
/// Railroad-level rows override parent-level defaults.
/// </summary>
public sealed class FraCertificationConfig : Entity
{
    public ControlNumber ParentCtrlNbr { get; private set; }
    public ControlNumber? RailroadCtrlNbr { get; private set; }

    /// <summary>Certification cycle length in months (default 36 per §240/242).</summary>
    public int CertCycleMonths { get; private set; }

    /// <summary>Days before expiration to auto-initiate a new Pending recertification.</summary>
    public int RecertWindowDays { get; private set; }

    /// <summary>Days before a check goes stale at which the cert status moves to Renew.</summary>
    public int RenewWindowDays { get; private set; }

    private FraCertificationConfig()
    {
        ParentCtrlNbr = null!;
    }

    public static FraCertificationConfig Create(
        ControlNumber parentCtrlNbr,
        ControlNumber? railroadCtrlNbr,
        int certCycleMonths = 36,
        int recertWindowDays = 180,
        int renewWindowDays = 60)
    {
        return new FraCertificationConfig
        {
            ParentCtrlNbr = parentCtrlNbr,
            RailroadCtrlNbr = railroadCtrlNbr,
            CertCycleMonths = certCycleMonths,
            RecertWindowDays = recertWindowDays,
            RenewWindowDays = renewWindowDays
        };
    }

    public void Update(int certCycleMonths, int recertWindowDays, int renewWindowDays)
    {
        CertCycleMonths = certCycleMonths;
        RecertWindowDays = recertWindowDays;
        RenewWindowDays = renewWindowDays;
    }
}
