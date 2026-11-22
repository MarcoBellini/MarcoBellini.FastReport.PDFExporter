using SkiaSharp;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Skia;

/// <summary>
/// Resolves fonts in order: fonts registered with <see cref="PDFExport.AddFontFile(string)"/>,
/// the platform font manager (<see cref="SKTypeface.FromFamilyName(string, SKFontStyle)"/>), then GDI for legacy family names.
/// Skia's PDF backend embeds the resolved fonts.
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

        var skFont = new SKFont(GetTypeface(font), size)
        {
            Subpixel = true,
            LinearMetrics = true,
            Hinting = SKFontHinting.None,
            Edging = SKFontEdging.Antialias
        };

        fonts.Add(key, skFont);

        return skFont;
    }

    private SKTypeface GetTypeface(Font font)
    {
        // Registered typefaces are owned by the registry: not cached here, so Dispose leaves them alone.
        if (FontRegistry.TryGet(font, out var registered))
            return registered;

        var key = (font.Name, font.Bold, font.Italic);

        if (typefaces.TryGetValue(key, out var cached))
            return cached;

        var style = new SKFontStyle(
            font.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            font.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

        var typeface = SKTypeface.FromFamilyName(font.Name, style) ?? SKTypeface.CreateDefault();

        // DirectWrite does not know legacy GDI family names ("Arial Narrow") and returns a fallback: ask GDI for the exact file.
        if (!string.Equals(typeface.FamilyName, font.Name, StringComparison.OrdinalIgnoreCase) && GdiFontLoader.TryLoad(font) is { } gdiTypeface)
        {
            typeface.Dispose();
            typeface = gdiTypeface;
        }

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
