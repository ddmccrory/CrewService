using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

public sealed record RegulatoryQualificationCatalogEntry(
    ControlNumber CtrlNbr,
    string Code,
    string CfrPart,
    string Description);

public interface IRegulatoryQualificationCatalog
{
    Task<IReadOnlyList<RegulatoryQualificationCatalogEntry>> GetAllAsync(CancellationToken ct = default);
}
