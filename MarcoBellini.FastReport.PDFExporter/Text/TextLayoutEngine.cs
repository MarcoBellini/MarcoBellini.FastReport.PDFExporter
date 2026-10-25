using System.Drawing;
using System.Text;

namespace MarcoBellini.FastReport.PDFExporter.Text;

/// <summary>
/// Emulates GDI+ <c>DrawString</c> line breaking and alignment: word wrap (with character breaking for words
/// wider than the line), explicit line breaks, horizontal and vertical alignment, <see cref="StringFormatFlags.NoWrap"/>,
/// <see cref="StringFormatFlags.LineLimit"/>, right-to-left alignment swap and ellipsis trimming.
/// A layout rectangle with zero width/height anchors the text to its location, like the GDI+ point overloads.
/// </summary>
internal sealed class TextLayoutEngine(ITextMeasurer measurer)
{
    private const char Space = ' ';
    private const string Ellipsis = "…";
    private const string TabReplacement = "    ";

    // GDI+ (non typographic) string formats add 1/6 em of padding on the left and right of each line.
    private const float GdiPaddingEm = 1f / 6f;
    private const float Epsilon = 0.01f;

    public ITextMeasurer Measurer => measurer;

    public TextLayout Layout(string text, Font font, RectangleF layoutRect, StringFormat format)
    {
        var metrics = measurer.GetMetrics(font);
        var padding = IsTypographic(format) ? 0f : PageUnits.FontPixelSize(font) * GdiPaddingEm;

        var hasWidth = layoutRect.Width > 0;
        var hasHeight = layoutRect.Height > 0;
        var wrap = hasWidth && !format.FormatFlags.HasFlag(StringFormatFlags.NoWrap);
        var maxWidth = Math.Max(0f, layoutRect.Width - 2 * padding);

        var lines = BreakLines(text, font, wrap, maxWidth);

        if (hasHeight)
            lines = LimitLines(lines, font, format, layoutRect.Height, metrics.LineHeight, maxWidth);

        if (hasWidth && format.FormatFlags.HasFlag(StringFormatFlags.NoWrap))
            lines = lines.Select(l => TrimToWidth(l, font, format.Trimming, maxWidth)).ToList();

        return new TextLayout(PositionLines(lines, font, layoutRect, format, metrics, padding), metrics);
    }

    /// <summary>
    /// GenericTypographic has no padding and sets <see cref="StringFormatFlags.FitBlackBox"/>.
    /// </summary>
    private static bool IsTypographic(StringFormat format) =>
        format.FormatFlags.HasFlag(StringFormatFlags.FitBlackBox);

    private List<string> BreakLines(string text, Font font, bool wrap, float maxWidth)
    {
        var paragraphs = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", TabReplacement).Split('\n');
        var lines = new List<string>();

        foreach (var paragraph in paragraphs)
        {
            if (wrap)
                lines.AddRange(WrapParagraph(paragraph, font, maxWidth));
            else
                lines.Add(paragraph);
        }

        return lines;
    }

    internal IEnumerable<string> WrapParagraph(string paragraph, Font font, float maxWidth)
    {
        if (paragraph.Length == 0)
        {
            yield return paragraph;
            yield break;
        }

        var line = new StringBuilder();

        foreach (var word in paragraph.Split(Space))
        {
            var candidate = line.Length == 0 ? word : line + " " + word;

            if (Fits(candidate, font, maxWidth))
            {
                line.Clear().Append(candidate);
                continue;
            }

            if (line.Length > 0)
            {
                yield return line.ToString();
                line.Clear();
            }

            // The word alone does not fit: break it at character level.
            var (parts, remainder) = BreakWord(word, font, maxWidth);

            foreach (var part in parts)
                yield return part;

            line.Append(remainder);
        }

        yield return line.ToString();
    }

