using FastReport;
using FastReport.Export;
using FastReport.Table;
using FastReport.Utils;
using SkiaSharp;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter;

public partial class PDFExport : ExportBase
{
    private const string ProducerName = "MarcoBellini.FastReport.PDFExporter (SkiaSharp)";

    private SKDocument? pdfDocument;
    private MemoryStream? pdfBuffer;
    private PdfRenderContext? renderContext;
    private PDFGraphicsAdapter? pdfAdapter;

    public PDFExport()
    {
    }

    /// <summary>
    /// This method is called when the export starts.
    /// </summary>
    protected override void Start()
    {
        base.Start();

        CreatePdfDocument();
    }

    private void CreatePdfDocument()
    {
        var metadata = new SKDocumentPdfMetadata
        {
            Title = Report.ReportInfo.Name,
            Author = Report.ReportInfo.Author,
            Subject = Report.ReportInfo.Description,
            Creator = "FastReport",
            Producer = ProducerName,
            Creation = DateTime.Now,
            Modified = DateTime.Now,
            RasterDpi = SKDocument.DefaultRasterDpi,
            EncodingQuality = SKDocumentPdfMetadata.DefaultEncodingQuality
        };

        // The document is buffered so that it can be post-processed (font subsetting) before reaching the export stream.
        pdfBuffer = new MemoryStream();
        pdfDocument = SKDocument.CreatePdf(pdfBuffer, metadata)
            ?? throw new InvalidOperationException("Unable to create the PDF document.");

        renderContext = PdfRenderContext.CreateDefault();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        ClosePdfDocument();
    }

    private void ClosePdfDocument()
    {
        pdfDocument?.Dispose();
        pdfDocument = null;

        pdfBuffer?.Dispose();
        pdfBuffer = null;

        renderContext?.Dispose();
        renderContext = null;
    }

    /// <summary>
    /// This method is called at the start of exports of each reportPage.
    /// </summary>
    /// <param name="reportPage">Page for export may be empty in this method.</param>
    protected override void ExportPageBegin(ReportPage reportPage)
    {
        base.ExportPageBegin(reportPage);

        CreatePdfPageAndAdapter(reportPage);

        DrawPageBackground(reportPage);
        DrawBottomWatermark(reportPage);

        AddPageMarginsToAdapter(reportPage);
    }

    private void CreatePdfPageAndAdapter(ReportPage reportPage)
    {
        if (pdfDocument is null || renderContext is null)
            throw new NullReferenceException($"{nameof(pdfDocument)} is not initialized");

        var pageSizeMm = GetPageSizeInMm(reportPage);

        var canvas = pdfDocument.BeginPage(
            (float)PageUnits.MmToPoints(pageSizeMm.Width),
            (float)PageUnits.MmToPoints(pageSizeMm.Height));

        // Draw in FastReport pixels: the page base matrix converts them to PDF points.
        canvas.Scale((float)PageUnits.PointsPerPixel);

        pdfAdapter = new PDFGraphicsAdapter(canvas, renderContext);
    }

    private void DrawPageBackground(ReportPage reportPage)
    {
        using var backgroundObject = new TextObject();

        var pageSizePx = GetPageSizeInPixels(reportPage);

        backgroundObject.Fill = reportPage.Fill;
        backgroundObject.Left = 0;
        backgroundObject.Top = 0;
        backgroundObject.Width = pageSizePx.Width;
        backgroundObject.Height = pageSizePx.Height;

        backgroundObject.Draw(CreatePaintArgs());
    }

    private void DrawBottomWatermark(ReportPage reportPage)
    {
        if (reportPage.Watermark.Enabled && !reportPage.Watermark.ShowImageOnTop)
            AddImageWatermark(reportPage);

        if (reportPage.Watermark.Enabled && !reportPage.Watermark.ShowTextOnTop)
            AddTextWatermark(reportPage);
    }

    private void AddPageMarginsToAdapter(ReportPage reportPage)
    {
        var leftMarginPx = (float)PageUnits.MmToPixel(reportPage.LeftMargin);
        var topMarginPx = (float)PageUnits.MmToPixel(reportPage.TopMargin);

        pdfAdapter?.TranslateTransform(leftMarginPx, topMarginPx);
    }

    /// <summary>
    /// This method is called at the end of exports of each reportPage.
    /// </summary>
    /// <param name="reportPage">Page for export may be empty in this method.</param>
    protected override void ExportPageEnd(ReportPage reportPage)
    {
        base.ExportPageEnd(reportPage);

        if (pdfAdapter is null)
            throw new NullReferenceException($"{nameof(pdfAdapter)} is not initialized");

        DrawPageBorders(reportPage);
        RemoveMarginsFromAdapter(reportPage);
        DrawTopWatermark(reportPage);

        ClosePdfPage();
    }

    private void DrawPageBorders(ReportPage reportPage)
    {
        if (reportPage.Border.Lines == BorderLines.None)
            return;

        using var borderObject = new TextObject();

        var contentAreaSizePx = GetContentAreaSizeInPixels(reportPage);

        borderObject.Border = reportPage.Border;
        borderObject.Left = 0;
        borderObject.Top = 0;
        borderObject.Width = contentAreaSizePx.Width;
        borderObject.Height = contentAreaSizePx.Height;

        borderObject.Draw(CreatePaintArgs());
    }

