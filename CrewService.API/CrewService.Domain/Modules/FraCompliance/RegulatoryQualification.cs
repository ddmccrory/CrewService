using CrewService.Domain.Primitives;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class RegulatoryQualification : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string CfrPart { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool RequiresCertification { get; private set; }
    public int? RecertificationIntervalMonths { get; private set; }
    public DateOnly EffectiveDate { get; private set; }

    private RegulatoryQualification() { }

    public static RegulatoryQualification Create(
        string code, string cfrPart, string description,
        bool requiresCertification, int? recertificationIntervalMonths,
        DateOnly effectiveDate)
    {
        return new RegulatoryQualification
        {
            Code = code,
            CfrPart = cfrPart,
            Description = description,
            RequiresCertification = requiresCertification,
            RecertificationIntervalMonths = recertificationIntervalMonths,
            EffectiveDate = effectiveDate
        };
    }
}
