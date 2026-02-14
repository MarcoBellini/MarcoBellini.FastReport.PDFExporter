using PdfSharp.Drawing;
using PdfSharp.Pdf;
using FastReport.Export.PdfExporter;
using System.Drawing;

namespace FastReport.PdfExporter.Tests;

public class GraphicsAdapterTests
{

    [Fact]
    public void TestGetResizedRectForText()
    {
        using var pdfDocument = new PdfDocument();
        var pdfPage = pdfDocument.AddPage();

        using var gfx = XGraphics.FromPdfPage(pdfPage);
        using var adapter = new PDFGraphicsAdapter(gfx, new XSize(0, 0));

        var font = new XFont("Arial", 12);
        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center            
        };

        var text = "Hello, World!";

        var layoutRect = new XRect(0, 0, 100, 50);

        var resizedRect = adapter.GetVerticallyAlignedRectForText(text, font, format, layoutRect);

        Assert.Equal(0, resizedRect.Left);
        Assert.Equal(100, resizedRect.Right);
        Assert.InRange(resizedRect.Top, 18.2, 18.3);
        Assert.InRange(resizedRect.Bottom, 49.9, 50.0);
    }

    [Fact]
    public void TestGetTextWrappedToRectWidth()
    {
        using var pdfDocument = new PdfDocument();
        var pdfPage = pdfDocument.AddPage();

        using var gfx = XGraphics.FromPdfPage(pdfPage);
        using var adapter = new PDFGraphicsAdapter(gfx, new XSize(0, 0));

        var font = new XFont("Arial", 12);
        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        var text = "This is a very long string inside the destination rectangle!";

        var layoutRect = new XRect(0, 0, 40, 100);
        var resizedRect = adapter.GetVerticallyAlignedRectForText(text, font, format, layoutRect);
        var wrappedText = adapter.GetTextWrappedToRectWidth(text, font, resizedRect);

        Assert.Equal("This is a very long string inside the destina tion rectan gle!", wrappedText);
    }
}
