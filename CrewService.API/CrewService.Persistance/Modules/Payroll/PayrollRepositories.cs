using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Payroll;

internal sealed class TimeEntryRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<TimeEntry>(dbContext, currentUserService), ITimeEntryRepository
{
    public async Task<List<TimeEntry>> GetByEmployeeAndPeriodAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc) =>
        await DbContext.Set<TimeEntry>()
            .Where(t => t.EmployeeCtrlNbr == employeeCtrlNbr && t.DateUtc >= startUtc && t.DateUtc < endUtc)
            .OrderBy(t => t.DateUtc)
            .ToListAsync();
}

internal sealed class PayrollRunRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<PayrollRun>(dbContext, currentUserService), IPayrollRunRepository
{
    public async Task<PayrollRun?> GetByPayPeriodAsync(string payPeriod) =>
        await DbContext.Set<PayrollRun>().SingleOrDefaultAsync(r => r.PayPeriod == payPeriod);

    public async Task<PayrollRun?> GetByPayPeriodAsync(string payPeriod, ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<PayrollRun>().FirstOrDefaultAsync(r => r.PayPeriod == payPeriod, ct);
}

internal sealed class PayrollRecordRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<PayrollRecord>(dbContext, currentUserService), IPayrollRecordRepository
{
    public async Task<List<PayrollRecord>> GetByRunAsync(ControlNumber payrollRunCtrlNbr) =>
        await DbContext.Set<PayrollRecord>().Where(r => r.PayrollRunCtrlNbr == payrollRunCtrlNbr).ToListAsync();

    public async Task<List<PayrollRecord>> GetByEmployeeAndRunAsync(ControlNumber employeeCtrlNbr, ControlNumber payrollRunCtrlNbr) =>
        await DbContext.Set<PayrollRecord>()
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr && r.PayrollRunCtrlNbr == payrollRunCtrlNbr)
            .ToListAsync();
}
