using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

/// <summary>
/// Junction entity: which crafts require which regulatory qualifications.
/// </summary>
public sealed class CraftRegulatoryQualification : Entity
{
    public ControlNumber CraftCtrlNbr { get; private set; }
    public ControlNumber RegulatoryQualificationCtrlNbr { get; private set; }

    private CraftRegulatoryQualification()
    {
        CraftCtrlNbr = null!;
        RegulatoryQualificationCtrlNbr = null!;
    }

    public static CraftRegulatoryQualification Create(
        ControlNumber craftCtrlNbr,
        ControlNumber regulatoryQualificationCtrlNbr)
    {
        return new CraftRegulatoryQualification
        {
            CraftCtrlNbr = craftCtrlNbr,
            RegulatoryQualificationCtrlNbr = regulatoryQualificationCtrlNbr,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }
}
