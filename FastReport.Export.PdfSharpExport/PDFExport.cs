using FastReport.Utils;
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

        var pageWidth = ExportUtils.GetPageWidth(reportPage);
        var pageHeight = ExportUtils.GetPageHeight(reportPage);

        pdfPage.Width = XUnit.FromPoint(PdfUtils.MmToPoints(pageWidth));
        pdfPage.Height = XUnit.FromPoint(PdfUtils.MmToPoints(pageHeight));

        pdfAdapter = new PDFGraphicsAdapter(XGraphics.FromPdfPage(pdfPage));
    }

    private void DrawPageBackground(ReportPage reportPage)
    {
        using var pageFill = new TextObject();

        var pageWidth = ExportUtils.GetPageWidth(reportPage);
        var pageHeight = ExportUtils.GetPageHeight(reportPage);

        pageWidth = Convert.ToSingle(PdfUtils.MmToPixel(pageWidth));
        pageHeight = Convert.ToSingle(PdfUtils.MmToPixel(pageHeight));

        pageFill.Fill = reportPage.Fill;
        pageFill.Left = 0;
        pageFill.Top = 0;
        pageFill.Width = pageWidth;
        pageFill.Height = pageHeight;

        pageFill.Draw(new FRPaintEventArgs(pdfAdapter, 1.0f, 1.0f, Report.GraphicCache));
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
        var LeftMargin = PdfUtils.MmToPixel(reportPage.LeftMargin);
        var TopMargin = PdfUtils.MmToPixel(reportPage.TopMargin);

        pdfAdapter?.TranslateTransform(Convert.ToSingle(LeftMargin), Convert.ToSingle(TopMargin));
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

        ClosePdfDapter();
    }
    private void DrawPageBorders(ReportPage reportPage)
    {
        if (reportPage.Border.Lines == BorderLines.None)
            return;

        using var pageBorder = new TextObject();

        var borderRectWidth = ExportUtils.GetPageWidth(reportPage) - reportPage.LeftMargin - reportPage.RightMargin;
        var borderRectHeight = ExportUtils.GetPageHeight(reportPage) - reportPage.TopMargin - reportPage.BottomMargin;

        borderRectWidth = Convert.ToSingle(PdfUtils.MmToPixel(borderRectWidth));
        borderRectHeight = Convert.ToSingle(PdfUtils.MmToPixel(borderRectHeight));

        pageBorder.Border = reportPage.Border;
        pageBorder.Left = 0;
        pageBorder.Top = 0;
        pageBorder.Width = borderRectWidth;
        pageBorder.Height = borderRectHeight;

        pageBorder.Draw(new FRPaintEventArgs(pdfAdapter, 1.0f, 1.0f, Report.GraphicCache));
    }
    private void RemoveMarginsFromAdapter(ReportPage reportPage)
    {
        var leftMargin = PdfUtils.MmToPixel(reportPage.LeftMargin);
        var topMargin = PdfUtils.MmToPixel(reportPage.TopMargin);

        pdfAdapter?.TranslateTransform(-Convert.ToSingle(leftMargin), -Convert.ToSingle(topMargin));
    }

    private void DrawTopWatermark(ReportPage reportPage)
    {
        if (reportPage.Watermark.Enabled && reportPage.Watermark.ShowImageOnTop)
            AddImageWatermark(reportPage);

        if (reportPage.Watermark.Enabled && reportPage.Watermark.ShowTextOnTop)
            AddTextWatermark(reportPage);
    }

    private void ClosePdfDapter()
    {
        pdfAdapter?.Dispose();
    }

    /// <summary>
    /// This method is called for each band on exported reportPage.
    /// </summary>
    /// <param name="band">Band, dispose after method compite.</param>
    protected override void ExportBand(BandBase band)
    {
        base.ExportBand(band);

        if (pdfAdapter is null)
            throw new NullReferenceException($"{nameof(pdfAdapter)} is not initialized");

        // Draw the band background
        DrawBandBackground(band);

        // Draw band objects
        DrawBandObjects(band);
    }

    private void DrawBandBackground(BandBase band)
    {
        band.Draw(new FRPaintEventArgs(pdfAdapter, 1.0f, 1.0f, Report.GraphicCache));
    }

    private void DrawBandObjects(BandBase band)
    {
        foreach (Base c in band.ForEachAllConvectedObjects(this))
        {
            // Skip tables objects
            if (c is Table.TableColumn || c is Table.TableCell || c is Table.TableRow)
                continue;

            var obj = c as ReportComponentBase;

            if ((obj is null) || (obj.Exportable == false))
                continue;

            obj.Draw(new FRPaintEventArgs(pdfAdapter, 1.0f, 1.0f, Report.GraphicCache));
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

        var pageWidth = ExportUtils.GetPageWidth(reportPage);
        var pageHeight = ExportUtils.GetPageHeight(reportPage);

        pageWidth = Convert.ToSingle(PdfUtils.MmToPixel(pageWidth));
        pageHeight = Convert.ToSingle(PdfUtils.MmToPixel(pageHeight));

        var layoutRect = new RectangleF(0, 0, pageWidth, pageHeight);

        reportPage.Watermark.DrawImage(new FRPaintEventArgs(pdfAdapter, 1.0f, 1.0f, Report.GraphicCache),
                                        layoutRect,
                                        reportPage.Report, 
                                        false);        
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

        var pageWidth = ExportUtils.GetPageWidth(reportPage);
        var pageHeight = ExportUtils.GetPageHeight(reportPage);

        pageWidth = Convert.ToSingle(PdfUtils.MmToPixel(pageWidth));
        pageHeight = Convert.ToSingle(PdfUtils.MmToPixel(pageHeight));

        var layoutRect = new RectangleF(0, 0, pageWidth, pageHeight);

        reportPage.Watermark.DrawText(new FRPaintEventArgs(pdfAdapter, 1.0f, 1.0f, Report.GraphicCache),
                                        layoutRect,
                                        reportPage.Report,
                                        false);
    }
}

