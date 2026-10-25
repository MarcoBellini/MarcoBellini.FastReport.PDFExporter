using SkiaSharp;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Skia.Brushes;

/// <summary>
/// Translates one kind of GDI+ <see cref="Brush"/> into the color / shader of an <see cref="SKPaint"/>.
/// New brush types are supported by adding a strategy, without touching the paint factory.
/// </summary>
internal interface IBrushStrategy
{
    bool CanHandle(Brush brush);

    void Apply(Brush brush, SKPaint paint);
}
