using CrewService.Application.Payroll;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class HolidayPayrollService(IServiceProvider serviceProvider)
    : HolidayPayrollSrvc.HolidayPayrollSrvcBase
{
    public override async Task<HolidayQualificationResponse> EvaluateQualification(
        EvaluateQualificationRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<HolidayQualificationService>();
        var ctx = new HolidayQualificationContext(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            request.WorkedDayBefore, request.WorkedDayAfter,
            request.HasAbsenceCodeDayBefore ? request.AbsenceCodeDayBefore : null,
            request.HasAbsenceCodeDayAfter ? request.AbsenceCodeDayAfter : null);

        var result = await svc.EvaluateAsync(
            ControlNumber.Create(request.HolidayCtrlNbr), ctx, context.CancellationToken);

        var resp = new HolidayQualificationResponse { IsQualified = result.IsQualified };
        if (result.DisqualificationReason is not null)
            resp.DisqualificationReason = result.DisqualificationReason;
        return resp;
    }

    public override async Task<GetHolidaysResponse> GetHolidays(
        GetHolidaysRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<HolidayQualificationService>();
        var holidays = await svc.GetActiveByWorkAreaAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), context.CancellationToken);

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
