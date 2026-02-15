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

    [Fact]
    public void TestGetBoundingBox()
    {
        var points = new PointF[]
         {
                new PointF(100, 10),
                new PointF(25, 110),
                new PointF(170, 195)
         };

        var box = PDFGraphicsAdapter.GetBoundingBox(points);

        Assert.Equal(25, box.X);
        Assert.Equal(10, box.Y);
        Assert.Equal(170, box.Width);
        Assert.Equal(195, box.Height);
    }

    [Fact]
    public void TestDrawImageUsingGdi()
    {
        using var pdfDocument = new PdfDocument();
        var pdfPage = pdfDocument.AddPage();

        using var gfx = XGraphics.FromPdfPage(pdfPage);
        using var adapter = new PDFGraphicsAdapter(gfx, new XSize(0, 0));

        // Create a simple green image using GDI+
        using var image = new Bitmap(200, 200);

        for (int y = 0; y < 200; y++)
        {
            for (int x = 0; x < 200; x++)
            {
                image.SetPixel(x, y, Color.Green);
            }
        }

        // Define points for the image corners
        var points = new PointF[]
        {
            new PointF(100, 10),
            new PointF(25, 110),
            new PointF(170, 195)       
        };

        adapter.DrawImage(image, points);
    
        var savePath = Path.Combine(Path.GetTempPath(), "TestDrawImageUsingGdi.pdf");
        pdfDocument.Save(savePath);

        Assert.True(File.Exists(savePath));
    }


    [Fact]
    public void TestIsVisible()
    {
        using var pdfDocument = new PdfDocument();
        var pdfPage = pdfDocument.AddPage();

        using var gfx = XGraphics.FromPdfPage(pdfPage);
        using var adapter = new PDFGraphicsAdapter(gfx, new XSize(0, 0));

        Assert.True(adapter.IsVisible(new RectangleF(0, 0, 100, 100)));
        Assert.False(adapter.IsVisible(new RectangleF(-200, -200, 50, 50)));
    }

    [Fact]
    public void TestGetRegionBoundingBox()
    {
        var region = new Region(new Rectangle(10, 10, 100, 100));

        var boundingBox = PDFGraphicsAdapter.GetRegionBoundingBox(region);

        Assert.Equal(10, boundingBox.X);
        Assert.Equal(10, boundingBox.Y);
        Assert.Equal(100, boundingBox.Width);
        Assert.Equal(100, boundingBox.Height);
    }

    [Fact]
    public void TestFillRegionUsingGdi()
    {
        var region = new Region(new Rectangle(10, 10, 100, 100));

        using var pdfDocument = new PdfDocument();
        var pdfPage = pdfDocument.AddPage();

        using var gfx = XGraphics.FromPdfPage(pdfPage);
        using var adapter = new PDFGraphicsAdapter(gfx, new XSize(0, 0));

        adapter.FillRegion(Brushes.Red, region);

        var savePath = Path.Combine(Path.GetTempPath(), "TestFillRegionUsingGdi.pdf");
        pdfDocument.Save(savePath);

        Assert.True(File.Exists(savePath));
    }

    [Fact]
    public void TestMeasureCharacterRangesGdi()
    {
        using var pdfDocument = new PdfDocument();
        var pdfPage = pdfDocument.AddPage();

        using var gfx = XGraphics.FromPdfPage(pdfPage);
        using var adapter = new PDFGraphicsAdapter(gfx, new XSize(0, 0));

        var font = new Font("Arial", 16);
        var layout = new RectangleF(20, 20, 130, 130);

        var format = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near
        };

        // See microsoft documentation
        // https://learn.microsoft.com/en-us/windows/win32/api/gdiplusgraphics/nf-gdiplusgraphics-graphics-measurecharacterranges

        format.SetMeasurableCharacterRanges([new CharacterRange(3, 5), new CharacterRange(15, 2)]);

        var regions = adapter.MeasureCharacterRanges("The quick, brown fox easily jumps over the lazy dog.", font, layout, format);

        var boundingBox = PDFGraphicsAdapter.GetRegionBoundingBox(regions[0]);

        Assert.Equal(61, boundingBox.X);
        Assert.Equal(20, boundingBox.Y);
        Assert.Equal(46, boundingBox.Width);
        Assert.Equal(24, boundingBox.Height);

        boundingBox = PDFGraphicsAdapter.GetRegionBoundingBox(regions[1]);

        Assert.Equal(70, boundingBox.X);
        Assert.Equal(45, boundingBox.Y);
        Assert.Equal(18, boundingBox.Width);
        Assert.Equal(24, boundingBox.Height);
    }

}
