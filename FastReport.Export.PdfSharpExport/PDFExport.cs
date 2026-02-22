using FastReport.Utils;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Drawing;

namespace FastReport.Export.PdfExporter;

public partial class PDFExport : ExportBase
{
    private PdfDocument? pdfDocument;
    private PdfPage? pdfPage;
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
        pdfDocument = new PdfDocument();

        // Use report informations
        pdfDocument.Info.Title = Report.ReportInfo.Name;
        pdfDocument.Info.Author = Report.ReportInfo.Author;
        pdfDocument.Info.Comment = Report.ReportInfo.Description;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        ClosePdfDocument();
    }

    private void ClosePdfDocument()
    {
        pdfDocument?.Dispose();
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
        if (pdfDocument is null)
            throw new NullReferenceException($"{nameof(pdfDocument)} is not initialized");

        pdfPage = pdfDocument.AddPage();

        var pageSizeMm = GetPageSizeInMm(reportPage);

        pdfPage.Width = XUnit.FromPoint(PdfUtils.MmToPoints(pageSizeMm.Width));
        pdfPage.Height = XUnit.FromPoint(PdfUtils.MmToPoints(pageSizeMm.Height));

        pdfAdapter = new PDFGraphicsAdapter(XGraphics.FromPdfPage(pdfPage));
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
        var leftMarginPx = (float)PdfUtils.MmToPixel(reportPage.LeftMargin);
        var topMarginPx = (float)PdfUtils.MmToPixel(reportPage.TopMargin);

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

        ClosePdfAdapter();
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
        var leftMarginPx = (float)PdfUtils.MmToPixel(reportPage.LeftMargin);
        var topMarginPx = (float)PdfUtils.MmToPixel(reportPage.TopMargin);

        pdfAdapter?.TranslateTransform(-leftMarginPx, -topMarginPx);
    }

    private void DrawTopWatermark(ReportPage reportPage)
    {
        if (reportPage.Watermark.Enabled && reportPage.Watermark.ShowImageOnTop)
            AddImageWatermark(reportPage);

        if (reportPage.Watermark.Enabled && reportPage.Watermark.ShowTextOnTop)
            AddTextWatermark(reportPage);
    }

    private void ClosePdfAdapter()
    {
        pdfAdapter?.Dispose();
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
            if (c is Table.TableColumn || c is Table.TableCell || c is Table.TableRow)
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
        if (pdfDocument is null)
            throw new NullReferenceException($"{nameof(pdfDocument)} is not initialized");

        pdfDocument.Save(Stream);
        pdfDocument.Close();
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
            (float)PdfUtils.MmToPixel(widthMm),
            (float)PdfUtils.MmToPixel(heightMm));
    }

    /// <summary>
    /// Returns the full page size in pixels.
    /// </summary>
    private static SizeF GetPageSizeInPixels(ReportPage reportPage)
    {
        var pageSizeMm = GetPageSizeInMm(reportPage);

        return new(
            (float)PdfUtils.MmToPixel(pageSizeMm.Width),
            (float)PdfUtils.MmToPixel(pageSizeMm.Height));
    }

    /// <summary>
    /// Returns the page size in millimeters.
    /// </summary>
    private static SizeF GetPageSizeInMm(ReportPage reportPage) =>
        new(ExportUtils.GetPageWidth(reportPage),
            ExportUtils.GetPageHeight(reportPage));
}

