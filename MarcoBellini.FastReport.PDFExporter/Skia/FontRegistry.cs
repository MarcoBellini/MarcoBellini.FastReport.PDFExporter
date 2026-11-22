using SkiaSharp;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace MarcoBellini.FastReport.PDFExporter.Skia;

/// <summary>
/// Process-wide fonts registered from files with <see cref="PDFExport.AddFontFile(string)"/>.
/// Typefaces live for the whole process and are shared by every export.
/// </summary>
internal static class FontRegistry
{
    private static readonly ConcurrentDictionary<(string Family, bool Bold, bool Italic), SKTypeface> typefaces = new();

    public static void AddFolder(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Font folder not found: {path}");

        foreach (var file in Directory.EnumerateFiles(path))
        {
            var extension = Path.GetExtension(file);

            if (extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase) || extension.Equals(".otf", StringComparison.OrdinalIgnoreCase))
                AddFile(file);
        }
    }

    public static void AddFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Font file not found.", path);

        var typeface = SKTypeface.FromFile(path)
            ?? throw new InvalidDataException($"Unsupported font file: {path}");

        var key = (GetGdiFamilyName(path, typeface).ToUpperInvariant(),
            typeface.FontWeight >= (int)SKFontStyleWeight.SemiBold,
            typeface.FontSlant != SKFontStyleSlant.Upright);

        if (!typefaces.TryAdd(key, typeface))
        {
            typeface.Dispose();
            return;
        }

        // FastReport lays out the report with GDI+: it must see the same font.
        global::FastReport.FontManager.AddFont(path);
    }

    public static bool TryGet(Font font, out SKTypeface typeface) =>
        typefaces.TryGetValue((font.Name.ToUpperInvariant(), font.Bold, font.Italic), out typeface!);

    /// <summary>
    /// The family name GDI+ (and so FastReport) uses for the file, e.g. "Arial Narrow" rather than DirectWrite's "Arial".
    /// </summary>
    private static string GetGdiFamilyName(string path, SKTypeface typeface)
    {
        using var collection = new PrivateFontCollection();

        try
        {
            collection.AddFontFile(path);
        }
        catch (Exception ex) when (ex is ArgumentException or ExternalException or FileNotFoundException)
        {
            // GDI+ rejects some OpenType (CFF) files: fall back to Skia's name.
            return typeface.FamilyName;
        }

        return collection.Families.Length > 0 ? collection.Families[0].Name : typeface.FamilyName;
    }
}
