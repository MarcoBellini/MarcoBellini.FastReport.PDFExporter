using SkiaSharp;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;

namespace MarcoBellini.FastReport.PDFExporter.Tests;

public class GraphicsAdapterTests
{
    private const float Tolerance = 0.001f;

    [Fact]
    public void TestTransformExcludesPageScale()
    {
        using var pdf = new PdfTestCanvas();
        var adapter = pdf.Adapter;

        Assert.True(adapter.Transform.IsIdentity);

        adapter.TranslateTransform(10, 20);

        var elements = adapter.Transform.Elements;
        Assert.Equal(10f, elements[4], 3);
        Assert.Equal(20f, elements[5], 3);

        // The canvas itself also carries the pixel to point scale.
        Assert.Equal(0.75f, pdf.Canvas.TotalMatrix.ScaleX, 3);
    }

    [Fact]
    public void TestTransformSetter()
    {
        using var pdf = new PdfTestCanvas();
        using var matrix = new Matrix(2, 0, 0, 3, 5, 7);

        pdf.Adapter.Transform = matrix;

        Assert.Equal(matrix.Elements, pdf.Adapter.Transform.Elements);
    }

    [Fact]
    public void TestRotateAndScaleTransformMatchGdi()
    {
        using var pdf = new PdfTestCanvas();
        using var gdiMatrix = new Matrix();

        pdf.Adapter.TranslateTransform(30, 40);
        pdf.Adapter.RotateTransform(45);
        pdf.Adapter.ScaleTransform(2, 0.5f);

        gdiMatrix.Translate(30, 40);
        gdiMatrix.Rotate(45);
        gdiMatrix.Scale(2, 0.5f);

        AssertMatrixEqual(gdiMatrix, pdf.Adapter.Transform);
    }

    [Theory]
    [InlineData(MatrixOrder.Prepend)]
    [InlineData(MatrixOrder.Append)]
    public void TestMultiplyTransformMatchesGdi(MatrixOrder order)
    {
        using var pdf = new PdfTestCanvas();
        using var gdiMatrix = new Matrix();
        using var multiplier = new Matrix(1, 0.5f, 0, 1, 10, 0);

        pdf.Adapter.TranslateTransform(5, 5);
        gdiMatrix.Translate(5, 5);

        pdf.Adapter.MultiplyTransform(multiplier, order);
        gdiMatrix.Multiply(multiplier, order);

        AssertMatrixEqual(gdiMatrix, pdf.Adapter.Transform);
    }

    [Fact]
    public void TestSaveRestoreTransform()
    {
        using var pdf = new PdfTestCanvas();
        var adapter = pdf.Adapter;

        var state = adapter.Save();
        adapter.TranslateTransform(100, 100);
        adapter.Restore(state);

        Assert.True(adapter.Transform.IsIdentity);
    }

    [Fact]
    public void TestRestoreWithForeignStateThrows()
    {
        using var pdf = new PdfTestCanvas();

        Assert.Throws<ArgumentException>(() => pdf.Adapter.Restore(new ForeignState()));
    }

    private sealed class ForeignState : global::FastReport.IGraphicsState
    {
    }

    [Fact]
    public void TestClipping()
    {
        using var pdf = new PdfTestCanvas();
        var adapter = pdf.Adapter;

        adapter.SetClip(new RectangleF(0, 0, 100, 100), CombineMode.Intersect);

        Assert.False(adapter.IsClipEmpty);
        Assert.True(adapter.IsVisible(new RectangleF(10, 10, 20, 20)));
        Assert.False(adapter.IsVisible(new RectangleF(200, 200, 20, 20)));

        adapter.SetClip(new RectangleF(500, 500, 10, 10), CombineMode.Intersect);
        Assert.True(adapter.IsClipEmpty);

        adapter.ResetClip();
        Assert.False(adapter.IsClipEmpty);
        Assert.True(adapter.IsVisible(new RectangleF(200, 200, 20, 20)));
    }

    [Fact]
    public void TestSetClipReplaceKeepsTransform()
    {
        using var pdf = new PdfTestCanvas();
        var adapter = pdf.Adapter;

        adapter.TranslateTransform(10, 10);
        adapter.SetClip(new RectangleF(0, 0, 50, 50));
        adapter.SetClip(new RectangleF(100, 100, 50, 50), CombineMode.Replace);

        Assert.True(adapter.IsVisible(new RectangleF(110, 110, 10, 10)));
        Assert.False(adapter.IsVisible(new RectangleF(10, 10, 10, 10)));
        Assert.Equal(10f, adapter.Transform.Elements[4], 3);
    }

