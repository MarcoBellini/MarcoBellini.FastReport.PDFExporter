using PdfSharp.Drawing;
using FastReport.Utils;
using System.Drawing;
using System.Drawing.Drawing2D;
using PdfSharp.Drawing.Layout;


namespace FastReport.Export.PdfExporter;


internal static class PdfUtils
{    

    public const double PointsPerPixel = 0.75;
    public const double PointsPerInch = 72.0;
    public const double MillimetersPerInch = 25.4;


    public static double MmToPoints(double mm)
    {
        return (mm * PointsPerInch) / MillimetersPerInch;
    }

    public static double PointsToMm(double points)
    {
        return (points * MillimetersPerInch) / PointsPerInch;
    }

    public static double PixelToMm(double pixels)
    {     
        return pixels / Units.Millimeters;
    }

    public static double MmToPixel(double mm)
    {
        return mm * Units.Millimeters;
    }

    public static double PixelToPoints(double pixels)
    {
        return pixels * PointsPerPixel;
    }

    public static double PointsToPixel(double points)
    {
        return points / PointsPerPixel;
    }

    public static XMatrix XMatrixFromGdiMatrix(System.Drawing.Drawing2D.Matrix m)
    {
        var xm = new XMatrix();
        
        xm.M11 = m.MatrixElements.M11;
        xm.M12 = m.MatrixElements.M12;
        xm.M21 = m.MatrixElements.M21;
        xm.M22 = m.MatrixElements.M22;


        xm.OffsetX = PixelToPoints(m.OffsetX);
        xm.OffsetY = PixelToPoints(m.OffsetY);

        return xm;
    }

    public static XRect XRectFromGdiRect(Rectangle rect)
    {
        double x = PixelToPoints(rect.X);
        double y = PixelToPoints(rect.Y);
        double width = PixelToPoints(rect.Width);
        double height = PixelToPoints(rect.Height);

        return new XRect(x, y, width, height);
    }

    public static XRect XRectFromGdiRect(RectangleF rect)
    {
        double x = PixelToPoints(rect.X);
        double y = PixelToPoints(rect.Y);
        double width = PixelToPoints(rect.Width);
        double height = PixelToPoints(rect.Height);

        return new XRect(x, y, width, height);
    }

    public static XPoint XPointFromGdiPoint(Point point)
    {
        double x = PixelToPoints(point.X);
        double y = PixelToPoints(point.Y);

        return new XPoint(x, y);
    }

    public static XPoint XPointFromGdiPoint(PointF point)
    {
        double x = PixelToPoints(point.X);
        double y = PixelToPoints(point.Y);

        return new XPoint(x, y);
    }

    public static XPoint[] XPointArrayFromGdiPoint(Point[] points)
    {
        if (points.Length == 0)
            return Array.Empty<XPoint>();

        XPoint[] xPoints = new XPoint[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            xPoints[i] = XPointFromGdiPoint(points[i]);
        }

        return xPoints;
    }

    public static XPoint[] XPointArrayFromGdiPoint(PointF[] points)
    {
        if (points.Length == 0)
            return Array.Empty<XPoint>();

        XPoint[] xPoints = new XPoint[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            xPoints[i] = XPointFromGdiPoint(points[i]);
        }

        return xPoints;
    }

    public static XSize XSizeFromGdiSize(Size size)
    {
        var w = PixelToPoints(size.Width);
        var h = PixelToPoints(size.Height);

        return new XSize(w, h);
    }

    public static XSize XSizeFromGdiSize(SizeF size)
    {
        var w = PixelToPoints(size.Width);
        var h = PixelToPoints(size.Height);

        return new XSize(w, h);
    }

    public static XColor XColorFromGdiColor(Color color)
    {
        return XColor.FromArgb(color.ToArgb()); 
    }

    public static XPen XPenFromGdiPen(Pen gdiPen)
    {
        XPen pen = new(XColorFromGdiColor(gdiPen.Color), PixelToPoints(gdiPen.Width));

        ApplyDashStyleFromGdiPen(gdiPen, pen);

        ApplyLineJoinFromGdiPen(gdiPen, pen);

        ApplyMiterLimitFromGdiPen(gdiPen, pen);

        ApplyLineCapFromGdiPen(gdiPen, pen);

        return pen;
    }

    private static void ApplyDashStyleFromGdiPen(Pen gdiPen, XPen pen)
    {
        switch (gdiPen.DashStyle)
        {
            case DashStyle.Solid:
                pen.DashStyle = XDashStyle.Solid;
                break;
            case DashStyle.Dash:
                pen.DashStyle = XDashStyle.Dash;
                break;
            case DashStyle.Dot:
                pen.DashStyle = XDashStyle.Dot;
                break;
            case DashStyle.DashDot:
                pen.DashStyle = XDashStyle.DashDot;
                break;
            case DashStyle.DashDotDot:
                pen.DashStyle = XDashStyle.DashDotDot;
                break;
            case DashStyle.Custom:
                pen.DashStyle = XDashStyle.Custom;

                if (gdiPen.DashPattern.Length > 0)
                    ApplyCustomDashStyle(gdiPen, pen);

                break;
            default:
                pen.DashStyle = XDashStyle.Solid;
                break;
        }
    }