    /// <summary>
    /// Split a word wider than the line into full-width parts plus the remaining tail.
    /// </summary>
    private (List<string> Parts, string Remainder) BreakWord(string word, Font font, float maxWidth)
    {
        var parts = new List<string>();
        var start = 0;

        while (start < word.Length)
        {
            var length = FitCharacters(word, start, font, maxWidth);

            if (start + length >= word.Length)
                break;

            parts.Add(word.Substring(start, length));
            start += length;
        }

        return (parts, word[start..]);
    }

    /// <summary>
    /// Number of characters from <paramref name="start"/> that fit in the width (at least one, to always progress).
    /// </summary>
    private int FitCharacters(string text, int start, Font font, float maxWidth)
    {
        var length = 1;

        while (start + length < text.Length && Fits(text.Substring(start, length + 1), font, maxWidth))
            length++;

        return length;
    }

    private bool Fits(string text, Font font, float maxWidth) =>
        measurer.MeasureWidth(text.TrimEnd(), font) <= maxWidth + Epsilon;

    private List<string> LimitLines(List<string> lines, Font font, StringFormat format, float height, float lineHeight, float maxWidth)
    {
        var fullLines = (int)Math.Floor((height + Epsilon) / lineHeight);
        var visible = format.FormatFlags.HasFlag(StringFormatFlags.LineLimit)
            ? fullLines
            : (int)Math.Ceiling(height / lineHeight - Epsilon);

        visible = Math.Max(0, visible);

        if (lines.Count <= visible)
            return lines;

        var result = lines.Take(visible).ToList();

        if (result.Count > 0 && IsEllipsis(format.Trimming))
            result[^1] = AppendEllipsis(result[^1], font, maxWidth);

        return result;
    }

    private string TrimToWidth(string line, Font font, StringTrimming trimming, float maxWidth)
    {
        if (!IsEllipsis(trimming) || Fits(line, font, maxWidth))
            return line;

        return AppendEllipsis(line, font, maxWidth);
    }

    private string AppendEllipsis(string line, Font font, float maxWidth)
    {
        var text = line.TrimEnd();

        while (text.Length > 0 && !Fits(text + Ellipsis, font, maxWidth))
            text = text[..^1];

        return text + Ellipsis;
    }

    private static bool IsEllipsis(StringTrimming trimming) =>
        trimming is StringTrimming.EllipsisCharacter or StringTrimming.EllipsisWord or StringTrimming.EllipsisPath;

    private List<TextLine> PositionLines(List<string> lines, Font font, RectangleF rect, StringFormat format,
        FontLineMetrics metrics, float padding)
    {
        var alignment = format.FormatFlags.HasFlag(StringFormatFlags.DirectionRightToLeft)
            ? Mirror(format.Alignment)
            : format.Alignment;

        var totalHeight = lines.Count * metrics.LineHeight;
        var top = Align(format.LineAlignment, rect.Top, rect.Height, totalHeight, 0f);

        var result = new List<TextLine>(lines.Count);

        for (int i = 0; i < lines.Count; i++)
        {
            var text = lines[i].TrimEnd();
            var width = measurer.MeasureWidth(text, font);
            var x = Align(alignment, rect.Left, rect.Width, width, padding);

            result.Add(new TextLine(text, x, top + i * metrics.LineHeight + metrics.Ascent, width));
        }

        return result;
    }

    /// <summary>
    /// Position of a block of <paramref name="size"/> inside [start, start + available], honoring padding.
    /// With zero available space the block is anchored to <paramref name="start"/> (GDI+ point semantics).
    /// </summary>
    private static float Align(StringAlignment alignment, float start, float available, float size, float padding) => alignment switch
    {
        StringAlignment.Center => start + (available - size) / 2f,
        StringAlignment.Far => start + available - padding - size,
        _ => start + padding
    };

    private static StringAlignment Mirror(StringAlignment alignment) => alignment switch
    {
        StringAlignment.Near => StringAlignment.Far,
        StringAlignment.Far => StringAlignment.Near,
        _ => alignment
    };
}
