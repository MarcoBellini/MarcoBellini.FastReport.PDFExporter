using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Text;

/// <summary>
/// Vertical font metrics in FastReport pixels. Positions are offsets from the baseline (positive = downward).
/// </summary>
internal readonly record struct FontLineMetrics(
    float Ascent,
    float LineHeight,
    float UnderlinePosition,
    float UnderlineThickness,
    float StrikeoutPosition,
    float StrikeoutThickness);

/// <summary>
/// Measures text with the same font engine used to draw it, so that laid-out lines never overflow.
/// </summary>
internal interface ITextMeasurer
{
    float MeasureWidth(string text, Font font);

    FontLineMetrics GetMetrics(Font font);
}
