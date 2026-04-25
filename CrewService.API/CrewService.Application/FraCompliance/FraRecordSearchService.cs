using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.FraCompliance;

namespace CrewService.Application.FraCompliance;

/// <summary>
/// Query service for FRA records by 7 CFR-mandated criteria (§228.203(d)).
/// </summary>
public sealed class FraRecordSearchService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<IReadOnlyList<FraDutyTour>> SearchAsync(FraRecordSearchCriteria criteria, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.FraDutyTours.SearchAsync(criteria, ct);
    }
}

