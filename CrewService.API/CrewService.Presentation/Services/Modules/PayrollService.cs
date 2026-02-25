using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class PayrollService(
    ITimeEntryRepository timeEntryRepository,
    IPayrollRunRepository payrollRunRepository,
    IPayrollRecordRepository payrollRecordRepository) : PayrollSrvc.PayrollSrvcBase
{
    public override async Task<GetTimeEntriesResponse> GetTimeEntries(GetTimeEntriesRequest request, ServerCallContext context)
    {
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        var endUtc = DateTime.Parse(request.EndUtc).ToUniversalTime();
        var entries = await timeEntryRepository.GetByEmployeeAndPeriodAsync(ControlNumber.Create(request.EmployeeCtrlNbr), startUtc, endUtc);
        var response = new GetTimeEntriesResponse { TotalCount = entries.Count };
        foreach (var e in entries) response.Entries.Add(MapTimeEntry(e));
        return response;
    }

    public override async Task<TimeEntryResponse> CreateTimeEntry(CreateTimeEntryRequest request, ServerCallContext context)
    {
        var entry = TimeEntry.Create(request.EmployeeCtrlNbr, DateTime.Parse(request.DateUtc).ToUniversalTime(),
            request.EntryType, (decimal)request.Hours, request.ReasonCode, request.Notes);
        await timeEntryRepository.AddAsync(entry);
        return MapTimeEntry(entry);
    }

    public override async Task<PayrollRunResponse> GetPayrollRun(GetPayrollRunRequest request, ServerCallContext context)
    {
        var run = await payrollRunRepository.GetByPayPeriodAsync(request.PayPeriod)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Payroll run for period {request.PayPeriod} not found."));
        return MapRun(run);
    }

    public override async Task<PayrollRunResponse> CreatePayrollRun(CreatePayrollRunRequest request, ServerCallContext context)
    {
        var run = PayrollRun.Create(request.PayPeriod);
        await payrollRunRepository.AddAsync(run);
        return MapRun(run);
    }

    public override async Task<PayrollRunResponse> LockPayrollRun(LockPayrollRunRequest request, ServerCallContext context)
    {
        var run = await payrollRunRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Payroll run {request.CtrlNbr} not found."));
        run.Lock();
        await payrollRunRepository.UpdateAsync(run);
        return MapRun(run);
    }

    public override async Task<GetPayrollRecordsResponse> GetPayrollRecords(GetPayrollRecordsRequest request, ServerCallContext context)
    {
        var records = await payrollRecordRepository.GetByRunAsync(ControlNumber.Create(request.PayrollRunCtrlNbr));
        var response = new GetPayrollRecordsResponse { TotalCount = records.Count };
        foreach (var r in records) response.Records.Add(MapRecord(r));
        return response;
    }

    private static TimeEntryResponse MapTimeEntry(TimeEntry e) => new()
    {
        CtrlNbr = e.CtrlNbr.Value,
        EmployeeCtrlNbr = e.EmployeeCtrlNbr.Value,
        DateUtc = e.DateUtc.ToString("O"),
        EntryType = e.EntryType,
        Hours = (double)e.Hours,
        ReasonCode = e.ReasonCode ?? string.Empty,
        Notes = e.Notes ?? string.Empty,
        IsAdjustment = e.IsAdjustment
    };

    private static PayrollRunResponse MapRun(PayrollRun r) => new()
    {
        CtrlNbr = r.CtrlNbr.Value,
        PayPeriod = r.PayPeriod,
        Status = r.Status,
        Version = r.Version
    };

    private static PayrollRecordResponse MapRecord(PayrollRecord r) => new()
    {
        CtrlNbr = r.CtrlNbr.Value,
        PayrollRunCtrlNbr = r.PayrollRunCtrlNbr.Value,
        EmployeeCtrlNbr = r.EmployeeCtrlNbr.Value,
        EarningsType = r.EarningsType,
        Amount = (double)r.Amount,
        Hours = (double)r.Hours,
        PolicyRef = r.PolicyRef ?? string.Empty
    };
}
