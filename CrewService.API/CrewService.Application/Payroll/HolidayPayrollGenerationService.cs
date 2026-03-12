using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Payroll;

public interface IHolidayPayrollRecordRepository
{
    Task AddAsync(HolidayPayrollRecord record, CancellationToken ct = default);
}

public sealed class HolidayPayrollGenerationService(
    HolidayQualificationService qualificationService,
    IHolidayPayrollRecordRepository recordRepo)
{
    public async Task<HolidayPayrollRecord> GenerateAsync(
        ControlNumber holidayCtrlNbr,
        HolidayQualificationContext ctx,
        CancellationToken ct = default)
    {
        var result = await qualificationService.EvaluateAsync(holidayCtrlNbr, ctx, ct);

        var record = HolidayPayrollRecord.Create(
            holidayCtrlNbr, ctx.EmployeeCtrlNbr,
            result.IsQualified, result.DisqualificationReason);

        await recordRepo.AddAsync(record, ct);
        return record;
    }
}
