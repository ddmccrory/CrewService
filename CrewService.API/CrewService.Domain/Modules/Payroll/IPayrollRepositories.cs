using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Payroll;

public interface ITimeEntryRepository : IRepository<TimeEntry>
{
    Task<List<TimeEntry>> GetByEmployeeAndPeriodAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc);
}

public interface IPayrollRunRepository : IRepository<PayrollRun>
{
    Task<PayrollRun?> GetByPayPeriodAsync(string payPeriod);
    Task<PayrollRun?> GetByPayPeriodAsync(string payPeriod, ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
}

public interface IPayrollRecordRepository : IRepository<PayrollRecord>
{
    Task<List<PayrollRecord>> GetByRunAsync(ControlNumber payrollRunCtrlNbr);
    Task<List<PayrollRecord>> GetByEmployeeAndRunAsync(ControlNumber employeeCtrlNbr, ControlNumber payrollRunCtrlNbr);
}

public interface IPayrollExportBatchRepository : IRepository<PayrollExportBatch>
{
    Task<IReadOnlyList<PayrollExportBatch>> GetByRunAsync(ControlNumber payrollRunCtrlNbr, CancellationToken ct = default);
}

public interface IPayrollImportRecordRepository : IRepository<PayrollImportRecord>
{
    Task<IReadOnlyList<PayrollImportRecord>> GetBySourceFileAsync(string sourceFile, CancellationToken ct = default);
}

public interface IHolidayRepository : IRepository<Holiday>
{
    Task<IReadOnlyList<Holiday>> GetActiveByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
}

public interface IHolidayQualificationRuleRepository : IRepository<HolidayQualificationRule>
{
    Task<IReadOnlyList<HolidayQualificationRule>> GetByHolidayAsync(ControlNumber holidayCtrlNbr, CancellationToken ct = default);
}

public interface IHolidayPayrollRecordRepository : IRepository<HolidayPayrollRecord>
{
}

public interface IEarningCodeRuleRepository : IRepository<EarningCodeRule>
{
    Task<IReadOnlyList<EarningCodeRule>> GetActiveByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
}

public interface IPayRateRepository : IRepository<PayRate>
{
    Task<PayRate?> GetEffectiveAsync(ControlNumber craftCtrlNbr, DateTime asOfDate,
        ControlNumber? craftRoleCtrlNbr = null, CancellationToken ct = default);
}
