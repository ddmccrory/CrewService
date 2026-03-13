using CrewService.Application.ReportingExports;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class ReportingExportsService(
    PayrollExportService exportService,
    PayrollImportService importService,
    DailyReportGenerationService dailyReportService,
    IPayrollExportBatchRepository exportBatchRepo) : ReportingExportsSrvc.ReportingExportsSrvcBase
{
    public override async Task<PayrollExportBatchResponse> ExportPayroll(ExportPayrollRequest request, ServerCallContext context)
    {
        var batch = await exportService.ExportAsync(
            ControlNumber.Create(request.PayrollRunCtrlNbr),
            request.FormatCode,
            context.CancellationToken);

        return MapBatch(batch);
    }

    public override async Task<GetExportBatchesResponse> GetExportBatches(GetExportBatchesRequest request, ServerCallContext context)
    {
        var batches = await exportBatchRepo.GetByRunAsync(
            ControlNumber.Create(request.PayrollRunCtrlNbr),
            context.CancellationToken);

        var response = new GetExportBatchesResponse { TotalCount = batches.Count };
        foreach (var b in batches) response.Batches.Add(MapBatch(b));
        return response;
    }

    public override async Task<ImportPayrollResponse> ImportPayroll(ImportPayrollRequest request, ServerCallContext context)
    {
        var rows = request.Rows
            .Select(r => new PayrollImportRow(r.EmployeeCtrlNbr, (decimal)r.PaidAmount, request.PayPeriod))
            .ToList();

        var records = await importService.ImportAsync(
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
        var reportDate = DateOnly.Parse(request.ReportDate);
        var report = await dailyReportService.GenerateAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
            ControlNumber.Create(request.WorkInstanceCtrlNbr),
            reportDate,
            context.CancellationToken);

        var response = new DailyReportResponse
        {
            ReportDate = report.ReportDate.ToString("yyyy-MM-dd"),
            WorkAreaGroupCtrlNbr = report.WorkAreaGroupCtrlNbr.Value,
            GeneratedAtUtc = report.GeneratedAtUtc.ToString("O"),
            ReportText = dailyReportService.RenderText(report)
        };

        foreach (var s in report.Shifts)
        {
            response.Shifts.Add(new ShiftReportSectionMsg
            {
                ShiftCode = s.ShiftCode,
                ShiftStartUtc = s.ShiftStartUtc.ToString("O"),
                ShiftEndUtc = s.ShiftEndUtc.ToString("O"),
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
