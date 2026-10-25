using MarcoBellini.FastReport.PDFExporter.Pdf;
using MarcoBellini.FastReport.PDFExporter.Skia;
using MarcoBellini.FastReport.PDFExporter.Text;

namespace MarcoBellini.FastReport.PDFExporter;

/// <summary>
/// Document-scoped rendering services shared by every page adapter (fonts, images and caches live as long
/// as the PDF document, so resources are embedded once).
/// </summary>
internal sealed class PdfRenderContext : IDisposable
{
    private readonly List<IDisposable> ownedServices;

    public PdfRenderContext(
        IPaintFactory paintFactory,
        IImageConverter imageConverter,
        TextLayoutEngine textLayoutEngine,
        SkiaTextRenderer textRenderer,
        IStringMeasurer stringMeasurer,
        IPdfPostProcessor postProcessor,
        IEnumerable<IDisposable>? ownedServices = null)
    {
        PaintFactory = paintFactory;
        ImageConverter = imageConverter;
        TextLayoutEngine = textLayoutEngine;
        TextRenderer = textRenderer;
        StringMeasurer = stringMeasurer;
        PostProcessor = postProcessor;

        this.ownedServices = ownedServices?.ToList() ?? [];
    }

    public IPaintFactory PaintFactory { get; }

    public IImageConverter ImageConverter { get; }

    public TextLayoutEngine TextLayoutEngine { get; }

    public SkiaTextRenderer TextRenderer { get; }

    public IStringMeasurer StringMeasurer { get; }

    public IPdfPostProcessor PostProcessor { get; }

    /// <summary>
    /// Compose the default Windows (GDI+ / Skia) services.
    /// </summary>
    public static PdfRenderContext CreateDefault()
    {
        var imageConverter = new GdiImageConverter();
        var fontProvider = new SkiaFontProvider();
        var stringMeasurer = new GdiStringMeasurer();
        var glyphUsage = new GlyphUsageTracker();

        return new PdfRenderContext(
            SkiaPaintFactory.CreateDefault(imageConverter),
            imageConverter,
            new TextLayoutEngine(new SkiaTextMeasurer(fontProvider)),
            new SkiaTextRenderer(fontProvider, glyphUsage),
            stringMeasurer,
            new FontSubsetPostProcessor(glyphUsage),
            [imageConverter, fontProvider, stringMeasurer]);
    }

    public void Dispose()
    {
        foreach (var service in ownedServices)
            service.Dispose();

        ownedServices.Clear();
    }
}
