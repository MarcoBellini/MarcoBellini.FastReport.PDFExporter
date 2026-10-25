using SkiaSharp;

namespace MarcoBellini.FastReport.PDFExporter.Tests;

/// <summary>
/// One-page in-memory PDF with a ready <see cref="PDFGraphicsAdapter"/>, configured like <see cref="PDFExport"/>.
/// </summary>
internal sealed class PdfTestCanvas : IDisposable
{
    public const float PageWidthPoints = 595f;
    public const float PageHeightPoints = 842f;

    private readonly MemoryStream stream = new();
    private readonly SKDocument document;
    private bool closed;

    public PdfTestCanvas()
    {
        document = SKDocument.CreatePdf(stream);
        Canvas = document.BeginPage(PageWidthPoints, PageHeightPoints);
        Canvas.Scale((float)PageUnits.PointsPerPixel);

        Context = PdfRenderContext.CreateDefault();
        Adapter = new PDFGraphicsAdapter(Canvas, Context);
    }

    public SKCanvas Canvas { get; }

    public PdfRenderContext Context { get; }

    public PDFGraphicsAdapter Adapter { get; }

    /// <summary>
    /// Finish the page and document and return the PDF bytes.
    /// </summary>
    public byte[] Close()
    {
        if (!closed)
        {
            Adapter.Dispose();
            document.EndPage();
            document.Close();
            closed = true;
        }

        return stream.ToArray();
    }

    public void Dispose()
    {
        Close();
        document.Dispose();
        Context.Dispose();
        stream.Dispose();
    }
}
