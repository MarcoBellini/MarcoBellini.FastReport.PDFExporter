using MarcoBellini.FastReport.PDFExporter.Skia;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Text;

internal sealed class SkiaTextMeasurer(IFontProvider fontProvider) : ITextMeasurer
{
    // Fallbacks for fonts that do not expose underline / strikeout metrics, as fractions of the em size.
    private const float DefaultUnderlinePosition = 0.1f;
    private const float DefaultStrikeoutPosition = -0.3f;
    private const float DefaultLineThickness = 1f / 14f;

    public float MeasureWidth(string text, Font font) =>
        string.IsNullOrEmpty(text) ? 0f : fontProvider.GetFont(font).MeasureText(text);

    public FontLineMetrics GetMetrics(Font font)
    {
        var skFont = fontProvider.GetFont(font);
        var metrics = skFont.Metrics;
        var em = skFont.Size;

        return new FontLineMetrics(
            Ascent: -metrics.Ascent,
            LineHeight: -metrics.Ascent + metrics.Descent + metrics.Leading,
            UnderlinePosition: metrics.UnderlinePosition ?? em * DefaultUnderlinePosition,
            UnderlineThickness: metrics.UnderlineThickness ?? em * DefaultLineThickness,
            StrikeoutPosition: metrics.StrikeoutPosition ?? em * DefaultStrikeoutPosition,
            StrikeoutThickness: metrics.StrikeoutThickness ?? em * DefaultLineThickness);
    }
}
