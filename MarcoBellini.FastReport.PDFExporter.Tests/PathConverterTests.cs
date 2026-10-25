using MarcoBellini.FastReport.PDFExporter.Skia;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MarcoBellini.FastReport.PDFExporter.Tests;

public class PathConverterTests
{
    [Fact]
    public void TestGraphicsPathWithLinesBeziersAndClose()
    {
        using var gdiPath = new GraphicsPath(FillMode.Winding);
        gdiPath.AddLine(10, 10, 100, 10);
        gdiPath.AddBezier(100, 10, 120, 40, 80, 60, 100, 90);
        gdiPath.CloseFigure();
        gdiPath.AddEllipse(200, 200, 50, 30);

        using var path = PathConverter.FromGraphicsPath(gdiPath);

        Assert.Equal(SKPathFillType.Winding, path.FillType);
        AssertBoundsEqual(gdiPath.GetBounds(), path.Bounds);
    }

    [Fact]
    public void TestEmptyGraphicsPath()
    {
        using var gdiPath = new GraphicsPath();
        using var path = PathConverter.FromGraphicsPath(gdiPath);

        Assert.True(path.IsEmpty);
    }

    [Fact]
    public void TestPolygonIsClosedEvenOdd()
    {
        using var path = PathConverter.Polygon([new SKPoint(0, 0), new SKPoint(10, 0), new SKPoint(10, 10)], true);

        Assert.Equal(SKPathFillType.EvenOdd, path.FillType);
        Assert.Equal(SKRect.Create(0, 0, 10, 10), path.Bounds);
    }

    [Fact]
    public void TestEllipseAndRectangleBounds()
    {
        var rect = SKRect.Create(5, 6, 70, 80);

        using var ellipse = PathConverter.Ellipse(rect);
        using var rectangle = PathConverter.Rectangle(rect);

        Assert.Equal(rect, ellipse.Bounds);
        Assert.Equal(rect, rectangle.Bounds);
    }

    [Fact]
    public void TestPieStartsFromCenter()
    {
        using var pie = PathConverter.Pie(SKRect.Create(0, 0, 100, 100), 0, 90);

        Assert.Equal(new SKPoint(50, 50), pie.Points[0]);
        AssertBoundsEqual(new RectangleF(50, 50, 50, 50), pie.TightBounds);
    }

    [Fact]
    public void TestArcBounds()
    {
        using var arc = PathConverter.Arc(SKRect.Create(0, 0, 100, 100), 0, 180);

        AssertBoundsEqual(new RectangleF(0, 50, 100, 50), arc.TightBounds);
    }

    [Fact]
    public void TestCurveMatchesGdiGeometry()
    {
        PointF[] points = [new(0, 0), new(50, 80), new(100, 20), new(150, 60)];

        using var gdiPath = new GraphicsPath();
        gdiPath.AddCurve(points, 0, 3, 0.5f);

        using var path = PathConverter.Curve(points, 0, 3, 0.5f);

        AssertBoundsEqual(gdiPath.GetBounds(), path.Bounds);
    }

    [Fact]
    public void TestRegionIsVectorized()
    {
        using var region = new Region(new Rectangle(10, 10, 100, 100));
        region.Union(new Rectangle(150, 10, 20, 20));

        using var path = PathConverter.FromRegion(region);

        Assert.Equal(SKRect.Create(10, 10, 160, 100), path.Bounds);
        Assert.True(path.Contains(50, 50));
        Assert.True(path.Contains(160, 20));
        Assert.False(path.Contains(130, 50));
    }

    private static void AssertBoundsEqual(RectangleF expected, SKRect actual)
    {
        Assert.Equal(expected.Left, actual.Left, 1);
        Assert.Equal(expected.Top, actual.Top, 1);
        Assert.Equal(expected.Right, actual.Right, 1);
        Assert.Equal(expected.Bottom, actual.Bottom, 1);
    }
}
