using MarcoBellini.FastReport.PDFExporter.Text;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Tests;

public sealed class TextLayoutEngineTests : IDisposable
{
    private const float CharWidth = 10f;
    private const float Ascent = 8f;
    private const float LineHeight = 12f;

    /// <summary>
    /// Deterministic measurer: every character is 10 px wide, lines are 12 px high.
    /// </summary>
    private sealed class FixedWidthMeasurer : ITextMeasurer
    {
        public float MeasureWidth(string text, Font font) => text.Length * CharWidth;

        public FontLineMetrics GetMetrics(Font font) => new(Ascent, LineHeight, 1f, 1f, -3f, 1f);
    }

    private readonly TextLayoutEngine engine = new(new FixedWidthMeasurer());
    private readonly Font font = new("Arial", 12);

    public void Dispose() => font.Dispose();

    /// <summary>
    /// Typographic formats have no GDI+ padding, which keeps expected positions simple.
    /// </summary>
    private static StringFormat Typographic(StringAlignment alignment = StringAlignment.Near,
        StringAlignment lineAlignment = StringAlignment.Near, StringFormatFlags extraFlags = 0)
    {
        var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        format.Alignment = alignment;
        format.LineAlignment = lineAlignment;
        format.FormatFlags = StringFormatFlags.FitBlackBox | extraFlags;
        return format;
    }

    [Fact]
    public void TestWordWrap()
    {
        using var format = Typographic();

        var layout = engine.Layout("aaa bbb ccc", font, new RectangleF(0, 0, 75, 100), format);

        Assert.Equal(["aaa bbb", "ccc"], layout.Lines.Select(l => l.Text));
        Assert.Equal(Ascent, layout.Lines[0].Baseline);
        Assert.Equal(Ascent + LineHeight, layout.Lines[1].Baseline);
    }

    [Fact]
    public void TestLongWordIsBrokenByCharacters()
    {
        using var format = Typographic();

        var layout = engine.Layout("abcdefgh xy", font, new RectangleF(0, 0, 30, 100), format);

        Assert.Equal(["abc", "def", "gh", "xy"], layout.Lines.Select(l => l.Text));
    }

    [Fact]
    public void TestExplicitLineBreaks()
    {
        using var format = Typographic();

        var layout = engine.Layout("one\r\ntwo\nthree", font, new RectangleF(0, 0, 500, 100), format);

        Assert.Equal(["one", "two", "three"], layout.Lines.Select(l => l.Text));
    }

    [Fact]
    public void TestNoWrapKeepsSingleLine()
    {
        using var format = Typographic(extraFlags: StringFormatFlags.NoWrap);

        var layout = engine.Layout("aaa bbb ccc", font, new RectangleF(0, 0, 30, 100), format);

        Assert.Single(layout.Lines);
    }

    [Theory]
    [InlineData(StringAlignment.Near, 0f)]
    [InlineData(StringAlignment.Center, 35f)]
    [InlineData(StringAlignment.Far, 70f)]
    public void TestHorizontalAlignment(StringAlignment alignment, float expectedX)
    {
        using var format = Typographic(alignment);

        var layout = engine.Layout("abc", font, new RectangleF(0, 0, 100, 50), format);

        Assert.Equal(expectedX, layout.Lines[0].X, 3);
        Assert.Equal(30f, layout.Lines[0].Width, 3);
    }

    [Theory]
    [InlineData(StringAlignment.Near, Ascent)]
    [InlineData(StringAlignment.Center, 44f + Ascent)]
    [InlineData(StringAlignment.Far, 88f + Ascent)]
    public void TestVerticalAlignment(StringAlignment lineAlignment, float expectedBaseline)
    {
        using var format = Typographic(lineAlignment: lineAlignment);

        var layout = engine.Layout("abc", font, new RectangleF(0, 0, 100, 100), format);

        Assert.Equal(expectedBaseline, layout.Lines[0].Baseline, 3);
    }

    [Fact]
    public void TestRightToLeftSwapsAlignment()
    {
        using var format = Typographic(StringAlignment.Near, extraFlags: StringFormatFlags.DirectionRightToLeft);

        var layout = engine.Layout("abc", font, new RectangleF(0, 0, 100, 50), format);

        Assert.Equal(70f, layout.Lines[0].X, 3);
    }

    [Fact]
    public void TestPointAnchoredLayout()
    {
        using var format = Typographic(StringAlignment.Center, StringAlignment.Far);

        var layout = engine.Layout("abcd", font, new RectangleF(100, 100, 0, 0), format);

        Assert.Equal(80f, layout.Lines[0].X, 3);
        Assert.Equal(100f - LineHeight + Ascent, layout.Lines[0].Baseline, 3);
    }

    [Fact]
    public void TestGdiPaddingForDefaultFormat()
    {
        using var format = new StringFormat();

        var layout = engine.Layout("abc", font, new RectangleF(0, 0, 100, 50), format);

        // Arial 12pt = 16 px em, GDI+ padding = em / 6.
        Assert.Equal(16f / 6f, layout.Lines[0].X, 3);
    }

    [Fact]
    public void TestLineLimitDropsPartialLines()
    {
        using var format = Typographic(extraFlags: StringFormatFlags.LineLimit);

        var layout = engine.Layout("a\nb\nc", font, new RectangleF(0, 0, 100, 30), format);

        Assert.Equal(2, layout.Lines.Count);
    }

    [Fact]
    public void TestPartialLinesAreKeptWithoutLineLimit()
    {
        using var format = Typographic();

        var layout = engine.Layout("a\nb\nc\nd", font, new RectangleF(0, 0, 100, 30), format);

        Assert.Equal(3, layout.Lines.Count);
    }

    [Fact]
    public void TestEllipsisTrimming()
    {
        using var format = Typographic(extraFlags: StringFormatFlags.NoWrap);
        format.Trimming = StringTrimming.EllipsisCharacter;

        var layout = engine.Layout("abcdefghij", font, new RectangleF(0, 0, 50, 20), format);

        Assert.Equal("abcd…", layout.Lines[0].Text);
    }
}