    [Fact]
    public void TestClipIsRestoredWithState()
    {
        using var pdf = new PdfTestCanvas();
        var adapter = pdf.Adapter;

        var state = adapter.Save();
        adapter.SetClip(new RectangleF(0, 0, 10, 10), CombineMode.Intersect);
        adapter.Restore(state);

        Assert.True(adapter.IsVisible(new RectangleF(300, 300, 10, 10)));
    }

    [Fact]
    public void TestPathClipAndExclude()
    {
        using var pdf = new PdfTestCanvas();
        using var path = new GraphicsPath();
        path.AddEllipse(0, 0, 100, 100);

        pdf.Adapter.SetClip(path, CombineMode.Intersect);
        pdf.Adapter.SetClip(new RectangleF(0, 0, 100, 50), CombineMode.Exclude);

        Assert.False(pdf.Adapter.IsClipEmpty);
        // IsVisible is conservative (bounds based) like Skia QuickReject: only rectangles outside the clip bounds are rejected.
        Assert.True(pdf.Adapter.IsVisible(new RectangleF(40, 70, 10, 10)));
        Assert.False(pdf.Adapter.IsVisible(new RectangleF(200, 10, 10, 10)));

        using var gdi = CreateGdiGraphics();
        using var clip = pdf.Adapter.Clip;
        var bounds = clip.GetBounds(gdi.Graphics);

        // Skia reports conservative local clip bounds: they contain the visible area and never exceed the ellipse.
        Assert.InRange(bounds.Top, -2f, 50f);
        Assert.InRange(bounds.Bottom, 100f, 102f);
        Assert.InRange(bounds.Left, -2f, 0f);
        Assert.InRange(bounds.Right, 100f, 102f);
    }

    [Fact]
    public void TestParallelogramMatrix()
    {
        PointF[] points = [new(100, 10), new(170, 30), new(25, 110)];

        var matrix = PDFGraphicsAdapter.GetParallelogramMatrix(points, 200, 100);

        Assert.Equal(new SKPoint(100, 10), matrix.MapPoint(0, 0));
        Assert.Equal(new SKPoint(170, 30), matrix.MapPoint(200, 0));
        Assert.Equal(new SKPoint(25, 110), matrix.MapPoint(0, 100));
    }

    [Fact]
    public void TestDrawImageVariants()
    {
        using var pdf = new PdfTestCanvas();
        using var image = CreateGreenBitmap(200, 100);
        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(new ColorMatrix { Matrix33 = 0.5f });

        var adapter = pdf.Adapter;

        adapter.DrawImage(image, 10, 10);
        adapter.DrawImage(image, new RectangleF(10, 10, 50, 50));
        adapter.DrawImage(image, 10, 10, 50, 50);
        adapter.DrawImage(image, new RectangleF(0, 0, 100, 50), new RectangleF(10, 10, 50, 50), GraphicsUnit.Pixel);
        adapter.DrawImage(image, new Rectangle(0, 0, 50, 50), 0, 0, 100, 50, GraphicsUnit.Pixel, attributes);
        adapter.DrawImage(image, new Rectangle(0, 0, 50, 50), 0f, 0f, 100f, 50f, GraphicsUnit.Pixel, attributes);
        adapter.DrawImage(image, [new PointF(100, 10), new PointF(170, 30), new PointF(25, 110)]);
        adapter.DrawImageUnscaled(image, new Rectangle(300, 300, 50, 50));

        AssertValidPdf(pdf.Close());
    }

    [Fact]
    public void TestDrawImageWithWrongPointsThrows()
    {
        using var pdf = new PdfTestCanvas();
        using var image = CreateGreenBitmap(10, 10);

        Assert.Throws<ArgumentException>(() => pdf.Adapter.DrawImage(image, [new PointF(0, 0)]));
    }

    [Fact]
    public void TestImageIsEmbeddedOnce()
    {
        using var pdf = new PdfTestCanvas();
        using var image = CreateGreenBitmap(300, 300);

        for (int i = 0; i < 5; i++)
            pdf.Adapter.DrawImage(image, new RectangleF(i * 50, 0, 50, 50));

        var pdfText = Encoding.Latin1.GetString(pdf.Close());

        Assert.Equal(1, CountOccurrences(pdfText, "/Subtype /Image"));
    }

    [Fact]
    public void TestPhysicalImageSize()
    {
        using var image = CreateGreenBitmap(300, 150);
        image.SetResolution(192, 192);

        Assert.Equal(new SizeF(150, 75), PDFGraphicsAdapter.GetPhysicalSize(image));
    }

    [Theory]
    [InlineData(GraphicsUnit.Pixel, 72f)]
    [InlineData(GraphicsUnit.Point, 96f)]
    [InlineData(GraphicsUnit.Inch, 96f * 72f)]
    public void TestSourceRectUnits(GraphicsUnit unit, float expectedWidth)
    {
        using var image = CreateGreenBitmap(10, 10);

        var rect = PDFGraphicsAdapter.ToSourcePixels(image, new RectangleF(0, 0, 72, 72), unit);

        Assert.Equal(expectedWidth, rect.Width, 2);
    }

