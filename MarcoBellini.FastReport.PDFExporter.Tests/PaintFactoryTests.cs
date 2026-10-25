using MarcoBellini.FastReport.PDFExporter.Skia;
using MarcoBellini.FastReport.PDFExporter.Skia.Brushes;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MarcoBellini.FastReport.PDFExporter.Tests;

public sealed class PaintFactoryTests : IDisposable
{
    private readonly GdiImageConverter imageConverter = new();
    private readonly SkiaPaintFactory factory;

    public PaintFactoryTests()
    {
        factory = SkiaPaintFactory.CreateDefault(imageConverter);
    }

    public void Dispose() => imageConverter.Dispose();

    [Fact]
    public void TestStrokeFromPen()
    {
        using var pen = new Pen(Color.FromArgb(200, Color.Red), 3)
        {
            LineJoin = LineJoin.Round,
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            MiterLimit = 7
        };

        using var paint = factory.CreateStroke(pen);

        Assert.Equal(SKPaintStyle.Stroke, paint.Style);
        Assert.Equal(3f, paint.StrokeWidth);
        Assert.Equal(7f, paint.StrokeMiter);
        Assert.Equal(SKStrokeJoin.Round, paint.StrokeJoin);
        Assert.Equal(SKStrokeCap.Round, paint.StrokeCap);
        Assert.Equal(new SKColor(255, 0, 0, 200), paint.Color);
        Assert.Null(paint.PathEffect);
    }

    [Theory]
    [InlineData(DashStyle.Dash)]
    [InlineData(DashStyle.Dot)]
    [InlineData(DashStyle.DashDot)]
    [InlineData(DashStyle.DashDotDot)]
    public void TestDashedPenCreatesPathEffect(DashStyle dashStyle)
    {
        using var pen = new Pen(Color.Black, 2) { DashStyle = dashStyle };
        using var paint = factory.CreateStroke(pen);

        Assert.NotNull(paint.PathEffect);
    }

    [Fact]
    public void TestCustomDashPattern()
    {
        using var pen = new Pen(Color.Black, 0) { DashPattern = [4, 2, 1] };

        Assert.NotNull(SkiaPaintFactory.CreateDashEffect(pen));
    }

    [Fact]
    public void TestDifferentCapsDoNotThrow()
    {
        using var pen = new Pen(Color.Black, 1) { StartCap = LineCap.Square, EndCap = LineCap.ArrowAnchor };
        using var paint = factory.CreateStroke(pen);

        Assert.Equal(SKStrokeCap.Square, paint.StrokeCap);
    }

    [Fact]
    public void TestPenWithGradientBrush()
    {
        using var brush = new LinearGradientBrush(new Rectangle(0, 0, 10, 10), Color.Red, Color.Blue, 0f);
        using var pen = new Pen(brush, 2);
        using var paint = factory.CreateStroke(pen);

        Assert.NotNull(paint.Shader);
    }

    [Fact]
    public void TestSolidBrush()
    {
        using var brush = new SolidBrush(Color.FromArgb(64, 1, 2, 3));
        using var paint = factory.CreateFill(brush);

        Assert.Equal(SKPaintStyle.Fill, paint.Style);
        Assert.Equal(new SKColor(1, 2, 3, 64), paint.Color);
        Assert.Null(paint.Shader);
    }

    [Fact]
    public void TestLinearGradientTwoStops()
    {
        using var brush = new LinearGradientBrush(new Rectangle(0, 0, 100, 50), Color.Red, Color.Blue, 90f);

        var (colors, positions) = LinearGradientBrushStrategy.GetStops(brush);

        Assert.Equal([SKColors.Red, SKColors.Blue], colors);
        Assert.Equal([0f, 1f], positions);
    }

    [Fact]
    public void TestLinearGradientWithBlendBecomesMultiStop()
    {
        using var brush = new LinearGradientBrush(new Rectangle(0, 0, 100, 50), Color.Red, Color.Blue, 0f);
        brush.SetSigmaBellShape(0.5f, 1f);

        var (colors, positions) = LinearGradientBrushStrategy.GetStops(brush);

        Assert.True(colors.Length > 2);
        Assert.Equal(colors.Length, positions.Length);
    }

    [Fact]
    public void TestLinearGradientWithInterpolationColors()
    {
        using var brush = new LinearGradientBrush(new Rectangle(0, 0, 100, 50), Color.Red, Color.Blue, 0f)
        {
            InterpolationColors = new ColorBlend
            {
                Colors = [Color.Red, Color.Lime, Color.Blue],
                Positions = [0f, 0.5f, 1f]
            }
        };

        var (colors, positions) = LinearGradientBrushStrategy.GetStops(brush);

        Assert.Equal([SKColors.Red, SKColors.Lime, SKColors.Blue], colors);
        Assert.Equal([0f, 0.5f, 1f], positions);
    }

    [Fact]
    public void TestShaderBrushes()
    {
        using var hatch = new HatchBrush(HatchStyle.DiagonalCross, Color.Black, Color.White);
        using var bitmap = new Bitmap(4, 4);
        using var texture = new TextureBrush(bitmap);
        using var pathBrush = new PathGradientBrush([new PointF(0, 0), new PointF(10, 0), new PointF(5, 10)])
        {
            CenterColor = Color.White,
            SurroundColors = [Color.Black]
        };
        using var gradient = new LinearGradientBrush(new Rectangle(0, 0, 10, 10), Color.Red, Color.Blue, 45f);

        foreach (Brush brush in new Brush[] { hatch, texture, pathBrush, gradient })
        {
            using var paint = factory.CreateFill(brush);
            Assert.NotNull(paint.Shader);
        }
    }
}
