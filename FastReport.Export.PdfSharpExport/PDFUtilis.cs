using PdfSharp.Drawing;
using FastReport.Utils;
using System.Drawing;
using System.Drawing.Drawing2D;
using PdfSharp.Drawing.Layout;


namespace FastReport.Export.PDFSharpExport;

internal class PdfUtils
{
    public static double MmToPoints(double mm)
    {
        
        return (mm * 72.0) / 25.4;
    }

    public static double PixelToMm(double pixels)
    {
        return pixels / Units.Millimeters;
    }

    public static double PixelToPoints(double pixels)
    {
       
        return MmToPoints(PixelToMm(pixels));
    }

    public static double MmToPixel(double mm)
    {
        return mm * Units.Millimeters;
    }

    public static double PointsToMm(double points)
    {
        return (points * 25.4) / 72.0;
    }

    public static double PointsToPixel(double points)
    {
        return MmToPixel(PointsToMm(points));
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

    public static XPen XPenFromGdiPen(Pen pen)
    {
        XPen p = new XPen(XColorFromGdiColor(pen.Color), PixelToPoints(pen.Width));            

        switch (pen.DashStyle)
        {
            case DashStyle.Solid:
                p.DashStyle = XDashStyle.Solid;
                break;
            case DashStyle.Dash:
                p.DashStyle = XDashStyle.Dash;
                break;
            case DashStyle.Dot:
                p.DashStyle = XDashStyle.Dot;
                break;
            case DashStyle.DashDot:
                p.DashStyle = XDashStyle.DashDot;
                break;
            case DashStyle.DashDotDot:
                p.DashStyle = XDashStyle.DashDotDot;
                break;
            case DashStyle.Custom:
                p.DashStyle = XDashStyle.Custom;
                break;
            default:
                p.DashStyle = XDashStyle.Solid;
                break;
        }
       
        // Add support for custom dash pattern
        if ((pen.DashStyle == DashStyle.Custom) && (pen.DashPattern.Length > 0))
        {
            p.DashOffset = Convert.ToDouble(pen.DashOffset);
            p.DashPattern = Array.ConvertAll(pen.DashPattern, c => (double)c);
        }               

        // Add support for LineJoin
        switch (pen.LineJoin)
        {
            case LineJoin.Bevel:
                p.LineJoin = XLineJoin.Bevel;
                break;
            case LineJoin.Miter:
                p.LineJoin = XLineJoin.Miter;
                break;
            case LineJoin.Round:
                p.LineJoin = XLineJoin.Round;
                break;
            default:
                p.LineJoin = XLineJoin.Miter;
                break;
        }

        p.MiterLimit = PixelToPoints(pen.MiterLimit);

        // TODO: Support line cap (PDFSharp support only left+right cap)
  
        return p;
    }


    public static XBrush XBrushFromGdiBrush(Brush brush)
    {
        XBrush _XBrush;
 
        switch(brush)
        {
            case SolidBrush:
                var _SolidBrush = (SolidBrush) brush;

                _XBrush = new XSolidBrush(XColorFromGdiColor(_SolidBrush.Color));

                break;
            case LinearGradientBrush:
                var _GradientBrush = (LinearGradientBrush)brush;

                var p1 = new XPoint(PixelToPoints(_GradientBrush.Rectangle.Left), 
                                    PixelToPoints(_GradientBrush.Rectangle.Top));

                var p2 = new XPoint(PixelToPoints(_GradientBrush.Rectangle.Right),
                                    PixelToPoints(_GradientBrush.Rectangle.Bottom));

                var colors = _GradientBrush.LinearColors;

                var c1 = XColorFromGdiColor(colors.First());
                var c2 = XColorFromGdiColor(colors.Last());

                var _XGradientBrush = new XLinearGradientBrush(p1, p2, c1, c2);

                var TrasfromMatrix = XMatrixFromGdiMatrix(_GradientBrush.Transform);

                _XGradientBrush.Transform = TrasfromMatrix;
                _XBrush = _XGradientBrush;

                break;
            default:
                // If brush is not supported, use a black solid brush
                _XBrush = new XSolidBrush(XColors.Black);
                break;
            
        }

        return _XBrush;
    }

    public static XFont XFontFromGdiFont(Font font)
    {           
        XFontStyleEx Style = XFontStyleEx.Regular;

        if (font.Bold)
            Style |= XFontStyleEx.Bold;

        if (font.Italic)
            Style |= XFontStyleEx.Italic;

        if (font.Underline)
            Style |= XFontStyleEx.Underline;

        if (font.Underline)
            Style |= XFontStyleEx.Strikeout;

        return new XFont(font.Name, font.Size, Style);
    }

    public static XStringFormat XStringFormatFromGdiFormat(StringFormat format)
    {
        XStringFormat _XStringFormat = new XStringFormat();            

        // If word wrap is not needed align text on user settings
        _XStringFormat.Alignment = XStringAlignment.Near;
        if (format.Alignment == StringAlignment.Center)
            _XStringFormat.Alignment = XStringAlignment.Center;
        else if (format.Alignment == StringAlignment.Far)
            _XStringFormat.Alignment = XStringAlignment.Far;

        _XStringFormat.LineAlignment = XLineAlignment.Near;
        if (format.LineAlignment == StringAlignment.Center)
            _XStringFormat.LineAlignment = XLineAlignment.Center;
        else if (format.LineAlignment == StringAlignment.Far)
            _XStringFormat.LineAlignment = XLineAlignment.Far;

        return _XStringFormat;
    }

    public static XParagraphAlignment GetParagraphAlignment(StringFormat format)
    {
        XParagraphAlignment Alignment;


        switch (format.Alignment)
        {
            case StringAlignment.Near:
                Alignment = XParagraphAlignment.Left;
                break;
            case StringAlignment.Center:
                Alignment = XParagraphAlignment.Center;
                break;
            case StringAlignment.Far:
                Alignment = XParagraphAlignment.Right;
                break;
            default:
                Alignment = XParagraphAlignment.Default;
                break;
        }          

        return Alignment;
    }

    public static XGraphicsUnit? XGraphicsUnitFromGdiUnit(GraphicsUnit unit)
    {
        XGraphicsUnit? _XGraphicsUnit;

        switch (unit)
        {
            case GraphicsUnit.Point:
                _XGraphicsUnit = XGraphicsUnit.Point;
                break;
            case GraphicsUnit.Inch:
                _XGraphicsUnit = XGraphicsUnit.Inch;
                break;             
            case GraphicsUnit.Millimeter:
                _XGraphicsUnit = XGraphicsUnit.Millimeter;
                break;
            default:
                _XGraphicsUnit = null;
                break;
        }

        return _XGraphicsUnit;
    }

    private static bool IsClosed(byte pathType)
    {
        return (pathType & (byte)PathPointType.CloseSubpath) != 0;
    }

    public static XGraphicsPath XGraphicsPathFromGdiPath(GraphicsPath gdiPath)
    {
        var xPath = new XGraphicsPath();
        var points = gdiPath.PathPoints;
        var types = gdiPath.PathTypes;

        int i = 0;
        while (i < points.Length)
        {           
            byte pointType = (byte)(types[i] & (int)PathPointType.PathTypeMask);

            // Start a new figure if needed
            if (pointType == (byte)PathPointType.Start)
            {
                xPath.StartFigure();
                i++;
                continue;
            }

            // Line segment
            if (pointType == (byte)PathPointType.Line)
            {
                var p1 = XPointFromGdiPoint(points[i - 1]);
                var p2 = XPointFromGdiPoint(points[i]);
                xPath.AddLine(p1, p2);
                if (IsClosed(types[i]))
                    xPath.CloseFigure();

                i++;
                continue;
            }

            // Bezier segment (3 points define 1 Bézier)
            if (pointType == (byte)PathPointType.Bezier)
            {
                if (i >= 3)
                {
                    var p0 = XPointFromGdiPoint(points[i - 3]);
                    var p1 = XPointFromGdiPoint(points[i - 2]);
                    var p2 = XPointFromGdiPoint(points[i - 1]);
                    var p3 = XPointFromGdiPoint(points[i]);
                    xPath.AddBezier(p0, p1, p2, p3);
                }

                if (IsClosed(types[i]))
                    xPath.CloseFigure();

                i++;
                continue;
            }

            // Unknown segment type: skip safely
            i++;
        }

        return xPath;
    }

}
