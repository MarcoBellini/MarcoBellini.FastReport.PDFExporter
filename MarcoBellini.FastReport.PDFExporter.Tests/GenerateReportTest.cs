
using FastReport;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.RegularExpressions;
using FastReport.Utils;

namespace MarcoBellini.FastReport.PDFExporter.Tests;

public partial class GenerateReportTest
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

        var pdf = AssertValidPdf(File.ReadAllBytes(OutputPath), expectedPages: 1);

        // Embedded fonts are subset: a one-line report must stay small (the full Tahoma Bold is ~850 KB).
        Assert.True(pdf.Length < 100_000, $"PDF size {pdf.Length} bytes");
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
        AssertValidPdf(File.ReadAllBytes(OutputPath), expectedPages: report.PreparedPages.Count);
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
        AssertValidPdf(File.ReadAllBytes(OutputPath), expectedPages: report.PreparedPages.Count);
    }

    [Fact]
    public void TestExportToStreamWithMetadata()
    {
        using var report = CreateFeatureReport();
        using var pdfExport = new PDFExport();
        using var stream = new MemoryStream();

        report.ReportInfo.Name = "Feature Report";
        report.ReportInfo.Author = "Unit Test";
        report.ReportInfo.Description = "Skia engine";

        Assert.True(report.Prepare(), "Cannot prepare the report");

        report.Export(pdfExport, stream);

        var pdf = AssertValidPdf(stream.ToArray(), expectedPages: 2);
        var text = Encoding.Latin1.GetString(pdf);

        Assert.Contains("(Feature Report)", text);
        Assert.Contains("(Unit Test)", text);
        Assert.Contains("(Skia engine)", text);
    }

    [Fact]
    public void TestExportFeatureReport()
    {
        using var report = CreateFeatureReport();
        using var pdfExport = new PDFExport();

        var outputPath = Path.Combine(Path.GetTempPath(), "FeatureReport.pdf");

        Assert.True(report.Prepare(), "Cannot prepare the report");

        report.Export(pdfExport, outputPath);

        AssertValidPdf(File.ReadAllBytes(outputPath), expectedPages: 2);
    }

    /// <summary>
    /// Two pages covering gradients, hatch fills, transparency, rotated text, pictures, shapes, dashed lines and watermarks.
    /// </summary>
    private static Report CreateFeatureReport()
    {
        var report = new Report();

        for (int pageIndex = 0; pageIndex < 2; pageIndex++)
        {
            var page = new ReportPage { Name = $"Page{pageIndex + 1}" };
            report.Pages.Add(page);

            page.Watermark.Enabled = true;
            page.Watermark.Text = "DRAFT";
            page.Watermark.Image = CreatePicture();
            page.Watermark.ImageTransparency = 0.7f;
            page.Border.Lines = BorderLines.All;

            page.ReportTitle = new ReportTitleBand { Name = $"Title{pageIndex + 1}", Height = Units.Centimeters * 12 };

            page.ReportTitle.Objects.Add(new TextObject
            {
                Name = $"Gradient{pageIndex}",
                Bounds = new RectangleF(0, 0, Units.Centimeters * 8, Units.Centimeters * 2),
                Text = "Gradient fill with a long text that must wrap inside the object",
                Fill = new LinearGradientFill(Color.Yellow, Color.Orange, 90),
                Border = new Border { Lines = BorderLines.All, Style = LineStyle.Dash }
            });

            page.ReportTitle.Objects.Add(new TextObject
            {
                Name = $"Hatch{pageIndex}",
                Bounds = new RectangleF(Units.Centimeters * 9, 0, Units.Centimeters * 8, Units.Centimeters * 2),
                Text = "Hatch fill",
                HorzAlign = HorzAlign.Right,
                VertAlign = VertAlign.Bottom,
                Fill = new HatchFill(Color.LightGray, Color.White, HatchStyle.DiagonalCross)
            });

            page.ReportTitle.Objects.Add(new TextObject
            {
                Name = $"Rotated{pageIndex}",
                Bounds = new RectangleF(0, Units.Centimeters * 3, Units.Centimeters * 3, Units.Centimeters * 5),
                Text = "Rotated text",
                Angle = 90,
                TextColor = Color.FromArgb(128, Color.Blue),
                Font = new Font("Arial", 12, FontStyle.Italic | FontStyle.Underline)
            });

            page.ReportTitle.Objects.Add(new PictureObject
            {
                Name = $"Picture{pageIndex}",
                Bounds = new RectangleF(Units.Centimeters * 4, Units.Centimeters * 3, Units.Centimeters * 4, Units.Centimeters * 3),
                Image = CreatePicture(),
                Angle = 30
            });

            page.ReportTitle.Objects.Add(new ShapeObject
            {
                Name = $"Shape{pageIndex}",
                Bounds = new RectangleF(Units.Centimeters * 9, Units.Centimeters * 3, Units.Centimeters * 4, Units.Centimeters * 3),
                Shape = ShapeKind.Ellipse,
                Fill = new SolidFill(Color.FromArgb(100, Color.Green))
            });

            page.ReportTitle.Objects.Add(new LineObject
            {
                Name = $"Line{pageIndex}",
                Bounds = new RectangleF(0, Units.Centimeters * 9, Units.Centimeters * 17, 0),
                Border = new Border { Lines = BorderLines.All, Style = LineStyle.DashDot, Width = 2 }
            });
        }

        return report;
    }

    private static Bitmap CreatePicture()
    {
        var bitmap = new Bitmap(120, 80);

        using var gfx = Graphics.FromImage(bitmap);
        using var brush = new LinearGradientBrush(new Rectangle(0, 0, 120, 80), Color.Red, Color.Blue, 45f);
        gfx.FillRectangle(brush, 0, 0, 120, 80);

        return bitmap;
    }

    private static byte[] AssertValidPdf(byte[] pdf, int expectedPages)
    {
        Assert.True(pdf.Length > 0);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
        Assert.Equal(expectedPages, PageRegex().Matches(Encoding.Latin1.GetString(pdf)).Count);

        return pdf;
    }

    [GeneratedRegex(@"/Type\s*/Page(?![a-zA-Z])")]
    private static partial Regex PageRegex();
}
