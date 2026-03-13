using System.Text;

namespace CrewService.Application.ReportingExports.Renderers;

/// <summary>
/// Lightweight plain-text renderer. Replace with a real PDF library
/// (e.g. QuestPDF, iText) for production PDF output.
/// </summary>
public sealed class PlainTextReportRenderer : IReportRenderer
{
    public string OutputFormat => "TXT";

    public byte[] Render(string title, IReadOnlyList<ReportSection> sections)
    {
        var sb = new StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine(new string('=', title.Length));
        sb.AppendLine();

        foreach (var section in sections)
        {
            sb.AppendLine(section.Heading);
            sb.AppendLine(new string('-', section.Heading.Length));

            if (section.ColumnHeaders is not null)
                sb.AppendLine(string.Join(" | ", section.ColumnHeaders));

            foreach (var row in section.Rows)
                sb.AppendLine(string.Join(" | ", row));

            sb.AppendLine();
        }

        sb.AppendLine($"Generated: {DateTime.UtcNow:u}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}

/// <summary>
/// Stub PDF renderer — produces a minimal placeholder.
/// Replace internals with QuestPDF or similar for real PDF output.
/// </summary>
public sealed class PdfReportRenderer : IReportRenderer
{
    public string OutputFormat => "PDF";

    public byte[] Render(string title, IReadOnlyList<ReportSection> sections)
    {
        // Placeholder: produces plain-text content wrapped in a minimal structure.
        // In production, replace with QuestPDF / iText / SkiaSharp PDF generation.
        var sb = new StringBuilder();
        sb.AppendLine($"%PDF-PLACEHOLDER% {title}");
        sb.AppendLine();

        foreach (var section in sections)
        {
            sb.AppendLine($"[{section.Heading}]");

            if (section.ColumnHeaders is not null)
                sb.AppendLine(string.Join(" | ", section.ColumnHeaders));

            foreach (var row in section.Rows)
                sb.AppendLine(string.Join(" | ", row));

            sb.AppendLine();
        }

        sb.AppendLine($"Generated: {DateTime.UtcNow:u}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