    private static void ApplyCustomDashStyle(Pen gdiPen, XPen pen)
    {
        pen.DashOffset = Convert.ToDouble(gdiPen.DashOffset);
        pen.DashPattern = Array.ConvertAll(gdiPen.DashPattern, c => (double)c);
    }

    private static void ApplyLineJoinFromGdiPen(Pen gdiPen, XPen pen)
    {
        switch (gdiPen.LineJoin)
        {
            case LineJoin.Bevel:
                pen.LineJoin = XLineJoin.Bevel;
                break;
            case LineJoin.Miter:
                pen.LineJoin = XLineJoin.Miter;
                break;
            case LineJoin.Round:
                pen.LineJoin = XLineJoin.Round;
                break;
            default:
                pen.LineJoin = XLineJoin.Miter;
                break;
        }
    }

    private static void ApplyMiterLimitFromGdiPen(Pen gdiPen, XPen pen)
    {
        pen.MiterLimit = PixelToPoints(gdiPen.MiterLimit);
    }

    public static XBrush XBrushFromGdiBrush(Brush brush)
    {
        PDFBrushFactory BrushFactory = new PDFBrushFactoryImpl();

        return BrushFactory.CreateBrush(brush);
    }

    private static void ApplyLineCapFromGdiPen(Pen gdiPen, XPen pen)
    {
        if (gdiPen.StartCap != gdiPen.EndCap)
            throw new NotSupportedException("Different start and end line caps are not supported.");

        if (gdiPen.StartCap == LineCap.Triangle)
            throw new NotSupportedException("Triangle line cap is not supported.");

        switch (gdiPen.StartCap)
        {
            case LineCap.Flat:
                pen.LineCap = XLineCap.Flat;
                break;
            case LineCap.Square:
                pen.LineCap = XLineCap.Square;
                break;
            case LineCap.Round:
                pen.LineCap = XLineCap.Round;
                break;
            default:
                pen.LineCap = XLineCap.Flat;
                break;
        }
    }

    public static XFont XFontFromGdiFont(Font font)
    {           
        XFontStyleEx style = XFontStyleEx.Regular;

        if (font.Bold)
            style |= XFontStyleEx.Bold;

        if (font.Italic)
            style |= XFontStyleEx.Italic;

        if (font.Underline)
            style |= XFontStyleEx.Underline;

        if (font.Underline)
            style |= XFontStyleEx.Strikeout;

        return new XFont(font.Name, font.Size, style);
    }

    public static XStringFormat XStringFormatFromGdiFormat(StringFormat format)
    {
        XStringFormat pdfFormat = new()
        {
            Alignment = GetStringAlignment(format),
            LineAlignment = GetLineAlignment(format)
        };

        return pdfFormat;
    }

    private static XStringAlignment GetStringAlignment(StringFormat format)
    {
        return format.Alignment switch
        {
            StringAlignment.Near => XStringAlignment.Near,
            StringAlignment.Center =>  XStringAlignment.Center,
            StringAlignment.Far => XStringAlignment.Far,
            _ => XStringAlignment.Near,
        };   
    }

    private static XLineAlignment GetLineAlignment(StringFormat format)
    {
        return format.LineAlignment switch
        {
            StringAlignment.Near => XLineAlignment.Near,
            StringAlignment.Center => XLineAlignment.Center,
            StringAlignment.Far => XLineAlignment.Far,
            _ => XLineAlignment.Near,
        };
    }

    public static XParagraphAlignment GetParagraphAlignment(StringFormat format)
    {
        return format.Alignment switch
        {
            StringAlignment.Near => XParagraphAlignment.Left,
            StringAlignment.Center => XParagraphAlignment.Center,
            StringAlignment.Far => XParagraphAlignment.Right,
            _ => XParagraphAlignment.Default
        };
    }

    public static XGraphicsUnit GetXGraphicsUnitFromGdiUnit(GraphicsUnit unit)
    {
        return unit switch
        {
            GraphicsUnit.Point => XGraphicsUnit.Point,
            GraphicsUnit.Inch => XGraphicsUnit.Inch,
            GraphicsUnit.Millimeter => XGraphicsUnit.Millimeter,
            _ => throw new System.NotSupportedException($"GraphicsUnit '{unit}' not supported.")
        };
    }

    private static XFillMode GetXFillModeFromGdi(FillMode fill)
    {
        return fill switch
        {
            FillMode.Alternate => XFillMode.Alternate,
            FillMode.Winding => XFillMode.Winding,       
            _ => XFillMode.Alternate
        };
    }

    public static XGraphicsPath XGraphicsPathFromGdiPath(GraphicsPath gdiPath)
    {
        return new XGraphicsPath(gdiPath.PathPoints, gdiPath.PathTypes, GetXFillModeFromGdi(gdiPath.FillMode));
    }

}
