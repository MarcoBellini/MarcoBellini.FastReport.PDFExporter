using SkiaSharp;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Skia.Brushes;

/// <summary>
/// Type-safe base class for brush strategies.
/// </summary>
internal abstract class BrushStrategy<TBrush> : IBrushStrategy where TBrush : Brush
{
    public bool CanHandle(Brush brush) => brush is TBrush;

    public void Apply(Brush brush, SKPaint paint) => Apply((TBrush)brush, paint);

    protected abstract void Apply(TBrush brush, SKPaint paint);
}
