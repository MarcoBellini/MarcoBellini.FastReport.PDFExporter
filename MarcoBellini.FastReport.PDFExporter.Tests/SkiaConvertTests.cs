using MarcoBellini.FastReport.PDFExporter.Skia;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MarcoBellini.FastReport.PDFExporter.Tests;

public class SkiaConvertTests
{
    [Fact]
    public void TestColorKeepsAlpha()
    {
        var color = SkiaConvert.ToSKColor(Color.FromArgb(128, 10, 20, 30));

        Assert.Equal(new SKColor(10, 20, 30, 128), color);
    }

    [Fact]
    public void TestRectConversion()
    {
        var rect = SkiaConvert.ToSKRect(new RectangleF(10, 20, 30, 40));

        Assert.Equal(SKRect.Create(10, 20, 30, 40), rect);
        Assert.Equal(new RectangleF(10, 20, 30, 40), SkiaConvert.ToRectangleF(rect));
    }

    [Fact]
    public void TestPointArrayConversion()
    {
        var points = SkiaConvert.ToSKPoints([new Point(1, 2), new Point(3, 4)]);

        Assert.Equal([new SKPoint(1, 2), new SKPoint(3, 4)], points);
    }

    [Fact]
    public void TestMatrixMapsPointsLikeGdi()
    {
        using var gdiMatrix = new Matrix();
        gdiMatrix.Translate(15, -7);
        gdiMatrix.Rotate(30);
        gdiMatrix.Scale(2, 3);

        var skMatrix = SkiaConvert.ToSKMatrix(gdiMatrix);

        PointF[] gdiPoints = [new PointF(10, 5)];
        gdiMatrix.TransformPoints(gdiPoints);

        var skPoint = skMatrix.MapPoint(10, 5);

        Assert.Equal(gdiPoints[0].X, skPoint.X, 3);
        Assert.Equal(gdiPoints[0].Y, skPoint.Y, 3);
    }

    [Fact]
    public void TestMatrixRoundTrip()
    {
        using var gdiMatrix = new Matrix(1, 2, 3, 4, 5, 6);
        using var roundTrip = SkiaConvert.ToGdiMatrix(SkiaConvert.ToSKMatrix(gdiMatrix));

        Assert.Equal(gdiMatrix.Elements, roundTrip.Elements);
    }

    [Theory]
    [InlineData(FillMode.Alternate, SKPathFillType.EvenOdd)]
    [InlineData(FillMode.Winding, SKPathFillType.Winding)]
    public void TestFillMode(FillMode fillMode, SKPathFillType expected)
    {
        Assert.Equal(expected, SkiaConvert.ToSKFillType(fillMode));
    }

    [Theory]
    [InlineData(LineCap.Flat, SKStrokeCap.Butt)]
    [InlineData(LineCap.Round, SKStrokeCap.Round)]
    [InlineData(LineCap.Square, SKStrokeCap.Square)]
    [InlineData(LineCap.Triangle, SKStrokeCap.Butt)]
    public void TestLineCap(LineCap cap, SKStrokeCap expected)
    {
        Assert.Equal(expected, SkiaConvert.ToSKStrokeCap(cap));
    }

    [Theory]
    [InlineData(LineJoin.Miter, SKStrokeJoin.Miter)]
    [InlineData(LineJoin.MiterClipped, SKStrokeJoin.Miter)]
    [InlineData(LineJoin.Round, SKStrokeJoin.Round)]
    [InlineData(LineJoin.Bevel, SKStrokeJoin.Bevel)]
    public void TestLineJoin(LineJoin join, SKStrokeJoin expected)
    {
        Assert.Equal(expected, SkiaConvert.ToSKStrokeJoin(join));
    }

    [Theory]
    [InlineData(WrapMode.Clamp, SKShaderTileMode.Clamp)]
    [InlineData(WrapMode.Tile, SKShaderTileMode.Repeat)]
    [InlineData(WrapMode.TileFlipXY, SKShaderTileMode.Mirror)]
    public void TestWrapMode(WrapMode wrapMode, SKShaderTileMode expected)
    {
        Assert.Equal(expected, SkiaConvert.ToSKTileMode(wrapMode));
    }
}
