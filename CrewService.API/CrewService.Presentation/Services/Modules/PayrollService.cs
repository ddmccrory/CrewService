using CrewService.Application.Payroll;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class PayrollService(IServiceProvider serviceProvider) : PayrollSrvc.PayrollSrvcBase
{
    public override async Task<GetTimeEntriesResponse> GetTimeEntries(GetTimeEntriesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Payroll.PayrollService>();
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        var endUtc = DateTime.Parse(request.EndUtc).ToUniversalTime();
        var entries = await svc.GetTimeEntriesAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), startUtc, endUtc, context.CancellationToken);
        var response = new GetTimeEntriesResponse { TotalCount = entries.Count };
        foreach (var e in entries) response.Entries.Add(MapTimeEntry(e));
        return response;
    }

    public override async Task<TimeEntryResponse> CreateTimeEntry(CreateTimeEntryRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Payroll.PayrollService>();
        var entry = await svc.CreateTimeEntryAsync(
            request.EmployeeCtrlNbr, DateTime.Parse(request.DateUtc).ToUniversalTime(),
            request.EntryType, (decimal)request.Hours, request.ReasonCode, request.Notes,
            context.CancellationToken);
        return MapTimeEntry(entry);
    }

    public override async Task<PayrollRunResponse> GetPayrollRun(GetPayrollRunRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Payroll.PayrollService>();
        try
        {
            var run = await svc.GetPayrollRunAsync(request.PayPeriod, context.CancellationToken);
            return MapRun(run);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<PayrollRunResponse> CreatePayrollRun(CreatePayrollRunRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Payroll.PayrollService>();
        var run = await svc.CreatePayrollRunAsync(request.PayPeriod, context.CancellationToken);
        return MapRun(run);
    }

    public override async Task<PayrollRunResponse> LockPayrollRun(LockPayrollRunRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Payroll.PayrollService>();
        try
        {
            var run = await svc.LockPayrollRunAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapRun(run);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GetPayrollRecordsResponse> GetPayrollRecords(GetPayrollRecordsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Payroll.PayrollService>();
        var records = await svc.GetPayrollRecordsAsync(
            ControlNumber.Create(request.PayrollRunCtrlNbr), context.CancellationToken);
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

