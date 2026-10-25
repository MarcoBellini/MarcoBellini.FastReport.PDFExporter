using FastReport;

namespace MarcoBellini.FastReport.PDFExporter;

/// <summary>
/// Graphics state returned by <see cref="PDFGraphicsAdapter.Save"/>: the Skia save count to restore to.
/// </summary>
internal sealed class PdfGraphicsState(int saveCount) : IGraphicsState
{
    public int SaveCount { get; } = saveCount;
}
