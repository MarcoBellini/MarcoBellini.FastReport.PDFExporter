using PdfSharp.Drawing;
using System.Drawing;
using System.Drawing.Drawing2D;
using static FastReport.Export.PdfExporter.PdfUtils;

namespace FastReport.Export.PdfExporter;

internal class PDFBrushFactoryImpl : PDFBrushFactory
{

    public XBrush CreateBrush(Brush brush)
    {  
        switch (brush)
        {
            case SolidBrush:
                return CreateSolidBrush(brush);
            
            case LinearGradientBrush:
                return CreateLinearGradientBrush(brush);
              
            default:
                return CreateFallbackBrush();               
        }
    }

    private static XBrush CreateSolidBrush(Brush brush)
    {
        var solidBrush = (SolidBrush)brush;

        return new XSolidBrush(XColorFromGdiColor(solidBrush.Color));       
    }

    private static XBrush CreateLinearGradientBrush(Brush brush)
    {
        var gdiGradientBrush = (LinearGradientBrush)brush;


        var firstPoint = new XPoint(PixelToPoints(gdiGradientBrush.Rectangle.Left),
                                    PixelToPoints(gdiGradientBrush.Rectangle.Top));

        var lastPoint = new XPoint(PixelToPoints(gdiGradientBrush.Rectangle.Right),
                                   PixelToPoints(gdiGradientBrush.Rectangle.Bottom));



        var firstColor = XColorFromGdiColor(gdiGradientBrush.LinearColors.First());
        var lastColor = XColorFromGdiColor(gdiGradientBrush.LinearColors.Last());


        var linearGradientBrush = new XLinearGradientBrush(firstPoint, lastPoint, firstColor, lastColor);

        linearGradientBrush.Transform = XMatrixFromGdiMatrix(gdiGradientBrush.Transform);

        return linearGradientBrush;
    }

    private static XBrush CreateFallbackBrush()
    {
        return new XSolidBrush(XColors.Black);
    }
}
