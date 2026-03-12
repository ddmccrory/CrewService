using CrewService.Application.HolidayManagement;
using CrewService.Application.Payroll;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class HolidayPayrollService(
    HolidayQualificationService qualificationService,
    HolidayAutoGenerationService autoGenerationService)
    : HolidayPayrollSrvc.HolidayPayrollSrvcBase
{
    public override async Task<HolidayQualificationResponse> EvaluateQualification(
        EvaluateQualificationRequest request, ServerCallContext context)
    {
        var ctx = new HolidayQualificationContext(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            request.WorkedDayBefore, request.WorkedDayAfter,
            request.HasAbsenceCodeDayBefore ? request.AbsenceCodeDayBefore : null,
            request.HasAbsenceCodeDayAfter ? request.AbsenceCodeDayAfter : null);

        var result = await qualificationService.EvaluateAsync(
            ControlNumber.Create(request.HolidayCtrlNbr), ctx, context.CancellationToken);

        var resp = new HolidayQualificationResponse { IsQualified = result.IsQualified };
        if (result.DisqualificationReason is not null)
            resp.DisqualificationReason = result.DisqualificationReason;
        return resp;
    }

    public override Task<GetHolidaysResponse> GetHolidays(
        GetHolidaysRequest request, ServerCallContext context)
    {
        return Task.FromResult(new GetHolidaysResponse());
    }

    public override Task<GetUsHolidayCatalogResponse> GetUsHolidayCatalog(
        GetUsHolidayCatalogRequest request, ServerCallContext context)
    {
        var resp = new GetUsHolidayCatalogResponse();
        foreach (var h in UsHolidayCatalog.All)
            resp.Holidays.Add(new UsHolidayDefinitionResponse { Code = h.Code, Name = h.Name });
        return Task.FromResult(resp);
    }

    public override async Task<GetHolidaysResponse> GenerateHolidaysForYear(
        GenerateHolidaysForYearRequest request, ServerCallContext context)
    {
        var holidays = await autoGenerationService.GenerateForYearAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), request.Year,
            request.ParentGroupCtrlNbr != 0 ? ControlNumber.Create(request.ParentGroupCtrlNbr) : null,
            context.CancellationToken);

        var resp = new GetHolidaysResponse();
        foreach (var h in holidays)
        {
            resp.Holidays.Add(new HolidayResponse
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
