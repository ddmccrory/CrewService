using CrewService.Application.Payroll;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class HolidayPayrollService(HolidayQualificationService qualificationService)
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
}
