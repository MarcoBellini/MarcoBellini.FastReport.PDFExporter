using SkiaSharp;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Skia;

/// <summary>
/// Resolves fonts through the platform font manager (<see cref="SKTypeface.FromFamilyName(string, SKFontStyle)"/>).
/// Skia's PDF backend embeds and subsets the resolved fonts automatically.
/// </summary>
internal sealed class SkiaFontProvider : IFontProvider, IDisposable
{
    private readonly Dictionary<(string Family, bool Bold, bool Italic), SKTypeface> typefaces = [];
    private readonly Dictionary<(string Family, bool Bold, bool Italic, float Size), SKFont> fonts = [];

    public SKFont GetFont(Font font)
    {
        ArgumentNullException.ThrowIfNull(font);

        var size = PageUnits.FontPixelSize(font);
        var key = (font.Name, font.Bold, font.Italic, size);

        if (fonts.TryGetValue(key, out var cached))
            return cached;

        var skFont = new SKFont(GetTypeface(font.Name, font.Bold, font.Italic), size)
        {
            Subpixel = true,
            LinearMetrics = true,
            Hinting = SKFontHinting.None,
            Edging = SKFontEdging.Antialias
        };

        fonts.Add(key, skFont);

        return skFont;
    }

    private SKTypeface GetTypeface(string family, bool bold, bool italic)
    {
        var key = (family, bold, italic);

        if (typefaces.TryGetValue(key, out var cached))
            return cached;

        var style = new SKFontStyle(
            bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

        var typeface = SKTypeface.FromFamilyName(family, style) ?? SKTypeface.CreateDefault();

        typefaces.Add(key, typeface);

        return typeface;
    }

    public void Dispose()
    {
        foreach (var font in fonts.Values)
            font.Dispose();

        foreach (var typeface in typefaces.Values)
            typeface.Dispose();

        fonts.Clear();
        typefaces.Clear();
    }
}
