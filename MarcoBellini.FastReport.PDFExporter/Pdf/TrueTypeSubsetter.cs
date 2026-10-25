using System.Buffers.Binary;
using System.Text;

namespace MarcoBellini.FastReport.PDFExporter.Pdf;

/// <summary>
/// Glyph-preserving TrueType subsetter for fonts embedded as PDF <c>FontFile2</c>.
/// Glyph IDs are kept unchanged (unused glyphs become empty), so the PDF widths and CID mapping stay valid.
/// Only the tables needed to render the font in a PDF are kept (PDF 32000-1, 9.9 plus cmap and OS/2).
/// </summary>
internal static class TrueTypeSubsetter
{
    private const uint TrueTypeVersion = 0x00010000;
    private const uint AppleTrueTypeVersion = 0x74727565; // 'true'
    private const uint ChecksumMagic = 0xB1B0AFBA;

    private const int HeadCheckSumAdjustmentOffset = 8;
    private const int HeadIndexToLocFormatOffset = 50;
    private const int MaxpNumGlyphsOffset = 4;
    private const int GlyphHeaderSize = 10;

    // Composite glyph flags.
    private const ushort Arg1And2AreWords = 0x0001;
    private const ushort WeHaveAScale = 0x0008;
    private const ushort MoreComponents = 0x0020;
    private const ushort WeHaveAnXAndYScale = 0x0040;
    private const ushort WeHaveATwoByTwo = 0x0080;

    private static readonly string[] KeptTables = ["OS/2", "cmap", "cvt ", "fpgm", "glyf", "head", "hhea", "hmtx", "loca", "maxp", "prep"];

    /// <summary>
    /// Returns the subset font, or <c>null</c> when the font is not a supported TrueType font.
    /// </summary>
    public static byte[]? Subset(ReadOnlySpan<byte> font, IReadOnlySet<ushort> glyphs)
    {
        var tables = ReadTableDirectory(font);

        if (tables is null || !HasRequiredTables(tables))
            return null;

        var head = Slice(font, tables["head"]);
        var maxp = Slice(font, tables["maxp"]);

        if (head.Length < HeadIndexToLocFormatOffset + 2 || maxp.Length < MaxpNumGlyphsOffset + 2)
            return null;

        var numGlyphs = BinaryPrimitives.ReadUInt16BigEndian(maxp[MaxpNumGlyphsOffset..]);
        var longLoca = BinaryPrimitives.ReadInt16BigEndian(head[HeadIndexToLocFormatOffset..]) == 1;

        var glyf = Slice(font, tables["glyf"]);
        var loca = ReadLoca(Slice(font, tables["loca"]), numGlyphs, longLoca, glyf.Length);

        if (loca is null)
            return null;

        var keep = ComputeClosure(glyf, loca, glyphs, numGlyphs);
        var (newGlyf, newLoca) = BuildGlyfAndLoca(glyf, loca, keep, numGlyphs);

        var newHead = head.ToArray();
        BinaryPrimitives.WriteInt16BigEndian(newHead.AsSpan(HeadIndexToLocFormatOffset), 1);
        BinaryPrimitives.WriteUInt32BigEndian(newHead.AsSpan(HeadCheckSumAdjustmentOffset), 0);

        var output = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var tag in KeptTables)
        {
            if (!tables.TryGetValue(tag, out var range))
                continue;

            output[tag] = tag switch
            {
                "glyf" => newGlyf,
                "loca" => newLoca,
                "head" => newHead,
                _ => Slice(font, range).ToArray()
            };
        }

