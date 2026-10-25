using SkiaSharp;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Skia;

/// <summary>
/// Creates Skia paints from GDI+ pens and brushes. The caller owns and must dispose the returned paint.
/// </summary>
internal interface IPaintFactory
{
    SKPaint CreateStroke(Pen pen);

    SKPaint CreateFill(Brush brush);
}