    private void RemoveMarginsFromAdapter(ReportPage reportPage)
    {
        var leftMarginPx = (float)PageUnits.MmToPixel(reportPage.LeftMargin);
        var topMarginPx = (float)PageUnits.MmToPixel(reportPage.TopMargin);

        pdfAdapter?.TranslateTransform(-leftMarginPx, -topMarginPx);
    }

    private void DrawTopWatermark(ReportPage reportPage)
    {
        if (reportPage.Watermark.Enabled && reportPage.Watermark.ShowImageOnTop)
            AddImageWatermark(reportPage);

        if (reportPage.Watermark.Enabled && reportPage.Watermark.ShowTextOnTop)
            AddTextWatermark(reportPage);
    }

    private void ClosePdfPage()
    {
        pdfAdapter?.Dispose();
        pdfAdapter = null;

        pdfDocument?.EndPage();
    }

    /// <summary>
    /// This method is called for each band on exported reportPage.
    /// </summary>
    /// <param name="band">Band, dispose after method completes.</param>
    protected override void ExportBand(BandBase band)
    {
        base.ExportBand(band);

        if (pdfAdapter is null)
            throw new NullReferenceException($"{nameof(pdfAdapter)} is not initialized");

        DrawBandBackground(band);
        DrawBandObjects(band);
    }

    private void DrawBandBackground(BandBase band)
    {
        band.Draw(CreatePaintArgs());
    }

    private void DrawBandObjects(BandBase band)
    {
        foreach (Base c in band.ForEachAllConvectedObjects(this))
        {
            // Skip table sub-objects
            if (c is TableColumn || c is TableCell || c is TableRow)
                continue;

            if (c is not ReportComponentBase obj || !obj.Exportable)
                continue;

            obj.Draw(CreatePaintArgs());
        }
    }

    /// <summary>
    /// This method is called when the export is finished.
    /// </summary>
    protected override void Finish()
    {
        base.Finish();

        SaveAndCloseDocument();
    }

    private void SaveAndCloseDocument()
    {
        if (pdfDocument is null || pdfBuffer is null || renderContext is null)
            throw new NullReferenceException($"{nameof(pdfDocument)} is not initialized");

        pdfDocument.Close();

        var pdf = renderContext.PostProcessor.Process(pdfBuffer.ToArray());
        Stream.Write(pdf);

        ClosePdfDocument();
    }

    /// <summary>
    /// Add Image Watermark to reportPage
    /// </summary>
    private void AddImageWatermark(ReportPage reportPage)
    {
        if (pdfAdapter is null)
            throw new NullReferenceException($"{nameof(pdfAdapter)} is not initialized");

        var watermarkRect = GetWatermarkLayoytRect(reportPage);

        reportPage.Watermark.DrawImage(CreatePaintArgs(), watermarkRect, reportPage.Report, false);
    }

    /// <summary>
    /// Add Text Watermark to reportPage
    /// </summary>
    private void AddTextWatermark(ReportPage reportPage)
    {
        if (pdfAdapter is null)
            throw new NullReferenceException($"{nameof(pdfAdapter)} is not initialized");

        if (string.IsNullOrEmpty(reportPage.Watermark.Text))
            return;

        var watermarkRect = GetWatermarkLayoytRect(reportPage);

        reportPage.Watermark.DrawText(CreatePaintArgs(), watermarkRect, reportPage.Report, false);
    }

    /// <summary>
    /// Creates a <see cref="FRPaintEventArgs"/> using the current adapter and report cache.
    /// </summary>
    private FRPaintEventArgs CreatePaintArgs() =>
        new(pdfAdapter, 1.0f, 1.0f, Report.GraphicCache);

    /// <summary>
    /// Returns the watermark layout rectangle in pixels.
    /// </summary>
    private static RectangleF GetWatermarkLayoytRect(ReportPage reportPage)
    {
        var pageSizePx = GetPageSizeInPixels(reportPage);

        return new RectangleF(0, 0, pageSizePx.Width, pageSizePx.Height);
    }

    /// <summary>
    /// Returns the content area size (page minus margins) in pixels.
    /// </summary>
    private static SizeF GetContentAreaSizeInPixels(ReportPage reportPage)
    {
        var pageSizeMm = GetPageSizeInMm(reportPage);
        var widthMm = pageSizeMm.Width - reportPage.LeftMargin - reportPage.RightMargin;
        var heightMm = pageSizeMm.Height - reportPage.TopMargin - reportPage.BottomMargin;

        return new(
            (float)PageUnits.MmToPixel(widthMm),
            (float)PageUnits.MmToPixel(heightMm));
    }

    /// <summary>
    /// Returns the full page size in pixels.
    /// </summary>
    private static SizeF GetPageSizeInPixels(ReportPage reportPage)
    {
        var pageSizeMm = GetPageSizeInMm(reportPage);

        return new(
            (float)PageUnits.MmToPixel(pageSizeMm.Width),
            (float)PageUnits.MmToPixel(pageSizeMm.Height));
    }

    /// <summary>
    /// Returns the page size in millimeters.
    /// </summary>
    private static SizeF GetPageSizeInMm(ReportPage reportPage) =>
        new(ExportUtils.GetPageWidth(reportPage),
            ExportUtils.GetPageHeight(reportPage));
}
