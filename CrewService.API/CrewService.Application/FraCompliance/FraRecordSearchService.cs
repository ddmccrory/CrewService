using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.FraCompliance;

/// <summary>
/// Query service for FRA records by 7 CFR-mandated criteria (§228.203(d)).
/// </summary>
public sealed class FraRecordSearchService(IFraDutyTourRepository repository)
{
    public Task<IReadOnlyList<FraDutyTour>> SearchAsync(FraRecordSearchCriteria criteria, CancellationToken ct = default)
        => repository.SearchAsync(criteria, ct);
}

public sealed record FraRecordSearchCriteria
{
    public ControlNumber? EmployeeCtrlNbr { get; init; }
    public DateTime? StartDateUtc { get; init; }
    public DateTime? EndDateUtc { get; init; }
    public string? LocationCode { get; init; }
    public string? RegulatoryStandardCode { get; init; }
    public bool? HasExcessService { get; init; }
    public bool? IsCertified { get; init; }
}

public interface IFraDutyTourRepository
{
    Task<IReadOnlyList<FraDutyTour>> SearchAsync(FraRecordSearchCriteria criteria, CancellationToken ct = default);
    Task<FraDutyTour?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task AddAsync(FraDutyTour tour, CancellationToken ct = default);
    Task<FraDutyTour?> GetActiveTourForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);
}
