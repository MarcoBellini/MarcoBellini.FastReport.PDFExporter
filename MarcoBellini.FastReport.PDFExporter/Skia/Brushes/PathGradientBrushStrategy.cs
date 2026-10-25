using SkiaSharp;
using System.Drawing.Drawing2D;

namespace MarcoBellini.FastReport.PDFExporter.Skia.Brushes;

/// <summary>
/// Approximates a GDI+ path gradient with a radial gradient from the center color to the first surround color,
/// stretched over the brush bounds.
/// </summary>
internal sealed class PathGradientBrushStrategy : BrushStrategy<PathGradientBrush>
{
    protected override void Apply(PathGradientBrush brush, SKPaint paint)
    {
        var bounds = brush.Rectangle;
        var center = brush.CenterPoint;
        var surround = brush.SurroundColors.Length > 0 ? brush.SurroundColors[0] : brush.CenterColor;

        var radiusX = Math.Max(bounds.Width / 2f, 0.001f);
        var radiusY = Math.Max(bounds.Height / 2f, 0.001f);

        using var brushTransform = brush.Transform;

        // Unit-radius circle scaled to the ellipse that fits the brush bounds.
        var localMatrix = SKMatrix.CreateScale(radiusX, radiusY, center.X, center.Y)
            .PostConcat(SkiaConvert.ToSKMatrix(brushTransform));

        paint.Color = SKColors.Black;
        paint.Shader = SKShader.CreateRadialGradient(
            new SKPoint(center.X, center.Y), 1f,
            [SkiaConvert.ToSKColor(brush.CenterColor), SkiaConvert.ToSKColor(surround)],
            [0f, 1f], SKShaderTileMode.Clamp, localMatrix);
    }
}
