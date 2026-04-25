using CrewService.Application.HolidayManagement;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class HolidayManagementService(IServiceProvider serviceProvider)
    : HolidayManagementSrvc.HolidayManagementSrvcBase
{
    public override Task<GetUsHolidayCatalogResponse> GetUsHolidayCatalog(
        GetUsHolidayCatalogRequest request, ServerCallContext context)
    {
        var resp = new GetUsHolidayCatalogResponse();
        foreach (var h in UsHolidayCatalog.All)
            resp.Holidays.Add(new UsHolidayDefinitionResponse { Code = h.Code, Name = h.Name });
        return Task.FromResult(resp);
    }

    public override async Task<GenerateHolidaysForYearResponse> GenerateHolidaysForYear(
        GenerateHolidaysForYearRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<HolidayAutoGenerationService>();
        var holidays = await svc.GenerateForYearAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), request.Year,
            request.ParentGroupCtrlNbr != 0 ? ControlNumber.Create(request.ParentGroupCtrlNbr) : null,
            context.CancellationToken);

        var resp = new GenerateHolidaysForYearResponse();
        foreach (var h in holidays)
        {
            resp.Holidays.Add(new GeneratedHolidayResponse
            {
                CtrlNbr = h.CtrlNbr.Value,
                Name = h.Name,
                ObservedDate = h.ObservedDate.ToString("yyyy-MM-dd"),
                IsActive = h.IsActive,
            });
        }
        return resp;
    }
}