    [Fact]
    public void TestFillRegion()
    {
        using var pdf = new PdfTestCanvas();
        using var region = new Region(new Rectangle(10, 10, 100, 100));

        pdf.Adapter.FillRegion(Brushes.Red, region);

        var pdfText = Encoding.Latin1.GetString(pdf.Close());

        // Vector output: no image is needed anymore.
        Assert.Equal(0, CountOccurrences(pdfText, "/Subtype /Image"));
    }

    [Fact]
    public void TestDrawAllShapes()
    {
        using var pdf = new PdfTestCanvas();
        using var pen = new Pen(Color.FromArgb(128, Color.Blue), 2) { DashStyle = DashStyle.DashDot };
        using var gradient = new LinearGradientBrush(new Rectangle(0, 0, 100, 100), Color.Red, Color.Yellow, 45f);
        using var hatch = new HatchBrush(HatchStyle.Cross, Color.Black, Color.Transparent);
        using var path = new GraphicsPath();
        path.AddRectangle(new RectangleF(10, 10, 40, 40));
        path.AddEllipse(60, 60, 40, 40);

        PointF[] pointsF = [new(0, 0), new(50, 10), new(30, 60)];
        Point[] points = [new(0, 0), new(50, 10), new(30, 60)];

        var adapter = pdf.Adapter;

        adapter.DrawArc(pen, 0, 0, 100, 100, 0, 90);
        adapter.DrawCurve(pen, pointsF, 0, 2, 0.5f);
        adapter.DrawEllipse(pen, 0, 0, 50, 30);
        adapter.DrawEllipse(pen, new RectangleF(0, 0, 50, 30));
        adapter.DrawLine(pen, 0, 0, 100, 100);
        adapter.DrawLine(pen, new PointF(0, 0), new PointF(100, 0));
        adapter.DrawLines(pen, pointsF);
        adapter.DrawPath(pen, path);
        adapter.DrawPie(pen, 0, 0, 100, 100, 45, 90);
        adapter.DrawPolygon(pen, pointsF);
        adapter.DrawPolygon(pen, points);
        adapter.DrawRectangle(pen, 0, 0, 10, 10);
        adapter.DrawRectangle(pen, new Rectangle(0, 0, 10, 10));
        adapter.FillAndDrawEllipse(pen, gradient, new RectangleF(0, 0, 50, 50));
        adapter.FillAndDrawEllipse(pen, gradient, 0, 0, 50, 50);
        adapter.FillAndDrawPath(pen, hatch, path);
        adapter.FillAndDrawPolygon(pen, gradient, points);
        adapter.FillAndDrawPolygon(pen, gradient, pointsF);
        adapter.FillAndDrawRectangle(pen, hatch, 0, 0, 100, 100);
        adapter.FillEllipse(Brushes.Green, 0, 0, 10, 10);
        adapter.FillEllipse(Brushes.Green, new RectangleF(0, 0, 10, 10));
        adapter.FillPath(gradient, path);
        adapter.FillPie(Brushes.Green, 0, 0, 100, 100, 0, 45);
        adapter.FillPolygon(Brushes.Green, pointsF);
        adapter.FillPolygon(Brushes.Green, points);
        adapter.FillRectangle(Brushes.Green, new RectangleF(0, 0, 10, 10));
        adapter.FillRectangle(Brushes.Green, 0, 0, 10, 10);

        AssertValidPdf(pdf.Close());
    }

    [Fact]
    public void TestDrawStringVariants()
    {
        using var pdf = new PdfTestCanvas();
        using var font = new Font("Arial", 12, FontStyle.Bold | FontStyle.Underline | FontStyle.Strikeout);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        var adapter = pdf.Adapter;
        var text = "Hello, World! àèìòù €";

        adapter.DrawString(text, font, Brushes.Black, 10, 10);
        adapter.DrawString(text, font, Brushes.Black, 10, 10, format);
        adapter.DrawString(text, font, Brushes.Black, new PointF(10, 10), format);
        adapter.DrawString(text, font, Brushes.Black, new RectangleF(10, 10, 60, 200));
        adapter.DrawString(text, font, Brushes.Black, new RectangleF(10, 10, 60, 200), format);
        adapter.DrawString(string.Empty, font, Brushes.Black, new RectangleF(10, 10, 60, 200), format);

        adapter.RotateTransform(90);
        adapter.DrawString(text, font, new SolidBrush(Color.FromArgb(100, Color.Red)), new RectangleF(10, -200, 300, 50), format);

        var pdfText = Encoding.Latin1.GetString(AssertValidPdf(pdf.Close()));

        Assert.Contains("/FontFile2", pdfText);
        Assert.Contains("Arial", pdfText);
    }

