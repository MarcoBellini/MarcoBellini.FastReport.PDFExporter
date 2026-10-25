using MarcoBellini.FastReport.PDFExporter.Skia.Brushes;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MarcoBellini.FastReport.PDFExporter.Skia;

/// <summary>
/// Default <see cref="IPaintFactory"/>: pens map to stroke paints (width, dash, cap, join, miter),
/// brushes are delegated to the first matching <see cref="IBrushStrategy"/>.
/// </summary>
internal sealed class SkiaPaintFactory : IPaintFactory
{
    // GDI+ dash patterns, expressed in multiples of the pen width.
    private static readonly float[] DashPattern = [3, 1];
    private static readonly float[] DotPattern = [1, 1];
    private static readonly float[] DashDotPattern = [3, 1, 1, 1];
    private static readonly float[] DashDotDotPattern = [3, 1, 1, 1, 1, 1];

    private readonly IReadOnlyList<IBrushStrategy> brushStrategies;

    public SkiaPaintFactory(IReadOnlyList<IBrushStrategy> brushStrategies)
    {
        this.brushStrategies = brushStrategies;
    }

    public static SkiaPaintFactory CreateDefault(IImageConverter imageConverter) => new(
    [
        new SolidBrushStrategy(),
        new LinearGradientBrushStrategy(),
        new PathGradientBrushStrategy(),
        new HatchBrushStrategy(),
        new TextureBrushStrategy(imageConverter)
    ]);

    public SKPaint CreateFill(Brush brush)
    {
        ArgumentNullException.ThrowIfNull(brush);

        var paint = CreatePaint(SKPaintStyle.Fill);
        ApplyBrush(brush, paint);

        return paint;
    }

    public SKPaint CreateStroke(Pen pen)
    {
        ArgumentNullException.ThrowIfNull(pen);

        var paint = CreatePaint(SKPaintStyle.Stroke);

        paint.StrokeWidth = pen.Width;
        paint.StrokeMiter = pen.MiterLimit;
        paint.StrokeJoin = SkiaConvert.ToSKStrokeJoin(pen.LineJoin);
        // Skia supports a single cap per stroke: GDI+ start cap wins when start and end caps differ.
        paint.StrokeCap = SkiaConvert.ToSKStrokeCap(pen.StartCap);
        paint.PathEffect = CreateDashEffect(pen);

        if (pen.PenType == PenType.SolidColor)
        {
            paint.Color = SkiaConvert.ToSKColor(pen.Color);
        }
        else
        {
            using var brush = pen.Brush;
            ApplyBrush(brush, paint);
        }

        return paint;
    }

    private static SKPaint CreatePaint(SKPaintStyle style) => new()
    {
        Style = style,
        IsAntialias = true
    };

    private void ApplyBrush(Brush brush, SKPaint paint)
    {
        var strategy = brushStrategies.FirstOrDefault(s => s.CanHandle(brush));

        if (strategy is not null)
            strategy.Apply(brush, paint);
        else
            paint.Color = SKColors.Black; // Unknown brush type: render with a neutral color instead of failing the export.
    }

    internal static SKPathEffect? CreateDashEffect(Pen pen)
    {
        var pattern = pen.DashStyle switch
        {
            DashStyle.Dash => DashPattern,
            DashStyle.Dot => DotPattern,
            DashStyle.DashDot => DashDotPattern,
            DashStyle.DashDotDot => DashDotDotPattern,
            DashStyle.Custom => pen.DashPattern,
            _ => null
        };

        if (pattern is null || pattern.Length == 0)
            return null;

        // Hairline pens (width 0) still need a visible dash length.
        var unit = Math.Max(pen.Width, 1f);
        var intervals = pattern.Length % 2 == 0 ? pattern : [.. pattern, .. pattern];

        return SKPathEffect.CreateDash(Array.ConvertAll(intervals, v => v * unit), pen.DashOffset * unit);
    }
}
