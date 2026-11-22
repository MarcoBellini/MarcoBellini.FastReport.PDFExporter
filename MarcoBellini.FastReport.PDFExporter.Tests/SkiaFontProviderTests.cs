using MarcoBellini.FastReport.PDFExporter.Skia;
using SkiaSharp;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Tests;

public class SkiaFontProviderTests
{
    private static readonly string FontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    // Arial Narrow ships with Office, not Windows.
    private static bool ArialNarrowInstalled => File.Exists(Path.Combine(FontsFolder, "ARIALNB.TTF"));

    [Fact]
    public void TestArialBoldIsResolvedByName()
    {
        using var provider = new SkiaFontProvider();
        using var font = new Font("Arial", 10, FontStyle.Bold);

        var typeface = provider.GetFont(font).Typeface;

        Assert.Equal("Arial", typeface.FamilyName);
        Assert.Equal((int)SKFontStyleWeight.Bold, typeface.FontWeight);
        Assert.Equal((int)SKFontStyleWidth.Normal, typeface.FontWidth);
    }

    [Fact]
    public void TestLegacyGdiFamilyKeepsWidthAndWeight()
    {
        Assert.SkipUnless(ArialNarrowInstalled, "Arial Narrow is not installed.");

        using var provider = new SkiaFontProvider();
        using var bold = new Font("Arial Narrow", 10, FontStyle.Bold);
        using var regular = new Font("Arial Narrow", 10);

        var boldTypeface = provider.GetFont(bold).Typeface;
        var regularTypeface = provider.GetFont(regular).Typeface;

        Assert.Equal((int)SKFontStyleWeight.Bold, boldTypeface.FontWeight);
        Assert.Equal((int)SKFontStyleWidth.Condensed, boldTypeface.FontWidth);
        Assert.Equal((int)SKFontStyleWeight.Normal, regularTypeface.FontWeight);
        Assert.Equal((int)SKFontStyleWidth.Condensed, regularTypeface.FontWidth);
    }

    [Fact]
    public void TestRegisteredFolderFontIsUsedAndNotDisposed()
    {
        // Italic only: the registry is process-wide and must not hide the GDI path tested above.
        var source = Path.Combine(FontsFolder, "ARIALNI.TTF");
        Assert.SkipUnless(File.Exists(source), "Arial Narrow is not installed.");

        var folder = Directory.CreateTempSubdirectory().FullName;
        File.Copy(source, Path.Combine(folder, "ARIALNI.TTF"));
        File.WriteAllText(Path.Combine(folder, "readme.txt"), "not a font");

        PDFExport.AddFontFolder(folder);

        using var font = new Font("Arial Narrow", 10, FontStyle.Italic);
        Assert.True(FontRegistry.TryGet(font, out var registered));

        SKTypeface resolved;
        using (var provider = new SkiaFontProvider())
            resolved = provider.GetFont(font).Typeface;

        Assert.Same(registered, resolved);
        Assert.Equal((int)SKFontStyleWidth.Condensed, registered.FontWidth);
        Assert.True(registered.GlyphCount > 0, "Registry typeface was disposed with the provider.");
    }

    [Fact]
    public void TestMissingFontFolderThrows()
    {
        Assert.Throws<DirectoryNotFoundException>(() => PDFExport.AddFontFolder(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
        Assert.Throws<FileNotFoundException>(() => PDFExport.AddFontFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ttf")));
    }
}
