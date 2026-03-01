using PdfSharp.Drawing;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter;

internal interface PDFBrushFactory
{
    public XBrush CreateXBrush(Brush brush);
}
