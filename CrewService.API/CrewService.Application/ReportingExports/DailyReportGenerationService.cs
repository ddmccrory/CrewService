using CrewService.Application.DailyOperations;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using System.Text;

namespace CrewService.Application.ReportingExports;

public sealed record DailyOperationalReport(
    DateOnly ReportDate,
    ControlNumber WorkAreaGroupCtrlNbr,
    IReadOnlyList<ShiftReportSection> Shifts,
    DateTime GeneratedAtUtc);

public sealed record ShiftReportSection(
    string ShiftCode,
    DateTime ShiftStartUtc,
    DateTime ShiftEndUtc,
    int TotalSlots,
    int FilledSlots,
    int OpenSlots);

public sealed class DailyReportGenerationService(
    IShiftInstanceRepository shiftInstanceRepo,
    IShiftDefinitionRepository shiftDefRepo)
{
    public async Task<DailyOperationalReport> GenerateAsync(
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber workInstanceCtrlNbr,
        DateOnly reportDate,
        CancellationToken ct = default)
    {
        var shiftInstances = await shiftInstanceRepo.GetByWorkInstanceAsync(workInstanceCtrlNbr, ct);

        var sections = new List<ShiftReportSection>();

        foreach (var shift in shiftInstances)
        {
            var total = shift.PositionSlots.Count;
            var filled = shift.PositionSlots.Count(p => p.Status != "Open");
            sections.Add(new ShiftReportSection(
                shift.ShiftCode,
                shift.ShiftStartUtc,
                shift.ShiftEndUtc,
                total,
                filled,
                total - filled));
        }

        return new DailyOperationalReport(
            reportDate,
            workAreaGroupCtrlNbr,
            sections,
            DateTime.UtcNow);
    }

    public string RenderText(DailyOperationalReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Daily Operational Report — {report.ReportDate:yyyy-MM-dd}");
        sb.AppendLine($"Work Area: {report.WorkAreaGroupCtrlNbr.Value}");
        sb.AppendLine(new string('-', 60));

        foreach (var section in report.Shifts)
        {
            sb.AppendLine($"  Shift: {section.ShiftCode}  ({section.ShiftStartUtc:HH:mm} – {section.ShiftEndUtc:HH:mm})");
            sb.AppendLine($"    Slots: {section.TotalSlots}  Filled: {section.FilledSlots}  Open: {section.OpenSlots}");
        }

        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"Generated: {report.GeneratedAtUtc:u}");
        return sb.ToString();
    }
}
