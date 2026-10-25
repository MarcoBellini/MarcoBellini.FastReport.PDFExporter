using SkiaSharp;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MarcoBellini.FastReport.PDFExporter.Skia.Brushes;

/// <summary>
/// GDI+ linear gradients run horizontally across <see cref="LinearGradientBrush.Rectangle"/> in brush space;
/// the angle is stored in <see cref="LinearGradientBrush.Transform"/>, which becomes the shader local matrix.
/// Blend factors and interpolation colors are converted into multi-stop gradients.
/// </summary>
internal sealed class LinearGradientBrushStrategy : BrushStrategy<LinearGradientBrush>
{
    protected override void Apply(LinearGradientBrush brush, SKPaint paint)
    {
        var rect = brush.Rectangle;
        var start = new SKPoint(rect.Left, rect.Top);
        var end = new SKPoint(rect.Right, rect.Top);

        var (colors, positions) = GetStops(brush);

        using var matrix = brush.Transform;

        // Clamp instead of the GDI+ wrap mode: Skia writes repeating gradients as PostScript-function shadings
        // (FunctionType 4) that many viewers cannot render, while clamped gradients become standard axial shadings.
        // FastReport always fills the brush rectangle, so the result is the same.
        paint.Color = SKColors.Black;
        paint.Shader = SKShader.CreateLinearGradient(start, end, colors, positions,
            SKShaderTileMode.Clamp, SkiaConvert.ToSKMatrix(matrix));
    }

    internal static (SKColor[] Colors, float[] Positions) GetStops(LinearGradientBrush brush)
    {
        var interpolation = brush.InterpolationColors;

        if (interpolation.Colors.Length > 1)
            return (Array.ConvertAll(interpolation.Colors, SkiaConvert.ToSKColor), interpolation.Positions);

        var startColor = brush.LinearColors[0];
        var endColor = brush.LinearColors[1];
        var blend = brush.Blend;

        if (blend is null || blend.Factors.Length <= 1)
            return ([SkiaConvert.ToSKColor(startColor), SkiaConvert.ToSKColor(endColor)], [0f, 1f]);

        var colors = Array.ConvertAll(blend.Factors, f => SkiaConvert.ToSKColor(Lerp(startColor, endColor, f)));

        return (colors, blend.Positions);
    }

    private static Color Lerp(Color from, Color to, float factor) => Color.FromArgb(
        LerpChannel(from.A, to.A, factor),
        LerpChannel(from.R, to.R, factor),
        LerpChannel(from.G, to.G, factor),
        LerpChannel(from.B, to.B, factor));

    private static int LerpChannel(int from, int to, float factor) =>
        Math.Clamp((int)Math.Round(from + (to - from) * factor), 0, 255);
}