        return WriteFont(output);
    }

    private static bool HasRequiredTables(Dictionary<string, (int Offset, int Length)> tables) =>
        new[] { "head", "maxp", "loca", "glyf", "hhea", "hmtx" }.All(tables.ContainsKey);

    private static Dictionary<string, (int Offset, int Length)>? ReadTableDirectory(ReadOnlySpan<byte> font)
    {
        if (font.Length < 12)
            return null;

        var version = BinaryPrimitives.ReadUInt32BigEndian(font);

        if (version != TrueTypeVersion && version != AppleTrueTypeVersion)
            return null;

        var numTables = BinaryPrimitives.ReadUInt16BigEndian(font[4..]);

        if (font.Length < 12 + numTables * 16)
            return null;

        var tables = new Dictionary<string, (int Offset, int Length)>(StringComparer.Ordinal);

        for (int i = 0; i < numTables; i++)
        {
            var record = font.Slice(12 + i * 16, 16);
            var tag = Encoding.ASCII.GetString(record[..4]);
            var offset = BinaryPrimitives.ReadUInt32BigEndian(record[8..]);
            var length = BinaryPrimitives.ReadUInt32BigEndian(record[12..]);

            if (offset + (ulong)length > (ulong)font.Length)
                return null;

            tables[tag] = ((int)offset, (int)length);
        }

        return tables;
    }

    private static ReadOnlySpan<byte> Slice(ReadOnlySpan<byte> font, (int Offset, int Length) range) =>
        font.Slice(range.Offset, range.Length);

    private static uint[]? ReadLoca(ReadOnlySpan<byte> loca, int numGlyphs, bool longFormat, int glyfLength)
    {
        var entrySize = longFormat ? 4 : 2;

        if (loca.Length < (numGlyphs + 1) * entrySize)
            return null;

        var offsets = new uint[numGlyphs + 1];

        for (int i = 0; i <= numGlyphs; i++)
        {
            offsets[i] = longFormat
                ? BinaryPrimitives.ReadUInt32BigEndian(loca[(i * 4)..])
                : BinaryPrimitives.ReadUInt16BigEndian(loca[(i * 2)..]) * 2u;

            if (offsets[i] > glyfLength || (i > 0 && offsets[i] < offsets[i - 1]))
                return null;
        }

        return offsets;
    }

    /// <summary>
    /// Requested glyphs plus .notdef and every component referenced by composite glyphs.
    /// </summary>
    internal static bool[] ComputeClosure(ReadOnlySpan<byte> glyf, uint[] loca, IReadOnlySet<ushort> glyphs, int numGlyphs)
    {
        var keep = new bool[numGlyphs];
        var pending = new Stack<int>();

        pending.Push(0);

        foreach (var glyph in glyphs)
            pending.Push(glyph);

        while (pending.Count > 0)
        {
            var glyph = pending.Pop();

            if (glyph >= numGlyphs || keep[glyph])
                continue;

            keep[glyph] = true;

            foreach (var component in GetComponents(glyf, loca, glyph))
                pending.Push(component);
        }

        return keep;
    }

    private static List<int> GetComponents(ReadOnlySpan<byte> glyf, uint[] loca, int glyph)
    {
        var components = new List<int>();
        var data = glyf[(int)loca[glyph]..(int)loca[glyph + 1]];

        if (data.Length < GlyphHeaderSize || BinaryPrimitives.ReadInt16BigEndian(data) >= 0)
            return components;

        var position = GlyphHeaderSize;
        ushort flags;

        do
        {
            if (position + 4 > data.Length)
                break;

            flags = BinaryPrimitives.ReadUInt16BigEndian(data[position..]);
            components.Add(BinaryPrimitives.ReadUInt16BigEndian(data[(position + 2)..]));

            position += 4;
            position += (flags & Arg1And2AreWords) != 0 ? 4 : 2;

            if ((flags & WeHaveAScale) != 0)
                position += 2;
            else if ((flags & WeHaveAnXAndYScale) != 0)
                position += 4;
            else if ((flags & WeHaveATwoByTwo) != 0)
                position += 8;
        }
        while ((flags & MoreComponents) != 0);

        return components;
    }

    private static (byte[] Glyf, byte[] Loca) BuildGlyfAndLoca(ReadOnlySpan<byte> glyf, uint[] loca, bool[] keep, int numGlyphs)
    {
        using var glyfStream = new MemoryStream();
        var newLoca = new byte[(numGlyphs + 1) * 4];

        for (int glyph = 0; glyph < numGlyphs; glyph++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(newLoca.AsSpan(glyph * 4), (uint)glyfStream.Length);

            if (!keep[glyph])
                continue;

            glyfStream.Write(glyf[(int)loca[glyph]..(int)loca[glyph + 1]]);
            Pad(glyfStream);
        }

        BinaryPrimitives.WriteUInt32BigEndian(newLoca.AsSpan(numGlyphs * 4), (uint)glyfStream.Length);

        return (glyfStream.ToArray(), newLoca);
    }

    private static byte[] WriteFont(SortedDictionary<string, byte[]> tables)
    {
        var numTables = (ushort)tables.Count;
        var entrySelector = (ushort)Math.Floor(Math.Log2(numTables));
        var searchRange = (ushort)((1 << entrySelector) * 16);
        var rangeShift = (ushort)(numTables * 16 - searchRange);

        var headerLength = 12 + numTables * 16;
        var offset = headerLength;

        using var stream = new MemoryStream();
        var header = new byte[headerLength];

        BinaryPrimitives.WriteUInt32BigEndian(header, TrueTypeVersion);
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(4), numTables);
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(6), searchRange);
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(8), entrySelector);
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(10), rangeShift);

        var index = 0;
        var headTableOffset = -1;

        foreach (var (tag, data) in tables)
        {
            var record = header.AsSpan(12 + index * 16, 16);

            Encoding.ASCII.GetBytes(tag, record);
            BinaryPrimitives.WriteUInt32BigEndian(record[4..], Checksum(data));
            BinaryPrimitives.WriteUInt32BigEndian(record[8..], (uint)offset);
            BinaryPrimitives.WriteUInt32BigEndian(record[12..], (uint)data.Length);

            if (tag == "head")
                headTableOffset = offset;

            offset += Align4(data.Length);
            index++;
        }

        stream.Write(header);

        foreach (var data in tables.Values)
        {
            stream.Write(data);
            Pad(stream);
        }

        var result = stream.ToArray();

        if (headTableOffset >= 0)
        {
            var adjustment = unchecked(ChecksumMagic - Checksum(result));
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(headTableOffset + HeadCheckSumAdjustmentOffset), adjustment);
        }

        return result;
    }

    internal static uint Checksum(ReadOnlySpan<byte> data)
    {
        uint sum = 0;
        var fullWords = data.Length / 4;

        for (int i = 0; i < fullWords; i++)
            sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(data[(i * 4)..]));

        var remaining = data.Length % 4;

        if (remaining > 0)
        {
            Span<byte> last = stackalloc byte[4];
            data[(fullWords * 4)..].CopyTo(last);
            sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(last));
        }

        return sum;
    }

    private static int Align4(int length) => (length + 3) & ~3;

    private static void Pad(MemoryStream stream)
    {
        while (stream.Length % 4 != 0)
            stream.WriteByte(0);
    }
}
