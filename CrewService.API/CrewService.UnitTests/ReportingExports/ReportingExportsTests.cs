using CrewService.Application.ReportingExports;
using CrewService.Application.ReportingExports.Formatters;
using CrewService.Application.ReportingExports.Renderers;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.ReportingExports;

public class AdpExportFormatterTests
{
    [Fact]
    public void FormatRow_ProducesCorrectCsv()
    {
        var formatter = new AdpExportFormatter();
        var row = new PayrollExportRow(12345, "REG", "R01", 500m, 40m, "2025-07-15");

        var result = formatter.FormatRow(row);

        Assert.Contains("12345", result);
        Assert.Contains("R01", result);
        Assert.Contains("40.00", result);
        Assert.Contains("500.00", result);
        Assert.Contains("2025-07-15", result);
    }

    [Fact]
    public void FormatHeader_ContainsExpectedColumns()
    {
        var formatter = new AdpExportFormatter();
        var header = formatter.FormatHeader();

        Assert.Contains("CO_CODE", header);
        Assert.Contains("EARN_CODE", header);
        Assert.Contains("HOURS", header);
        Assert.Contains("AMOUNT", header);
    }
}

public class UkgExportFormatterTests
{
    [Fact]
    public void FormatRow_ProducesCorrectCsv()
    {
        var formatter = new UkgExportFormatter();
        var row = new PayrollExportRow(67890, "OT", "OT1", 375m, 10m, "2025-07-15");

        var result = formatter.FormatRow(row);

        Assert.Contains("67890", result);
        Assert.Contains("OT1", result);
        Assert.Contains("10.00", result);
        Assert.Contains("375.00", result);
        Assert.Contains("37.5000", result); // rate = 375/10
    }

    [Fact]
    public void FormatRow_ZeroHours_DoesNotDivideByZero()
    {
        var formatter = new UkgExportFormatter();
        var row = new PayrollExportRow(11111, "ADJ", null, 100m, 0m, "2025-07-15");

        var result = formatter.FormatRow(row);

        Assert.Contains("0.0000", result);
    }
}

public class PayrollExportBatchEntityTests
{
    [Fact]
    public void Create_SetsPropertiesCorrectly()
    {
        var runCtrl = ControlNumber.Create(1001);
        var batch = PayrollExportBatch.Create(runCtrl, "ADP", 50, "exports/test.csv");

        Assert.Equal(runCtrl, batch.PayrollRunCtrlNbr);
        Assert.Equal("ADP", batch.ExportFormat);
        Assert.Equal(50, batch.RecordCount);
        Assert.Equal("exports/test.csv", batch.FilePath);
        Assert.True(batch.DomainEvents.Count > 0);
    }
}

public class PayrollImportRecordEntityTests
{
    [Fact]
    public void Create_DefaultsToUnmatched()
    {
        var empCtrl = ControlNumber.Create(2001);
        var record = PayrollImportRecord.Create("file.csv", empCtrl, 100.50m);

        Assert.Equal("Unmatched", record.MatchStatus);
        Assert.Null(record.PayrollRecordCtrlNbr);
    }

    [Fact]
    public void MatchToRecord_SetsMatchedStatus()
    {
        var empCtrl = ControlNumber.Create(2001);
        var record = PayrollImportRecord.Create("file.csv", empCtrl, 100.50m);

        record.MatchToRecord(ControlNumber.Create(3001));

        Assert.Equal("Matched", record.MatchStatus);
        Assert.Equal(3001, record.PayrollRecordCtrlNbr!.Value);
    }
}

public class PlainTextReportRendererTests
{
    [Fact]
    public void Render_ContainsTitleAndSections()
    {
        var renderer = new PlainTextReportRenderer();
        var sections = new List<ReportSection>
        {
            new("Section A", [["row1col1", "row1col2"]], ["Col1", "Col2"])
        };

        var bytes = renderer.Render("Test Report", sections);
        var text = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.Contains("Test Report", text);
        Assert.Contains("Section A", text);
        Assert.Contains("Col1", text);
        Assert.Contains("row1col1", text);
    }
}
