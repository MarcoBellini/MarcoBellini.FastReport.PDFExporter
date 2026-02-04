using PdfSharp.Drawing;
using PdfSharp.Pdf;
using FastReport.Export.PdfExporter;
using System.Drawing;

namespace FastReport.PdfExporter.Tests;

public class GraphicsAdapterTests
{

    [Fact]
    public void TestAlignTextVertically()
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

        adapter.AlignRectVertically(text, font, format, ref layoutRect);

        Assert.Equal(0, layoutRect.Left);
        Assert.Equal(100, layoutRect.Right);
        Assert.InRange(layoutRect.Top, 18.2, 18.3);
        Assert.InRange(layoutRect.Bottom, 49.0, 50.0);
    }
}
