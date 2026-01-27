using PdfSharp.Pdf;
using PdfSharp.Drawing;
using FastReport.Utils;
using System.Drawing;

namespace FastReport.Export.PdfExporter;

public partial class PDFExport : ExportBase
{
    
    private PdfDocument? _Document;
    private PdfPage? _Page;
    private PDFGraphicsAdapter? _Bind;  

    public PDFExport()
    { 

    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    /// <summary>
    /// This method is called when the export starts.
    /// </summary>
    protected override void Start()
    {
        base.Start();      

        _Document = new PdfDocument();
        
        // Use report informations
        _Document.Info.Title = Report.ReportInfo.Name ;
        _Document.Info.Author = Report.ReportInfo.Author;
        _Document.Info.Comment = Report.ReportInfo.Description;           
    }

    /// <summary>
    /// This method is called at the start of exports of each page.
    /// </summary>
    /// <param name="page">Page for export may be empty in this method.</param>
    protected override void ExportPageBegin(ReportPage page)
    {
        base.ExportPageBegin(page);

        if(_Document is null)
            throw new NullReferenceException("_Document is not initialized");

        _Page = _Document.AddPage();

        var Width = ExportUtils.GetPageWidth(page);
        var Height = ExportUtils.GetPageHeight(page);

        // Convert to Points Units
        _Page.Width = XUnit.FromPoint(PdfUtils.MmToPoints(Width));
        _Page.Height = XUnit.FromPoint(PdfUtils.MmToPoints(Height));    

        var LeftMargin = PdfUtils.MmToPixel(page.LeftMargin);
        var TopMargin =  PdfUtils.MmToPixel(page.TopMargin);

        _Bind = new PDFGraphicsAdapter(XGraphics.FromPdfPage(_Page), new XSize(LeftMargin, TopMargin));

        // Draw the page background
        using (TextObject _PageFill = new TextObject())
        {
            _PageFill.Fill = page.Fill;
            _PageFill.Left = -page.LeftMargin * Units.Millimeters;
            _PageFill.Top = -page.TopMargin * Units.Millimeters;
            _PageFill.Width = ExportUtils.GetPageWidth(page) * Units.Millimeters;
            _PageFill.Height = ExportUtils.GetPageHeight(page) * Units.Millimeters;
            _PageFill.Draw(new FRPaintEventArgs(_Bind, 1.0f, 1.0f, Report.GraphicCache));
        }

        // Translate origin by Margins values
        _Bind.TranslateTransform(Convert.ToSingle(LeftMargin), Convert.ToSingle(TopMargin));        

        // Export bottom watermark
        if (page.Watermark.Enabled && !page.Watermark.ShowImageOnTop)
            AddImageWatermark(page);
        if (page.Watermark.Enabled && !page.Watermark.ShowTextOnTop)
            AddTextWatermark(page);

         
    }

    /// <summary>
    /// This method is called at the end of exports of each page.
    /// </summary>
    /// <param name="page">Page for export may be empty in this method.</param>
    protected override void ExportPageEnd(ReportPage page)
    {
        base.ExportPageEnd(page);

        // Draw page borders
        if (page.Border.Lines != BorderLines.None)
        {
            using (TextObject _PageBorder = new TextObject())
            {
                _PageBorder.Border = page.Border;
                _PageBorder.Left = 0;
                _PageBorder.Top = 0;
                _PageBorder.Width = (ExportUtils.GetPageWidth(page) - page.LeftMargin - page.RightMargin) * Units.Millimeters;
                _PageBorder.Height = (ExportUtils.GetPageHeight(page) - page.TopMargin - page.BottomMargin) * Units.Millimeters;
                _PageBorder.Draw(new FRPaintEventArgs(_Bind, 1.0f, 1.0f, Report.GraphicCache));
            }
        }

        // Export top watermark
        if (page.Watermark.Enabled && page.Watermark.ShowImageOnTop)
            AddImageWatermark(page);
        if (page.Watermark.Enabled && page.Watermark.ShowTextOnTop)
            AddTextWatermark(page);

        if (_Bind is null)
            throw new NullReferenceException("_Page is not initialized");


        _Bind?.Dispose();
    }

    /// <summary>
    /// This method is called for each band on exported page.
    /// </summary>
    /// <param name="band">Band, dispose after method compite.</param>
    protected override void ExportBand(BandBase band)
    {
        base.ExportBand(band);

        if (_Bind is null)
            throw new NullReferenceException("_Page is not initialized");

        // Draw the band background
        band.Draw(new FRPaintEventArgs(_Bind, 1.0f, 1.0f, Report.GraphicCache));

        // Draw band objects
        foreach (Base c in band.ForEachAllConvectedObjects(this))
        {
            // Skip tables objects
            if (c is Table.TableColumn || c is Table.TableCell || c is Table.TableRow)
                continue;

            var obj = c as ReportComponentBase;

            if ((obj is null) || (obj.Exportable == false))
                continue;
            
            obj.Draw(new FRPaintEventArgs(_Bind, 1.0f, 1.0f, Report.GraphicCache));                                  
        }
    }


    /// <summary>
    /// This method is called when the export is finished.
    /// </summary>
    protected override void Finish()
    {
        base.Finish();

        if (_Document is null)
            throw new NullReferenceException("_Document is not initialized");

        // Save to FastReport Stream
        _Document.Save(Stream);

        _Document.Close();          
    }

    /// <summary>
    /// Add Image Watermark to page
    /// </summary>  
    private void AddImageWatermark(ReportPage page)
    {
        if (_Bind is null)
            throw new NullReferenceException("_Page is not initialized");


        page.Watermark.DrawImage(new FRPaintEventArgs(_Bind, 1.0f, 1.0f, Report.GraphicCache),
                new RectangleF(0, 0, ExportUtils.GetPageWidth(page) * Units.Millimeters, ExportUtils.GetPageHeight(page) * Units.Millimeters),
                page.Report, false);
        
    }

    /// <summary>
    /// Add Text Watermark to page
    /// </summary>
    private void AddTextWatermark(ReportPage page)
    {

        if (_Bind is null)
            throw new NullReferenceException("_Page is not initialized");

        if (string.IsNullOrEmpty(page.Watermark.Text))
            return;

        page.Watermark.DrawText(new FRPaintEventArgs(_Bind, 1.0f, 1.0f, Report.GraphicCache),
            new RectangleF(0, 0, ExportUtils.GetPageWidth(page) * Units.Millimeters, ExportUtils.GetPageHeight(page) * Units.Millimeters),
            page.Report, false);
        
    }


}

