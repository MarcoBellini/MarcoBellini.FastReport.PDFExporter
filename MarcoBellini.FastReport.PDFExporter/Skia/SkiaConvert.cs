using SkiaSharp;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MarcoBellini.FastReport.PDFExporter.Skia;

/// <summary>
/// Pure conversions between System.Drawing (GDI+) and SkiaSharp value types.
/// </summary>
internal static class SkiaConvert
{
    public static SKColor ToSKColor(Color color) => new(color.R, color.G, color.B, color.A);

    public static SKPoint ToSKPoint(PointF point) => new(point.X, point.Y);

    public static SKPoint[] ToSKPoints(PointF[] points) => Array.ConvertAll(points, ToSKPoint);

    public static SKPoint[] ToSKPoints(Point[] points) => Array.ConvertAll(points, p => new SKPoint(p.X, p.Y));

    public static SKRect ToSKRect(RectangleF rect) => SKRect.Create(rect.X, rect.Y, rect.Width, rect.Height);

    public static SKRect ToSKRect(Rectangle rect) => SKRect.Create(rect.X, rect.Y, rect.Width, rect.Height);

    public static RectangleF ToRectangleF(SKRect rect) => new(rect.Left, rect.Top, rect.Width, rect.Height);

    /// <summary>
    /// GDI+ maps (x, y) to (x*M11 + y*M21 + dx, x*M12 + y*M22 + dy).
    /// </summary>
    public static SKMatrix ToSKMatrix(Matrix matrix)
    {
        var e = matrix.Elements;

        return new SKMatrix(e[0], e[2], e[4],
                            e[1], e[3], e[5],
                            0, 0, 1);
    }

    public static Matrix ToGdiMatrix(SKMatrix matrix) =>
        new(matrix.ScaleX, matrix.SkewY, matrix.SkewX, matrix.ScaleY, matrix.TransX, matrix.TransY);

    public static SKPathFillType ToSKFillType(FillMode fillMode) =>
        fillMode == FillMode.Winding ? SKPathFillType.Winding : SKPathFillType.EvenOdd;

    public static SKShaderTileMode ToSKTileMode(WrapMode wrapMode) => wrapMode switch
    {
        WrapMode.Clamp => SKShaderTileMode.Clamp,
        WrapMode.TileFlipX or WrapMode.TileFlipY or WrapMode.TileFlipXY => SKShaderTileMode.Mirror,
        _ => SKShaderTileMode.Repeat
    };

    public static SKStrokeCap ToSKStrokeCap(LineCap cap) => cap switch
    {
        LineCap.Round or LineCap.RoundAnchor => SKStrokeCap.Round,
        LineCap.Square or LineCap.SquareAnchor => SKStrokeCap.Square,
        _ => SKStrokeCap.Butt
    };

    public static SKStrokeJoin ToSKStrokeJoin(LineJoin join) => join switch
    {
        LineJoin.Round => SKStrokeJoin.Round,
        LineJoin.Bevel => SKStrokeJoin.Bevel,
        _ => SKStrokeJoin.Miter
    };
}
