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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        pdfDocument?.Dispose();
    }

    /// <summary>
    /// This method is called when the export starts.
    /// </summary>
    protected override void Start()
    {
        base.Start();      

        pdfDocument = new PdfDocument();
        
        // Use report informations
        pdfDocument.Info.Title = Report.ReportInfo.Name ;
        pdfDocument.Info.Author = Report.ReportInfo.Author;
        pdfDocument.Info.Comment = Report.ReportInfo.Description;           
    }

    /// <summary>
    /// This method is called at the start of exports of each reportPage.
    /// </summary>
    /// <param name="reportPage">Page for export may be empty in this method.</param>
    protected override void ExportPageBegin(ReportPage reportPage)
    {
        base.ExportPageBegin(reportPage);

        if(pdfDocument is null)
            throw new NullReferenceException($"{nameof(pdfDocument)} is not initialized");

        pdfPage = pdfDocument.AddPage();

        var pageWidth = ExportUtils.GetPageWidth(reportPage);
        var pageHeight = ExportUtils.GetPageHeight(reportPage);

        // Convert to Points Units
        pdfPage.Width = XUnit.FromPoint(PdfUtils.MmToPoints(pageWidth));
        pdfPage.Height = XUnit.FromPoint(PdfUtils.MmToPoints(pageHeight));    

        var LeftMargin = PdfUtils.MmToPixel(reportPage.LeftMargin);
        var TopMargin =  PdfUtils.MmToPixel(reportPage.TopMargin);

        pdfAdapter = new PDFGraphicsAdapter(XGraphics.FromPdfPage(pdfPage), new XSize(LeftMargin, TopMargin));

        // Draw the reportPage background
        using (TextObject pageFill = new TextObject())
        {
            pageFill.Fill = reportPage.Fill;
            pageFill.Left = -reportPage.LeftMargin * Units.Millimeters;
            pageFill.Top = -reportPage.TopMargin * Units.Millimeters;
            pageFill.Width = ExportUtils.GetPageWidth(reportPage) * Units.Millimeters;
            pageFill.Height = ExportUtils.GetPageHeight(reportPage) * Units.Millimeters;
            pageFill.Draw(new FRPaintEventArgs(pdfAdapter, 1.0f, 1.0f, Report.GraphicCache));
        }     

        // Export bottom watermark
        if (reportPage.Watermark.Enabled && !reportPage.Watermark.ShowImageOnTop)
            AddImageWatermark(reportPage);
        if (reportPage.Watermark.Enabled && !reportPage.Watermark.ShowTextOnTop)
            AddTextWatermark(reportPage);

        // Translate origin by Margins values
        pdfAdapter.TranslateTransform(Convert.ToSingle(LeftMargin), Convert.ToSingle(TopMargin));
    }

    /// <summary>
    /// This method is called at the end of exports of each reportPage.
    /// </summary>
    /// <param name="reportPage">Page for export may be empty in this method.</param>
    protected override void ExportPageEnd(ReportPage reportPage)
    {
        base.ExportPageEnd(reportPage);

        // Draw reportPage borders
        if (reportPage.Border.Lines != BorderLines.None)
        {
            using (TextObject pageBorder = new TextObject())
            {
                pageBorder.Border = reportPage.Border;
                pageBorder.Left = 0;
                pageBorder.Top = 0;
                pageBorder.Width = (ExportUtils.GetPageWidth(reportPage) - reportPage.LeftMargin - reportPage.RightMargin) * Units.Millimeters;
                pageBorder.Height = (ExportUtils.GetPageHeight(reportPage) - reportPage.TopMargin - reportPage.BottomMargin) * Units.Millimeters;
                pageBorder.Draw(new FRPaintEventArgs(pdfAdapter, 1.0f, 1.0f, Report.GraphicCache));
            }
        }

        // Remove translation by Margins values
        pdfAdapter?.TranslateTransform(-Convert.ToSingle(reportPage.LeftMargin * Units.Millimeters), -Convert.ToSingle(reportPage.TopMargin * Units.Millimeters));

        // Export top watermark
        if (reportPage.Watermark.Enabled && reportPage.Watermark.ShowImageOnTop)
            AddImageWatermark(reportPage);
        if (reportPage.Watermark.Enabled && reportPage.Watermark.ShowTextOnTop)
            AddTextWatermark(reportPage);

        if (pdfAdapter is null)
            throw new NullReferenceException($"{nameof(pdfAdapter)} is not initialized");


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
        band.Draw(new FRPaintEventArgs(pdfAdapter, 1.0f, 1.0f, Report.GraphicCache));

        // Draw band objects
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

        if (pdfDocument is null)
            throw new NullReferenceException($"{nameof(pdfDocument)} is not initialized");

        // Save to FastReport Stream
        pdfDocument.Save(Stream);

        pdfDocument.Close();          
    }

    /// <summary>
    /// Add Image Watermark to reportPage
    /// </summary>  
    private void AddImageWatermark(ReportPage reporPage)
    {
        if (pdfAdapter is null)
            throw new NullReferenceException($"{nameof(pdfAdapter)} is not initialized");


        reporPage.Watermark.DrawImage(new FRPaintEventArgs(pdfAdapter, 1.0f, 1.0f, Report.GraphicCache),
                new RectangleF(0, 0, ExportUtils.GetPageWidth(reporPage) * Units.Millimeters, ExportUtils.GetPageHeight(reporPage) * Units.Millimeters),
                reporPage.Report, false);
        
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

        reportPage.Watermark.DrawText(new FRPaintEventArgs(pdfAdapter, 1.0f, 1.0f, Report.GraphicCache),
            new RectangleF(0, 0, ExportUtils.GetPageWidth(reportPage) * Units.Millimeters, ExportUtils.GetPageHeight(reportPage) * Units.Millimeters),
            reportPage.Report, false);
        
    }


}

