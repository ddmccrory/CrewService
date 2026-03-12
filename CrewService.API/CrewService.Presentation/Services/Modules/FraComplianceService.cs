using CrewService.Application.FraCompliance;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class FraComplianceService(IFraDutyTourRepository dutyTourRepository)
    : FraComplianceSrvc.FraComplianceSrvcBase
{
    public override async Task<SearchDutyToursResponse> SearchDutyTours(
        SearchDutyToursRequest request, ServerCallContext context)
    {
        var criteria = new FraRecordSearchCriteria
        {
            EmployeeCtrlNbr = request.HasEmployeeCtrlNbr
                ? ControlNumber.Create(request.EmployeeCtrlNbr) : null,
            StartDateUtc = request.StartDate?.ToDateTime(),
            EndDateUtc = request.EndDate?.ToDateTime(),
            LocationCode = request.HasLocationCode ? request.LocationCode : null,
            RegulatoryStandardCode = request.HasRegulatoryStandardCode
                ? request.RegulatoryStandardCode : null,
            HasExcessService = request.HasHasExcessService ? request.HasExcessService : null,
        };

        var tours = await dutyTourRepository.SearchAsync(criteria, context.CancellationToken);

        var response = new SearchDutyToursResponse();
        foreach (var tour in tours)
            response.DutyTours.Add(MapTour(tour));

        return response;
    }

    public override async Task<DutyTourResponse> GetDutyTour(
        GetDutyTourRequest request, ServerCallContext context)
    {
        var tour = await dutyTourRepository.GetByCtrlNbrAsync(
            ControlNumber.Create(request.CtrlNbr), context.CancellationToken);

        if (tour is null)
            throw new RpcException(new Status(StatusCode.NotFound, "Duty tour not found"));

        return MapTour(tour);
    }

    public override Task<GetEmployeeCertificationsResponse> GetEmployeeCertifications(
        GetEmployeeCertificationsRequest request, ServerCallContext context)
    {
        // Placeholder — full implementation in commit 19 of the plan
        return Task.FromResult(new GetEmployeeCertificationsResponse());
    }

    private static DutyTourResponse MapTour(Domain.Modules.FraCompliance.FraDutyTour tour)
    {
        var response = new DutyTourResponse
        {
            CtrlNbr = tour.CtrlNbr.Value,
            EmployeeCtrlNbr = tour.EmployeeCtrlNbr.Value,
            RegulatoryStandardCtrlNbr = tour.RegulatoryStandardCtrlNbr.Value,
            DutyTourStart = Timestamp.FromDateTime(
                DateTime.SpecifyKind(tour.DutyTourStartUtc, DateTimeKind.Utc)),
            ConsecutiveDays = tour.ConsecutiveDays,
            IsQuickTieUp = tour.IsQuickTieUp,
            IsCertified = tour.IsCertified,
        };

        if (tour.DutyTourEndUtc.HasValue)
            response.DutyTourEnd = Timestamp.FromDateTime(
                DateTime.SpecifyKind(tour.DutyTourEndUtc.Value, DateTimeKind.Utc));

        if (tour.TotalTimeOnDutyMinutes.HasValue)
            response.TotalTimeOnDutyMinutes = tour.TotalTimeOnDutyMinutes.Value;

        if (tour.ExcessMinutes.HasValue)
            response.ExcessMinutes = tour.ExcessMinutes.Value;

        return response;
    }
}
