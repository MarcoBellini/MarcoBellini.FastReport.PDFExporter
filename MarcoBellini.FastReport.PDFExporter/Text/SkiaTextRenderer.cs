using MarcoBellini.FastReport.PDFExporter.Skia;
using SkiaSharp;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Text;

/// <summary>
/// Draws a <see cref="TextLayout"/> on a Skia canvas, including underline and strikeout decorations,
/// and records the drawn glyphs for font subsetting.
/// </summary>
internal sealed class SkiaTextRenderer(IFontProvider fontProvider, GlyphUsageTracker glyphUsage)
{
    public void Draw(SKCanvas canvas, TextLayout layout, Font font, SKPaint paint)
    {
        var skFont = fontProvider.GetFont(font);
        var metrics = layout.Metrics;

        foreach (var line in layout.Lines)
        {
            if (line.Text.Length == 0)
                continue;

            canvas.DrawText(line.Text, line.X, line.Baseline, SKTextAlign.Left, skFont, paint);
            glyphUsage.Register(skFont, line.Text);

            if (font.Underline)
                DrawDecoration(canvas, line, metrics.UnderlinePosition, metrics.UnderlineThickness, paint);

            if (font.Strikeout)
                DrawDecoration(canvas, line, metrics.StrikeoutPosition, metrics.StrikeoutThickness, paint);
        }
    }

    private static void DrawDecoration(SKCanvas canvas, TextLine line, float offset, float thickness, SKPaint paint)
    {
        var rect = SKRect.Create(line.X, line.Baseline + offset, line.Width, thickness);
        canvas.DrawRect(rect, paint);
    }
}
