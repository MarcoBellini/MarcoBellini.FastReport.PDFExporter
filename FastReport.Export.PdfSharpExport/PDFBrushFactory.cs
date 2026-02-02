using PdfSharp.Drawing;
using System.Drawing;

namespace FastReport.Export.PdfExporter;

internal interface PDFBrushFactory
{
    public XBrush CreateXBrush(Brush brush);
}
