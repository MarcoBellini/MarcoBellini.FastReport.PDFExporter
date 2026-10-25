using SkiaSharp;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Skia.Brushes;

internal sealed class SolidBrushStrategy : BrushStrategy<SolidBrush>
{
    protected override void Apply(SolidBrush brush, SKPaint paint)
    {
        paint.Color = SkiaConvert.ToSKColor(brush.Color);
    }
}
