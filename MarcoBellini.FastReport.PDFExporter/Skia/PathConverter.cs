using SkiaSharp;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MarcoBellini.FastReport.PDFExporter.Skia;

/// <summary>
/// Builds <see cref="SKPath"/> instances from GDI+ geometry.
/// </summary>
internal static class PathConverter
{
    private const byte PathTypeMask = (byte)PathPointType.PathTypeMask;
    private const byte CloseSubpathFlag = (byte)PathPointType.CloseSubpath;

    /// <summary>
    /// Convert a GDI+ path (Start / Line / Bezier3 points plus close flags) into an SKPath.
    /// </summary>
    public static SKPath FromGraphicsPath(GraphicsPath gdiPath)
    {
        using var builder = new SKPathBuilder { FillType = SkiaConvert.ToSKFillType(gdiPath.FillMode) };

        if (gdiPath.PointCount == 0)
            return builder.Detach();

        var points = gdiPath.PathPoints;
        var types = gdiPath.PathTypes;

        for (int i = 0; i < points.Length; i++)
        {
            var type = types[i] & PathTypeMask;

            if (type == (byte)PathPointType.Start)
            {
                builder.MoveTo(points[i].X, points[i].Y);
            }
            else if (type == (byte)PathPointType.Bezier3 && i + 2 < points.Length)
            {
                builder.CubicTo(points[i].X, points[i].Y,
                                points[i + 1].X, points[i + 1].Y,
                                points[i + 2].X, points[i + 2].Y);
                i += 2;
            }
            else
            {
                builder.LineTo(points[i].X, points[i].Y);
            }

            if ((types[i] & CloseSubpathFlag) != 0)
                builder.Close();
        }

        return builder.Detach();
    }

    public static SKPath Polygon(SKPoint[] points, bool close, SKPathFillType fillType = SKPathFillType.EvenOdd)
    {
        using var builder = new SKPathBuilder { FillType = fillType };

        if (points.Length > 0)
            builder.AddPoly(points, close);

        return builder.Detach();
    }

    public static SKPath Rectangle(SKRect rect)
    {
        using var builder = new SKPathBuilder();
        builder.AddRect(rect, SKPathDirection.Clockwise);
        return builder.Detach();
    }

    public static SKPath Ellipse(SKRect rect)
    {
        using var builder = new SKPathBuilder();
        builder.AddOval(rect, SKPathDirection.Clockwise);
        return builder.Detach();
    }

    public static SKPath Arc(SKRect rect, float startAngle, float sweepAngle)
    {
        using var builder = new SKPathBuilder();
        builder.ArcTo(rect, startAngle, sweepAngle, true);
        return builder.Detach();
    }

    public static SKPath Pie(SKRect rect, float startAngle, float sweepAngle)
    {
        using var builder = new SKPathBuilder();
        builder.MoveTo(rect.MidX, rect.MidY);
        builder.ArcTo(rect, startAngle, sweepAngle, false);
        builder.Close();
        return builder.Detach();
    }

    /// <summary>
    /// Cardinal spline, converted to Béziers by GDI+ itself to match its exact geometry.
    /// </summary>
    public static SKPath Curve(PointF[] points, int offset, int numberOfSegments, float tension)
    {
        using var gdiPath = new GraphicsPath();
        gdiPath.AddCurve(points, offset, numberOfSegments, tension);
        return FromGraphicsPath(gdiPath);
    }

    /// <summary>
    /// Vector representation of a region as the union of its scan rectangles.
    /// </summary>
    public static SKPath FromRegion(Region region)
    {
        using var identity = new Matrix();
        using var builder = new SKPathBuilder { FillType = SKPathFillType.Winding };

        foreach (var scan in region.GetRegionScans(identity))
            builder.AddRect(SkiaConvert.ToSKRect(scan), SKPathDirection.Clockwise);

        return builder.Detach();
    }
}
