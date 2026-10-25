using SkiaSharp;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Skia;

/// <summary>
/// Resolves GDI+ fonts into Skia fonts sized in FastReport pixels (96 DPI).
/// The returned font is owned and cached by the provider: callers must not modify or dispose it.
/// </summary>
internal interface IFontProvider
{
    SKFont GetFont(Font font);
}
