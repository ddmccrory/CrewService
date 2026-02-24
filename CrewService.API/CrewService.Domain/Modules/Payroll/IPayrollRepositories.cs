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
}

public interface IPayrollRecordRepository : IRepository<PayrollRecord>
{
    Task<List<PayrollRecord>> GetByRunAsync(ControlNumber payrollRunCtrlNbr);
    Task<List<PayrollRecord>> GetByEmployeeAndRunAsync(ControlNumber employeeCtrlNbr, ControlNumber payrollRunCtrlNbr);
}
