using System.Globalization;
using System.Text;

namespace MarcoBellini.FastReport.PDFExporter.Pdf;

/// <summary>
/// Rewrites the indirect objects of a PDF that uses a single classic cross-reference table (the layout written by
/// Skia) and rebuilds the table with the new offsets. Unsupported layouts are returned unchanged.
/// </summary>
internal static class PdfXrefRewriter
{
    private static readonly Encoding Latin1 = Encoding.Latin1;

    private readonly record struct XrefEntry(int ObjectNumber, long Offset, bool InUse, string Raw);

    /// <summary>
    /// Apply <paramref name="transform"/> to every object (from "N 0 obj" to the next object).
    /// The transform returns the replacement bytes, or <c>null</c> to keep the object unchanged.
    /// </summary>
    public static byte[] Rewrite(byte[] pdf, Func<ReadOnlyMemory<byte>, byte[]?> transform)
    {
        if (!TryParse(pdf, out var xrefOffset, out var entries, out var trailer))
            return pdf;

        var objects = entries.Where(e => e.InUse).OrderBy(e => e.Offset).ToList();

        if (objects.Count == 0)
            return pdf;

        using var output = new MemoryStream(pdf.Length);
        var newOffsets = new Dictionary<int, long>();

        output.Write(pdf, 0, (int)objects[0].Offset);

        for (int i = 0; i < objects.Count; i++)
        {
            var start = objects[i].Offset;
            var end = i + 1 < objects.Count ? objects[i + 1].Offset : xrefOffset;
            var original = new ReadOnlyMemory<byte>(pdf, (int)start, (int)(end - start));

            newOffsets[objects[i].ObjectNumber] = output.Position;
            output.Write(transform(original) ?? original.ToArray());
        }

        WriteXref(output, entries, newOffsets, trailer);

        return output.ToArray();
    }

    private static bool TryParse(byte[] pdf, out long xrefOffset, out List<XrefEntry> entries, out string trailer)
    {
        xrefOffset = 0;
        entries = [];
        trailer = string.Empty;

        var text = Latin1.GetString(pdf, Math.Max(0, pdf.Length - 64), Math.Min(64, pdf.Length));
        var startxrefIndex = text.LastIndexOf("startxref", StringComparison.Ordinal);

        if (startxrefIndex < 0)
            return false;

        var offsetText = text[(startxrefIndex + "startxref".Length)..].Trim().Split('\n', '\r')[0];

        if (!long.TryParse(offsetText, NumberStyles.None, CultureInfo.InvariantCulture, out xrefOffset) || xrefOffset <= 0 || xrefOffset >= pdf.Length)
            return false;

        var tail = Latin1.GetString(pdf, (int)xrefOffset, pdf.Length - (int)xrefOffset);

        if (!tail.StartsWith("xref", StringComparison.Ordinal))
            return false;

        var trailerIndex = tail.IndexOf("trailer", StringComparison.Ordinal);
        var tailStartxref = tail.LastIndexOf("startxref", StringComparison.Ordinal);

        if (trailerIndex < 0 || tailStartxref < trailerIndex)
            return false;

        var lines = tail[4..trailerIndex].Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 1)
            return false;

        var subsection = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // A single subsection starting at object 0 is required.
        if (subsection.Length != 2 || subsection[0] != "0" || !int.TryParse(subsection[1], out var count) || lines.Length != count + 1)
            return false;

        for (int i = 0; i < count; i++)
        {
            var parts = lines[i + 1].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3 || !long.TryParse(parts[0], out var offset))
                return false;

            var inUse = parts[2] == "n";

            if (inUse && (offset <= 0 || offset >= xrefOffset))
                return false;

            entries.Add(new XrefEntry(i, offset, inUse, lines[i + 1].TrimEnd()));
        }

        trailer = tail[trailerIndex..tailStartxref];

        return true;
    }

    private static void WriteXref(MemoryStream output, List<XrefEntry> entries, Dictionary<int, long> newOffsets, string trailer)
    {
        var xrefPosition = output.Position;
        var builder = new StringBuilder();

        builder.Append("xref\n0 ").Append(entries.Count).Append('\n');

        foreach (var entry in entries)
        {
            if (entry.InUse)
                builder.Append(newOffsets[entry.ObjectNumber].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
            else
                builder.Append(entry.Raw).Append(" \n");
        }

        builder.Append(trailer)
               .Append("startxref\n")
               .Append(xrefPosition.ToString(CultureInfo.InvariantCulture))
               .Append("\n%%EOF\n");

        output.Write(Latin1.GetBytes(builder.ToString()));
    }
}
