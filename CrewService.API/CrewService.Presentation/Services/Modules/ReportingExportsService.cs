using CrewService.Application.ReportingExports;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class ReportingExportsService(IServiceProvider serviceProvider)
    : ReportingExportsSrvc.ReportingExportsSrvcBase
{
    public override async Task<PayrollExportBatchResponse> ExportPayroll(ExportPayrollRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<PayrollExportService>();
        var batch = await svc.ExportAsync(
            ControlNumber.Create(request.PayrollRunCtrlNbr),
            request.FormatCode,
            context.CancellationToken);

        return MapBatch(batch);
    }

    public override async Task<GetExportBatchesResponse> GetExportBatches(GetExportBatchesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<PayrollExportService>();
        var batches = await svc.GetExportBatchesAsync(
            ControlNumber.Create(request.PayrollRunCtrlNbr),
            context.CancellationToken);

        var response = new GetExportBatchesResponse { TotalCount = batches.Count };
        foreach (var b in batches) response.Batches.Add(MapBatch(b));
        return response;
    }

    public override async Task<ImportPayrollResponse> ImportPayroll(ImportPayrollRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<PayrollImportService>();
        var rows = request.Rows
            .Select(r => new PayrollImportRow(r.EmployeeCtrlNbr, (decimal)r.PaidAmount, request.PayPeriod))
            .ToList();

        var records = await svc.ImportAsync(
            request.SourceFile, rows, request.PayPeriod, context.CancellationToken);

        var matched = records.Count(r => r.MatchStatus == "Matched");
        return new ImportPayrollResponse
        {
            TotalRecords = records.Count,
            MatchedRecords = matched,
            UnmatchedRecords = records.Count - matched
        };
    }

    public override async Task<DailyReportResponse> GenerateDailyReport(GenerateDailyReportRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<DailyReportGenerationService>();
        var reportDate = DateOnly.Parse(request.ReportDate);
        var report = await svc.GenerateAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
            ControlNumber.Create(request.WorkInstanceCtrlNbr),
            reportDate,
            context.CancellationToken);

        var response = new DailyReportResponse
        {
            ReportDate = report.ReportDate.ToString("yyyy-MM-dd"),
            WorkAreaGroupCtrlNbr = report.WorkAreaGroupCtrlNbr.Value,
            GeneratedAtUtc = report.GeneratedAtUtc.ToString("O"),
            ReportText = DailyReportGenerationService.RenderText(report)
        };

        foreach (var s in report.Shifts)
        {
            response.Shifts.Add(new ShiftReportSectionMsg
            {
                ShiftCode = s.ShiftCode,
                TotalSlots = s.TotalSlots,
                FilledSlots = s.FilledSlots,
                OpenSlots = s.OpenSlots
            });
        }

        return response;
    }

    private static PayrollExportBatchResponse MapBatch(PayrollExportBatch b) => new()
    {
        CtrlNbr = b.CtrlNbr.Value,
        PayrollRunCtrlNbr = b.PayrollRunCtrlNbr.Value,
        ExportFormat = b.ExportFormat,
        GeneratedAtUtc = b.GeneratedAtUtc.ToString("O"),
        RecordCount = b.RecordCount,
        FilePath = b.FilePath ?? string.Empty
    };
}