    [Fact]
    public void TestMeasureCharacterRangesMatchesGdi()
    {
        using var pdf = new PdfTestCanvas();
        using var font = new Font("Arial", 16);
        using var format = new StringFormat();

        var layout = new RectangleF(20, 20, 130, 130);
        var text = "The quick, brown fox easily jumps over the lazy dog.";

        // See microsoft documentation
        // https://learn.microsoft.com/en-us/windows/win32/api/gdiplusgraphics/nf-gdiplusgraphics-graphics-measurecharacterranges
        format.SetMeasurableCharacterRanges([new CharacterRange(3, 5), new CharacterRange(15, 2)]);

        var regions = pdf.Adapter.MeasureCharacterRanges(text, font, layout, format);

        using var gdi = CreateGdiGraphics();
        var expected = gdi.Graphics.MeasureCharacterRanges(text, font, layout, format);

        Assert.Equal(expected.Length, regions.Length);

        for (int i = 0; i < regions.Length; i++)
            Assert.Equal(expected[i].GetBounds(gdi.Graphics), regions[i].GetBounds(gdi.Graphics));
    }

    [Fact]
    public void TestMeasureStringCharsAndLinesFit()
    {
        using var pdf = new PdfTestCanvas();
        using var font = new Font("Arial", 14);
        using var format = new StringFormat();

        // With this layout the string should be wrapped into 2 lines and
        // only 24 characters should fit in the layout width
        var layout = new SizeF(130, 46);

        pdf.Adapter.MeasureString("The quick, brown fox easily jumps over the lazy dog.", font, layout, format, out int charsFit, out int linesFit);

        Assert.Equal(24, charsFit);
        Assert.Equal(2, linesFit);
    }

    [Fact]
    public void TestMeasureStringMatchesGdi()
    {
        using var pdf = new PdfTestCanvas();
        using var font = new Font("Arial", 12);
        using var format = new StringFormat();
        using var gdi = CreateGdiGraphics();

        var text = "Melodic rain bounces off the roof top";
        var g = gdi.Graphics;

        Assert.Equal(g.MeasureString(text, font), pdf.Adapter.MeasureString(text, font));
        Assert.Equal(g.MeasureString(text, font, new SizeF(100, 100)), pdf.Adapter.MeasureString(text, font, new SizeF(100, 100)));
        Assert.Equal(g.MeasureString(text, font, 100, format), pdf.Adapter.MeasureString(text, font, 100, format));
        Assert.Equal(g.MeasureString(text, font, new SizeF(100, 100), format), pdf.Adapter.MeasureString(text, font, new SizeF(100, 100), format));
    }

    [Fact]
    public void TestBasicProperties()
    {
        using var pdf = new PdfTestCanvas();
        var adapter = pdf.Adapter;

        Assert.Equal(96f, adapter.DpiX);
        Assert.Equal(96f, adapter.DpiY);
        Assert.Equal(GraphicsUnit.Pixel, adapter.PageUnit);
        Assert.Throws<NotSupportedException>(() => adapter.PageUnit = GraphicsUnit.Point);
        Assert.Throws<NotSupportedException>(() => adapter.Graphics);

        adapter.SmoothingMode = SmoothingMode.AntiAlias;
        Assert.Equal(SmoothingMode.AntiAlias, adapter.SmoothingMode);
    }

    private static Bitmap CreateGreenBitmap(int width, int height)
    {
        var image = new Bitmap(width, height);

        using var gfx = Graphics.FromImage(image);
        gfx.Clear(Color.Green);

        return image;
    }

    private static byte[] AssertValidPdf(byte[] pdf)
    {
        Assert.True(pdf.Length > 0);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
        Assert.Contains("%%EOF", Encoding.Latin1.GetString(pdf, pdf.Length - 16, 16));

        return pdf;
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0, index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static void AssertMatrixEqual(Matrix expected, Matrix actual)
    {
        var e = expected.Elements;
        var a = actual.Elements;

        for (int i = 0; i < e.Length; i++)
            Assert.Equal(e[i], a[i], Tolerance);
    }

    private sealed class GdiGraphics : IDisposable
    {
        private readonly Bitmap bitmap = new(1, 1);

        public GdiGraphics()
        {
            Graphics = Graphics.FromImage(bitmap);
            Graphics.PageUnit = GraphicsUnit.Pixel;
        }

        public Graphics Graphics { get; }

        public void Dispose()
        {
            Graphics.Dispose();
            bitmap.Dispose();
        }
    }

    private static GdiGraphics CreateGdiGraphics() => new();
}
