using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Payroll;

public sealed class HolidayPayrollGenerationService(
    HolidayQualificationService qualificationService,
    IOrchestrationUnitOfWorkFactory uowFactory)
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

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        await uow.HolidayPayrollRecords.AddAsync(record, ct);
        await uow.CommitAsync(ct);
        return record;
    }
}

