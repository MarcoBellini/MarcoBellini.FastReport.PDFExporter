namespace MarcoBellini.FastReport.PDFExporter.Text;

/// <summary>
/// A single positioned line of text. <see cref="X"/> is the left edge, <see cref="Baseline"/> the baseline Y.
/// </summary>
internal readonly record struct TextLine(string Text, float X, float Baseline, float Width);

/// <summary>
/// Result of laying out a string inside a rectangle (or relative to a point).
/// </summary>
internal sealed record TextLayout(IReadOnlyList<TextLine> Lines, FontLineMetrics Metrics);
