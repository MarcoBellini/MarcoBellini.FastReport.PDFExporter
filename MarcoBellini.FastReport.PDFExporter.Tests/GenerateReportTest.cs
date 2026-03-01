
using FastReport;
using System.Drawing;
using FastReport.Utils;

namespace MarcoBellini.FastReport.PDFExporter.Tests;

public class GenerateReportTest
{

    [Fact]
    public void TestExportReportFromCode()
    {   
        using var report = new Report();
        using var pdfExport = new PDFExport();

        ReportPage page1 = new ReportPage();
        page1.Name = "Page1";
        report.Pages.Add(page1);

        page1.ReportTitle = new ReportTitleBand();

        page1.ReportTitle.Height = Units.Centimeters * 1.5f;

        // create "Text" objects
        // report title
        TextObject text1 = new TextObject();
        text1.Name = "Text1";
        // set bounds
        text1.Bounds = new RectangleF(0, 0,
        Units.Centimeters * 19, Units.Centimeters * 1);
        // set text
        text1.Text = "TEST REPORT";
        // set appearance
        text1.HorzAlign = HorzAlign.Center;
        text1.Font = new Font("Tahoma", 14, FontStyle.Bold);
        // add it to ReportTitle
        page1.ReportTitle.Objects.Add(text1);

        var OutputPath = Path.Combine(Path.GetTempPath(), "SimpleReport.pdf");

        Assert.True(report.Prepare(), "Cannot prepare the report");

        report.Export(pdfExport, OutputPath);

        Assert.True(File.Exists(OutputPath));
    }

    [Fact]
    public void TestExportAdvancedReport()
    {
        using var report = new Report();
        using var pdfExport = new PDFExport();

        report.Load("Reports/AdvancedReport.frx");

        var OutputPath = Path.Combine(Path.GetTempPath(), "AdvancedReport.pdf");

        Assert.True(report.Prepare(), "Cannot prepare the report");               
  
        report.Export(pdfExport, OutputPath);

        Assert.True(File.Exists(OutputPath));
    }

    [Fact]
    public void TestExportBackgroundAndBorders()
    {
        using var report = new Report();
        using var pdfExport = new PDFExport();

        report.Load("Reports/BackgroundReport.frx");

        var OutputPath = Path.Combine(Path.GetTempPath(), "BackgroundReport.pdf");

        Assert.True(report.Prepare(), "Cannot prepare the report");

        report.Export(pdfExport, OutputPath);

        Assert.True(File.Exists(OutputPath));
    }
}
