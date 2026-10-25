using SkiaSharp;
using System.Security.Cryptography;

namespace MarcoBellini.FastReport.PDFExporter.Text;

/// <summary>
/// Records the glyphs drawn with each font program, so that embedded fonts can be subset after the PDF is written.
/// Font programs are identified by a hash of their data, computed when a typeface is first used (while it is
/// guaranteed to be alive); typefaces sharing the same font file (e.g. synthetic bold) are merged.
/// </summary>
internal sealed class GlyphUsageTracker
{
    private readonly Dictionary<SKTypeface, string?> fontDataKeys = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, HashSet<ushort>> usage = [];

    public void Register(SKFont font, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var key = GetFontDataKey(font.Typeface);

        if (key is null)
            return;

        if (!usage.TryGetValue(key, out var glyphs))
        {
            glyphs = [];
            usage.Add(key, glyphs);
        }

        glyphs.UnionWith(font.GetGlyphs(text));
    }

    /// <summary>
    /// Used glyphs keyed by <see cref="GetFontDataKey(ReadOnlySpan{byte})"/>.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlySet<ushort>> GetUsageByFontData() =>
        usage.ToDictionary(p => p.Key, p => (IReadOnlySet<ushort>)p.Value);

    public static string GetFontDataKey(ReadOnlySpan<byte> fontData) => Convert.ToHexString(SHA256.HashData(fontData));

    private string? GetFontDataKey(SKTypeface typeface)
    {
        if (fontDataKeys.TryGetValue(typeface, out var key))
            return key;

        var data = ReadFontData(typeface);
        key = data is null ? null : GetFontDataKey(data);

        fontDataKeys.Add(typeface, key);

        return key;
    }

    private static byte[]? ReadFontData(SKTypeface typeface)
    {
        using var stream = typeface.OpenStream(out _);

        if (stream is null)
            return null;

        using var data = SKData.Create(stream);

        return data?.ToArray();
    }
}
