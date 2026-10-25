using MarcoBellini.FastReport.PDFExporter.Text;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace MarcoBellini.FastReport.PDFExporter.Pdf;

/// <summary>
/// The Skia native build shipped with SkiaSharp embeds TrueType fonts without subsetting them.
/// This post-processor finds every embedded <c>FontFile2</c> stream, matches it with the glyphs recorded by
/// <see cref="GlyphUsageTracker"/> and replaces it with a glyph-preserving subset.
/// </summary>
internal sealed partial class FontSubsetPostProcessor(GlyphUsageTracker tracker) : IPdfPostProcessor
{
    private static readonly Encoding Latin1 = Encoding.Latin1;
    private static readonly byte[] StreamKeyword = Latin1.GetBytes("stream");

    public byte[] Process(byte[] pdf)
    {
        var usage = tracker.GetUsageByFontData();

        if (usage.Count == 0)
            return pdf;

        return PdfXrefRewriter.Rewrite(pdf, obj => SubsetFontStream(obj, usage));
    }

    private static byte[]? SubsetFontStream(ReadOnlyMemory<byte> obj, IReadOnlyDictionary<string, IReadOnlySet<ushort>> usage)
    {
        var span = obj.Span;
        var keywordIndex = span.IndexOf(StreamKeyword);

        if (keywordIndex < 0)
            return null;

        var header = Latin1.GetString(span[..keywordIndex]);

        if (!Length1Regex().IsMatch(header))
            return null;

        var lengthMatch = LengthRegex().Match(header);

        if (!lengthMatch.Success)
            return null;

        var dataStart = SkipEndOfLine(span, keywordIndex + StreamKeyword.Length);
        var length = int.Parse(lengthMatch.Groups[1].Value);

        if (dataStart < 0 || dataStart + length > span.Length)
            return null;

        var isFlate = header.Contains("/FlateDecode", StringComparison.Ordinal);
        var encoded = span.Slice(dataStart, length);
        var font = isFlate ? Inflate(encoded) : encoded.ToArray();

        if (font is null || !usage.TryGetValue(GlyphUsageTracker.GetFontDataKey(font), out var glyphs))
            return null;

        var subset = TrueTypeSubsetter.Subset(font, glyphs);

        if (subset is null)
            return null;

        var newData = isFlate ? Deflate(subset) : subset;

        var newHeader = Length1Regex().Replace(header, $"/Length1 {subset.Length}");
        newHeader = LengthRegex().Replace(newHeader, $"/Length {newData.Length}");

        using var result = new MemoryStream();
        result.Write(Latin1.GetBytes(newHeader));
        result.Write(span[keywordIndex..dataStart]);
        result.Write(newData);
        result.Write(span[(dataStart + length)..]);

        return result.ToArray();
    }

    private static int SkipEndOfLine(ReadOnlySpan<byte> span, int position)
    {
        if (position < span.Length && span[position] == '\r')
            position++;

        return position < span.Length && span[position] == '\n' ? position + 1 : -1;
    }

    private static byte[]? Inflate(ReadOnlySpan<byte> data)
    {
        try
        {
            using var input = new MemoryStream(data.ToArray());
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();

            zlib.CopyTo(output);

            return output.ToArray();
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static byte[] Deflate(byte[] data)
    {
        using var output = new MemoryStream();

        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            zlib.Write(data);

        return output.ToArray();
    }

    [GeneratedRegex(@"/Length1\s+\d+")]
    private static partial Regex Length1Regex();

    [GeneratedRegex(@"/Length\s+(\d+)")]
    private static partial Regex LengthRegex();
}
