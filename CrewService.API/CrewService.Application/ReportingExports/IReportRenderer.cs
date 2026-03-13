namespace CrewService.Application.ReportingExports;

public interface IReportRenderer
{
    string OutputFormat { get; }
    byte[] Render(string title, IReadOnlyList<ReportSection> sections);
}

public sealed record ReportSection(
    string Heading,
    IReadOnlyList<string[]> Rows,
    string[]? ColumnHeaders = null);
