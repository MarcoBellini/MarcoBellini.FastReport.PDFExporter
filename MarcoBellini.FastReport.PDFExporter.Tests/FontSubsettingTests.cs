using MarcoBellini.FastReport.PDFExporter.Pdf;
using MarcoBellini.FastReport.PDFExporter.Text;
using SkiaSharp;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace MarcoBellini.FastReport.PDFExporter.Tests;

public partial class FontSubsettingTests
{
    private static readonly string ArialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

    [Fact]
    public void TestSubsetKeepsGlyphIdsAndDropsUnusedOutlines()
    {
        var font = File.ReadAllBytes(ArialPath);

        using var original = SKTypeface.FromData(SKData.CreateCopy(font));
        using var originalFont = new SKFont(original, 12);
        var glyphA = originalFont.GetGlyphs("A")[0];
        var glyphB = originalFont.GetGlyphs("B")[0];

        var subset = TrueTypeSubsetter.Subset(font, new HashSet<ushort> { glyphA });

        Assert.NotNull(subset);
        Assert.True(subset.Length < font.Length / 4, $"Subset size {subset.Length} vs {font.Length}");

        using var subsetTypeface = SKTypeface.FromData(SKData.CreateCopy(subset));
        Assert.NotNull(subsetTypeface);

        using var subsetFont = new SKFont(subsetTypeface, 12);
        Assert.Equal(original.GlyphCount, subsetTypeface.GlyphCount);

        using var pathA = subsetFont.GetGlyphPath(glyphA);
        using var pathB = subsetFont.GetGlyphPath(glyphB);

        Assert.False(pathA.IsEmpty);
        Assert.True(pathB is null || pathB.IsEmpty);
    }

    [Fact]
    public void TestSubsetChecksumAdjustmentIsValid()
    {
        var subset = TrueTypeSubsetter.Subset(File.ReadAllBytes(ArialPath), new HashSet<ushort> { 36 });

        Assert.NotNull(subset);
        Assert.Equal(0xB1B0AFBA, TrueTypeSubsetter.Checksum(subset));
    }

    [Fact]
    public void TestUnsupportedFontReturnsNull()
    {
        Assert.Null(TrueTypeSubsetter.Subset([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13], new HashSet<ushort>()));
        Assert.Null(TrueTypeSubsetter.Subset("OTTO"u8.ToArray().Concat(new byte[20]).ToArray(), new HashSet<ushort>()));
    }

    [Fact]
    public void TestChecksum()
    {
        Assert.Equal(0x01020304u + 0x05000000u, TrueTypeSubsetter.Checksum([1, 2, 3, 4, 5]));
    }

    [Fact]
    public void TestGlyphUsageTrackerGroupsByFontData()
    {
        var tracker = new GlyphUsageTracker();

        using var typeface = SKTypeface.FromFile(ArialPath);
        using var small = new SKFont(typeface, 10);
        using var large = new SKFont(typeface, 20);

        tracker.Register(small, "AB");
        tracker.Register(large, "BC");
        tracker.Register(large, string.Empty);

        var usage = tracker.GetUsageByFontData();
        var key = GlyphUsageTracker.GetFontDataKey(File.ReadAllBytes(ArialPath));

        Assert.Single(usage);
        Assert.Equal(new HashSet<ushort>(small.GetGlyphs("ABC")), usage[key]);
    }

    [Fact]
    public void TestXrefRewriterIdentityKeepsDocumentValid()
    {
        var pdf = CreatePdfWithText("Identity");

        var rewritten = PdfXrefRewriter.Rewrite(pdf, _ => null);

        Assert.Equal(pdf, rewritten);
    }

    [Fact]
    public void TestXrefRewriterUpdatesOffsets()
    {
        var pdf = CreatePdfWithText("Offsets");

        // Grow every object by a comment line: all following offsets must move.
        var rewritten = PdfXrefRewriter.Rewrite(pdf, obj => [.. obj.ToArray(), .. "% padding\n"u8.ToArray()]);
        var text = Encoding.Latin1.GetString(rewritten);

        foreach (Match entry in XrefEntryRegex().Matches(text))
        {
            var offset = int.Parse(entry.Groups[1].Value);
            Assert.Matches(@"^\d+ 0 obj", text.Substring(offset, 12));
        }

        var startxref = int.Parse(StartXrefRegex().Match(text).Groups[1].Value);
        Assert.StartsWith("xref", text[startxref..]);
    }

    [Fact]
    public void TestXrefRewriterIgnoresInvalidInput()
    {
        var invalid = "not a pdf"u8.ToArray();

        Assert.Same(invalid, PdfXrefRewriter.Rewrite(invalid, _ => []));
    }

    [Fact]
    public void TestPostProcessorSubsetsEmbeddedFont()
    {
        var tracker = new GlyphUsageTracker();
        var pdf = CreatePdfWithText("Subset me", tracker);

        var processed = new FontSubsetPostProcessor(tracker).Process(pdf);

        var originalLength1 = GetLength1(pdf);
        var subsetLength1 = GetLength1(processed);

        Assert.True(processed.Length < pdf.Length / 4, $"{processed.Length} vs {pdf.Length}");
        Assert.True(subsetLength1 < originalLength1);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(processed, 0, 5));
    }

    [Fact]
    public void TestPostProcessorWithoutTextReturnsSameDocument()
    {
        var pdf = CreatePdfWithText(null);

        Assert.Same(pdf, new FontSubsetPostProcessor(new GlyphUsageTracker()).Process(pdf));
    }

    private static byte[] CreatePdfWithText(string? text, GlyphUsageTracker? tracker = null)
    {
        using var stream = new MemoryStream();
        using (var document = SKDocument.CreatePdf(stream))
        {
            var canvas = document.BeginPage(200, 200);

            if (text is not null)
            {
                using var typeface = SKTypeface.FromFile(ArialPath);
                using var font = new SKFont(typeface, 12);
                using var paint = new SKPaint { Color = SKColors.Black };

                canvas.DrawText(text, 10, 50, SKTextAlign.Left, font, paint);
                tracker?.Register(font, text);
            }

            document.EndPage();
            document.Close();
        }

        return stream.ToArray();
    }

    private static int GetLength1(byte[] pdf) =>
        int.Parse(Length1Regex().Match(Encoding.Latin1.GetString(pdf)).Groups[1].Value);

    [GeneratedRegex(@"/Length1 (\d+)")]
    private static partial Regex Length1Regex();

    [GeneratedRegex(@"(\d{10}) 00000 n ")]
    private static partial Regex XrefEntryRegex();

    [GeneratedRegex(@"startxref\s+(\d+)")]
    private static partial Regex StartXrefRegex();
}
