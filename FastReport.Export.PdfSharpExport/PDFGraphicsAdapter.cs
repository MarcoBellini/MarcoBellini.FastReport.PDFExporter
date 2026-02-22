using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Text;


namespace FastReport.Export.PdfExporter;

internal class PDFGraphicsAdapter : IGraphics
{
    private const char SpaceChar = ' ';

    private XGraphics pdfGfx;

    public Graphics Graphics => throw new NotSupportedException("Cannot enter PDFSharp gdi object");   
    public float DpiX => 96.0f;
    public float DpiY => 96.0f;
    public bool IsClipEmpty => true;         
    public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.SystemDefault;
    public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.Default;

    public System.Drawing.Drawing2D.Matrix Transform 
    {
        get
        {          
            var m = pdfGfx.Transform;
            var xmatrix = new System.Drawing.Drawing2D.Matrix(
                (float)m.M11, 
                (float)m.M12, 
                (float)m.M21, 
                (float)m.M22, 
                (float)PdfUtils.PointsToPixel(m.OffsetX), 
                (float)PdfUtils.PointsToPixel(m.OffsetY));

            return xmatrix;  
        }
        set
        {
            throw new NotSupportedException("Cannot Set Trasformation Matrix");
        }
    }


    public GraphicsUnit PageUnit
    {
        get
        {
            return GraphicsUnit.Pixel;
        }
        set
        {
            if (value != GraphicsUnit.Pixel)                
                throw new NotSupportedException("GraphicsUnit not supported");
            
        }
    }

    public Region Clip 
    {
        get 
        {
            throw new NotSupportedException("Cannot Get Clip Region");
        }
        set
        {
            throw new NotSupportedException("Cannot Set Clip Region");
        }
    } 

    public CompositingQuality CompositingQuality { get; set; } = CompositingQuality.Default;

    public SmoothingMode SmoothingMode 
    {
        get
        {
            switch(pdfGfx.SmoothingMode)
            {
                case XSmoothingMode.Default:
                    return SmoothingMode.Default;
                case XSmoothingMode.AntiAlias:
                    return SmoothingMode.AntiAlias;
                case XSmoothingMode.HighQuality:
                    return SmoothingMode.HighQuality;
                case XSmoothingMode.None:
                    return SmoothingMode.None;
                case XSmoothingMode.Invalid:
                    return SmoothingMode.Invalid;
                case XSmoothingMode.HighSpeed:
                    return SmoothingMode.HighSpeed;
                default:
                    return SmoothingMode.Default;
            }
        }
        set 
        {
            switch(value)
            {
                case SmoothingMode.Default:
                    pdfGfx.SmoothingMode = XSmoothingMode.Default;
                    break;
                case SmoothingMode.AntiAlias:
                    pdfGfx.SmoothingMode = XSmoothingMode.AntiAlias;
                    break;
                case SmoothingMode.HighQuality:
                    pdfGfx.SmoothingMode = XSmoothingMode.HighQuality;
                    break;
                case SmoothingMode.None:
                    pdfGfx.SmoothingMode = XSmoothingMode.None;
                    break;
                case SmoothingMode.Invalid:
                    pdfGfx.SmoothingMode = XSmoothingMode.Invalid;
                    break;
                case SmoothingMode.HighSpeed:
                    pdfGfx.SmoothingMode = XSmoothingMode.HighSpeed;
                    break;
                default:
                    throw new NotSupportedException("Smoothing Mode not supported");
            }
        } 
    }


    public PDFGraphicsAdapter(XGraphics pageGraphics)
    {
        ArgumentNullException.ThrowIfNull(pageGraphics);

        pdfGfx = pageGraphics;
    }

    public void Dispose()
    {
       
    }

    public void DrawArc(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
    {   
        pdfGfx.DrawArc(PdfUtils.XPenFromGdiPen(pen),
            PdfUtils.PixelToPoints(x),
            PdfUtils.PixelToPoints(y),
            PdfUtils.PixelToPoints(width),
            PdfUtils.PixelToPoints(height),
            startAngle, sweepAngle);
    }

    public void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments, float tension)
    {        
        pdfGfx.DrawCurve(PdfUtils.XPenFromGdiPen(pen),
            PdfUtils.XPointArrayFromGdiPointF(points),
            offset, numberOfSegments, tension);
    }

    public void DrawEllipse(Pen pen, float left, float top, float width, float height)
    {
        pdfGfx.DrawEllipse(PdfUtils.XPenFromGdiPen(pen),
            PdfUtils.PixelToPoints(left),
            PdfUtils.PixelToPoints(top),
            PdfUtils.PixelToPoints(width),
            PdfUtils.PixelToPoints(height));
    }

    public void DrawEllipse(Pen pen, RectangleF rect)
    {       
        DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
    }

    public void DrawImage(System.Drawing.Image image, float x, float y)
    {       
        var img = XImage.FromGdiPlusImage(image);

        pdfGfx.DrawImage(img, 
            PdfUtils.PixelToPoints(x), 
            PdfUtils.PixelToPoints(y));
    }

    public void DrawImage(System.Drawing.Image image, RectangleF src, RectangleF dst, GraphicsUnit srcUnit)
    {        
        var img = XImage.FromGdiPlusImage(image);
        var sourceRect = PdfUtils.XRectFromGdiRect(src);
        var destRect = PdfUtils.XRectFromGdiRect(dst);
        var xGraphicsUnit = PdfUtils.GetXGraphicsUnitFromGdi(srcUnit);

        pdfGfx.DrawImage(img, destRect, sourceRect, xGraphicsUnit);           
    }

    public void DrawImage(System.Drawing.Image image, RectangleF rect)
    {      
        var img = XImage.FromGdiPlusImage(image);
        var destRect = PdfUtils.XRectFromGdiRect(rect);

        pdfGfx.DrawImage(img, destRect);       
    }

    public void DrawImage(System.Drawing.Image image, float x, float y, float width, float height)
    {
        DrawImage(image, new RectangleF(x, y, width, height));
    }

    public void DrawImage(System.Drawing.Image image, PointF[] points)
    {
        if(points.Length != 3)
            throw new ArgumentException("Points array must contain exactly 3 points for parallelogram transformation.");

        // Switch to GDI+ to draw image to parallelogram
        using var bitmap = new Bitmap(image.Width, image.Height);
        using var gfx = Graphics.FromImage(bitmap);
    
        gfx.DrawImage(image, points);       

        var boundingBox = GetBoundingBox(points);
   
        DrawImage(bitmap, boundingBox);
    }

    /// <summary>
    /// Get the bounding box of the parallelogram defined by the specified points.
    /// </summary>
    internal static RectangleF GetBoundingBox(PointF[] points)
    {       
        var minX = points.Min(p => p.X);
        var minY = points.Min(p => p.Y);

        var maxX = points.Max(p => p.X);
        var maxY = points.Max(p => p.Y);

        var width = maxX - minX;
        var height = maxY - minY;

        return new RectangleF(minX, minY, width, height); 
    }

    public void DrawImage(System.Drawing.Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttr)
    {        
        DrawImage(image, destRect, (float)srcX, (float)srcY, (float)srcWidth, (float)srcHeight, srcUnit, imageAttr);
    }

    public void DrawImage(System.Drawing.Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttrs)
    {
        var img = XImage.FromGdiPlusImage(image);
        var dest = PdfUtils.XRectFromGdiRect(destRect);
        var unit = PdfUtils.GetXGraphicsUnitFromGdi(srcUnit);

        var x = PdfUtils.PixelToPoints(srcX);
        var y = PdfUtils.PixelToPoints(srcY);
        var width = PdfUtils.PixelToPoints(srcWidth);
        var height = PdfUtils.PixelToPoints(srcHeight);

        var sourceRect = new XRect(x, y, width, height);

        pdfGfx.DrawImage(img, dest, sourceRect, unit);          
    }

    public void DrawImageUnscaled(System.Drawing.Image image, Rectangle rect)
    {        
        var state = pdfGfx.Save();
        var clipRect = PdfUtils.XRectFromGdiRect(rect);
        var sourcePoint = new XPoint(clipRect.X, clipRect.Y);

        pdfGfx.IntersectClip(clipRect);
        pdfGfx.DrawImage(XImage.FromGdiPlusImage(image), sourcePoint);
       
        pdfGfx.Restore(state);
    }

    public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
    {     
        var xPen = PdfUtils.XPenFromGdiPen(pen);

        var x1d = PdfUtils.PixelToPoints(x1);
        var y1d = PdfUtils.PixelToPoints(y1);
        var x2d = PdfUtils.PixelToPoints(x2);
        var y2d = PdfUtils.PixelToPoints(y2);

        pdfGfx.DrawLine(xPen, x1d, y1d, x2d, y2d);         
    }

    public void DrawLine(Pen pen, PointF p1, PointF p2)
    {
        DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y);
    }

    public void DrawLines(Pen pen, PointF[] points)
    {
        var xPointsArray = PdfUtils.XPointArrayFromGdiPointF(points);
        var xPen = PdfUtils.XPenFromGdiPen(pen);

        pdfGfx.DrawLines(xPen, xPointsArray);
    }

    public void DrawPath(Pen outlinePen, GraphicsPath path)
    {      
        var xPen = PdfUtils.XPenFromGdiPen(outlinePen);
        var xGraphicsPath = PdfUtils.XGraphicsPathFromGdiPath(path);

        pdfGfx.DrawPath(xPen, xGraphicsPath);
    }

    public void DrawPie(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
    {
        var xPen = PdfUtils.XPenFromGdiPen(pen);
        var _x = PdfUtils.PixelToPoints(x);
        var _y = PdfUtils.PixelToPoints(y);
        var _w = PdfUtils.PixelToPoints(width);
        var _h = PdfUtils.PixelToPoints(height);

        pdfGfx.DrawPie(xPen, _x, _y, _w, _h, startAngle, sweepAngle);
    }

    public void DrawPolygon(Pen pen, PointF[] points)
    {
        var xPen = PdfUtils.XPenFromGdiPen(pen);
        var xPointsArray = PdfUtils.XPointArrayFromGdiPointF(points);

        pdfGfx.DrawPolygon(xPen, xPointsArray);
    }

    public void DrawPolygon(Pen pen, Point[] points)
    {
        var xPen = PdfUtils.XPenFromGdiPen(pen);
        var xPointsArray = PdfUtils.XPointArrayFromGdiPoint(points);

        pdfGfx.DrawPolygon(xPen, xPointsArray);
    }

    public void DrawRectangle(Pen pen, float left, float top, float width, float height)
    {
        var xPen = PdfUtils.XPenFromGdiPen(pen);
        var destRect = new XRect(PdfUtils.PixelToPoints(left),
                           PdfUtils.PixelToPoints(top),
                           PdfUtils.PixelToPoints(width),
                           PdfUtils.PixelToPoints(height));

        pdfGfx.DrawRectangle(xPen, destRect);
    }

    public void DrawRectangle(Pen pen, Rectangle rectangle)
    {
        var xPen = PdfUtils.XPenFromGdiPen(pen);
        var destRect = PdfUtils.XRectFromGdiRect(rectangle);

        pdfGfx.DrawRectangle(xPen, destRect);
    }

    public void DrawString(string text, Font font, Brush brush, float left, float top)
    {
        DrawString(text, font, brush, left, top, StringFormat.GenericDefault);
    }

    public void DrawString(string text, Font font, Brush brush, float left, float top, StringFormat format)
    {
        var xFont = PdfUtils.XFontFromGdiFont(font);
        var xBrush = PdfUtils.XBrushFromGdiBrush(brush);

        var x = PdfUtils.PixelToPoints(left);
        var y = PdfUtils.PixelToPoints(top);

        var xStringFormat = PdfUtils.XStringFormatFromGdiFormat(format);

        pdfGfx.DrawString(text, xFont, xBrush, x, y, xStringFormat);
    }

    public void DrawString(string text, Font font, Brush brush, RectangleF textRect)
    {
        DrawString(text, font, brush, textRect, StringFormat.GenericDefault);
    }

    public void DrawString(string text, Font font, Brush brush, RectangleF textRect, StringFormat format)
    {       
        var xFont = PdfUtils.XFontFromGdiFont(font);
        var xBrush = PdfUtils.XBrushFromGdiBrush(brush);
        var layoutRect = PdfUtils.XRectFromGdiRect(textRect);
        var textFormatter = new XTextFormatter(pdfGfx);
        var state = pdfGfx.Save();           

        textFormatter.Alignment = PdfUtils.GetParagraphAlignment(format);

        
        var preparedText = GetTextWrappedToRectWidth(text, xFont, layoutRect);
        var alignedRect = GetVerticallyAlignedRectForText(preparedText, xFont, format, layoutRect);

        pdfGfx.IntersectClip(alignedRect);
        textFormatter.DrawString(preparedText, xFont, xBrush, alignedRect);

        pdfGfx.Restore(state); 
    }

    public void DrawString(string s, Font font, Brush brush, PointF point, StringFormat format)
    {       
        DrawString(s, font, brush, point.X, point.Y, format);
    }

    public void FillAndDrawEllipse(Pen pen, Brush brush, RectangleF rect)
    {
        FillEllipse(brush, rect);
        DrawEllipse(pen, rect);
    }

    public void FillAndDrawEllipse(Pen pen, Brush brush, float left, float top, float width, float height)
    {
        FillEllipse(brush, left, top, width, height);
        DrawEllipse(pen, left, top, width, height);
    }

    public void FillAndDrawPath(Pen pen, Brush brush, GraphicsPath path)
    {
        FillPath(brush, path);
        DrawPath(pen, path);
    }

    public void FillAndDrawPolygon(Pen pen, Brush brush, Point[] points)
    {
        FillPolygon(brush, points);
        DrawPolygon(pen, points);
    }

    public void FillAndDrawPolygon(Pen pen, Brush brush, PointF[] points)
    {
        FillPolygon(brush, points);
        DrawPolygon(pen, points);
    }

    public void FillAndDrawRectangle(Pen pen, Brush brush, float left, float top, float width, float height)
    {
        FillRectangle(brush, left, top, width, height);
        DrawRectangle(pen, left, top, width, height);
    }

    public void FillEllipse(Brush brush, float left, float top, float width, float height)
    {
        var b = PdfUtils.XBrushFromGdiBrush(brush);

        var rc = new XRect(PdfUtils.PixelToPoints(left),
                           PdfUtils.PixelToPoints(top),
                           PdfUtils.PixelToPoints(width),
                           PdfUtils.PixelToPoints(height));

        pdfGfx.DrawEllipse(b, rc);
    }

    public void FillEllipse(Brush brush, RectangleF rect)
    {
        var b = PdfUtils.XBrushFromGdiBrush(brush);
        var rc =  PdfUtils.XRectFromGdiRect(rect);

        pdfGfx.DrawEllipse(b, rc);
    }

    public void FillPath(Brush brush, GraphicsPath path)
    {
        var b = PdfUtils.XBrushFromGdiBrush(brush);
        var _path = PdfUtils.XGraphicsPathFromGdiPath(path);

        pdfGfx.DrawPath(b, _path);
    }

    public void FillPie(Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle)
    {
        var b = PdfUtils.XBrushFromGdiBrush(brush);
        var _x = PdfUtils.PixelToPoints(x);
        var _y = PdfUtils.PixelToPoints(y);
        var _w = PdfUtils.PixelToPoints(width);
        var _h = PdfUtils.PixelToPoints(height);

        pdfGfx.DrawPie(b, _x, _y, _w, _h, startAngle, sweepAngle);
    }

    public void FillPolygon(Brush brush, PointF[] points)
    {
        var b = PdfUtils.XBrushFromGdiBrush(brush);
        var pts = PdfUtils.XPointArrayFromGdiPointF(points);

        pdfGfx.DrawPolygon(b, pts, XFillMode.Alternate);
    }

    public void FillPolygon(Brush brush, Point[] points)
    {
        var b = PdfUtils.XBrushFromGdiBrush(brush);
        var pts = PdfUtils.XPointArrayFromGdiPoint(points);

        pdfGfx.DrawPolygon(b, pts, XFillMode.Alternate);
    }

    public void FillRectangle(Brush brush, RectangleF rect)
    {
        var b = PdfUtils.XBrushFromGdiBrush(brush);
        var rc = PdfUtils.XRectFromGdiRect(rect);

        pdfGfx.DrawRectangle(b, rc);
    }

    public void FillRectangle(Brush brush, float left, float top, float width, float height)
    {
        FillRectangle(brush, new RectangleF(left, top, width, height));
    }

    public void FillRegion(Brush brush, Region region)
    {
        var boundingBox = GetRegionBoundingBox(region);

        using var bitmap = new Bitmap(boundingBox.Width, boundingBox.Height);
        using var gfx = Graphics.FromImage(bitmap);

        gfx.FillRegion(brush, region);

        DrawImage(bitmap, boundingBox);
    }

    internal static Rectangle GetRegionBoundingBox(Region region)
    {
        var rects = region.GetRegionScans(new System.Drawing.Drawing2D.Matrix());

        var left = Convert.ToInt32(rects.Min(r => r.Left));
        var top = Convert.ToInt32(rects.Min(r => r.Top));
        var right = Convert.ToInt32(rects.Max(r => r.Right));
        var bottom = Convert.ToInt32(rects.Max(r => r.Bottom));

        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    public bool IsVisible(RectangleF rect)
    {
        var rc = PdfUtils.XRectFromGdiRect(rect);
        var page = new XRect(0, 0, pdfGfx.PageSize.Width, pdfGfx.PageSize.Height);

        return page.Contains(rc);
    }

    public Region[] MeasureCharacterRanges(string text, Font font, RectangleF textRect, StringFormat format)
    {
        using var bitmap = new Bitmap(Convert.ToInt32(textRect.Width), Convert.ToInt32(textRect.Height));
        using var gfx = Graphics.FromImage(bitmap);

        return gfx.MeasureCharacterRanges(text, font, textRect, format);       
    }

    public SizeF MeasureString(string text, Font font)
    {
        var xfont = PdfUtils.XFontFromGdiFont(font);
        var size = pdfGfx.MeasureString(text, xfont);

        var gdiSize = size.ToSizeF();

        gdiSize.Width = Convert.ToSingle(PdfUtils.PointsToPixel(gdiSize.Width));
        gdiSize.Height = Convert.ToSingle(PdfUtils.PointsToPixel(gdiSize.Height));

        return gdiSize;
    }

    public SizeF MeasureString(string text, Font font, SizeF size)
    {
        var sz = MeasureString(text, font);

        sz.Width = Math.Min(sz.Width, size.Width);
        sz.Height = Math.Min(sz.Height, size.Height);

        return sz;
    }

    public SizeF MeasureString(string text, Font font, int v, StringFormat format)
    {
        var xfont = PdfUtils.XFontFromGdiFont(font);
        var xformat = PdfUtils.XStringFormatFromGdiFormat(format);

        var size = pdfGfx.MeasureString(text, xfont, xformat);
        var gdiSize = size.ToSizeF();

        gdiSize.Width = Convert.ToSingle(PdfUtils.PointsToPixel(gdiSize.Width));
        gdiSize.Height = Convert.ToSingle(PdfUtils.PointsToPixel(gdiSize.Height));

        gdiSize.Width = Math.Min(v, gdiSize.Width);

        return gdiSize;
    }

    public void MeasureString(string text, Font font, SizeF size, StringFormat format, out int charsFit, out int linesFit)
    {
        using var bitmap = new Bitmap(Convert.ToInt32(size.Width), Convert.ToInt32(size.Height));
        using var gfx = Graphics.FromImage(bitmap);

        gfx.MeasureString(text, font, size, format, out charsFit, out linesFit);
    }

    public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat)
    {          
        return MeasureString(text, font, layoutArea);
    }

    public void MultiplyTransform(System.Drawing.Drawing2D.Matrix matrix, MatrixOrder prepend)
    {
        var xm = PdfUtils.XMatrixFromGdiMatrix(matrix);

        switch(prepend)
        {
            case MatrixOrder.Append:
                pdfGfx.MultiplyTransform(xm, XMatrixOrder.Append);
                break;
            case MatrixOrder.Prepend:
                pdfGfx.MultiplyTransform(xm, XMatrixOrder.Prepend);
                break;
            default:
                throw new NotSupportedException("MatrixOrder not supported");
        }        
    }

    public void ResetClip()
    {       
        pdfGfx.IntersectClip(new XRect(0, 0, pdfGfx.PageSize.Width, pdfGfx.PageSize.Height));
    }

    public void Restore(IGraphicsState state)
    {
        pdfGfx.Restore(((PdfGraphicsState)state).GetState);
    }

    public void RotateTransform(float angle)
    {       
        pdfGfx.RotateTransform(angle);    
    }

    public IGraphicsState Save()
    {
        return new PdfGraphicsState(pdfGfx.Save());       
    }

    public void ScaleTransform(float scaleX, float scaleY)
    {       
        pdfGfx.ScaleTransform(scaleX,
                              scaleY);          
    }

    public void SetClip(RectangleF rect)
    {
        var rc = new XRect(PdfUtils.PixelToPoints(rect.Left),
                   PdfUtils.PixelToPoints(rect.Top),
                   PdfUtils.PixelToPoints(rect.Width),
                   PdfUtils.PixelToPoints(rect.Height));

        pdfGfx.IntersectClip(rc);
    }

    public void SetClip(RectangleF rect, CombineMode combineMode)
    {
        switch (combineMode)
        {
            case CombineMode.Replace:
                ResetClip();
                SetClip(rect);
                break;
            case CombineMode.Intersect:
                SetClip(rect);
                break;
            default:
                throw new NotSupportedException("Only Intersect / Replace combine mode is supported for clipping");
        }           
    }

    public void SetClip(GraphicsPath path, CombineMode combineMode)
    {
        switch (combineMode)
        {
            case CombineMode.Replace:
                SetGraphicsPathClip(path);
                ResetClip();
                break;
            case CombineMode.Intersect:
                SetGraphicsPathClip(path);
                break;
            default:
                throw new NotSupportedException("Only Intersect / Replace combine mode is supported for clipping");
        }    
    }

    /// <summary>
    /// Use Gdi GraphicsPath to set clipping region.
    /// </summary>
    private void SetGraphicsPathClip(GraphicsPath path)
    {
        var xGraphicsPath = PdfUtils.XGraphicsPathFromGdiPath(path);

        pdfGfx.IntersectClip(xGraphicsPath);
    }

    public void TranslateTransform(float left, float top)
    {        
        pdfGfx.TranslateTransform(PdfUtils.PixelToPoints(left),
                                  PdfUtils.PixelToPoints(top));       
    }

    /// <summary>
    /// Resize source rectangle Y coordinate and maxY to align text vertically in the rectangle 
    /// according to StringFormat.LineAlignment property. 
    /// 
    /// This is needed because PdfSharp does not support vertical alignment of text.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="font"></param>
    /// <param name="format"></param>
    /// <param name="sourceRect"></param>
    /// <returns></returns>
    internal XRect GetVerticallyAlignedRectForText(string text, XFont font, StringFormat format, XRect sourceRect)
    {           
        var hasNoWrap = format.FormatFlags.HasFlag(StringFormatFlags.NoWrap);

        var textSize = pdfGfx.MeasureString(text, font);
        var lineCount = hasNoWrap ? 1 : Math.Ceiling(textSize.Width / sourceRect.Width);
        var textHeight = Math.Min(textSize.Height * lineCount, sourceRect.Height);

        double verticalOffset = format.LineAlignment switch
        {
            StringAlignment.Near => 0.0,
            StringAlignment.Center => (sourceRect.Height - textHeight) / 2.0,
            StringAlignment.Far => sourceRect.Height - textHeight,
            _ => 0.0
        };

        var newY = sourceRect.Y + verticalOffset;
        var newHeight = sourceRect.Height - verticalOffset;

        return new(sourceRect.X, newY, sourceRect.Width, newHeight);
    }

    /// <summary>
    /// Returns text with added line breaks to fit the text within the specified rectangle maxX.
    /// </summary>
    /// <param name="text">Text to prepare</param>
    /// <param name="font"><c>XFont</c> object used to draw text</param>
    /// <param name="rect">Destination rectangle</param>
    /// <returns>Prepared text for <c>DrawString</c> function of PDFSharp</returns>
    internal string GetTextWrappedToRectWidth(string text, XFont font, XRect rect)
    {
        var result = new StringBuilder(text.Length);      
        var words = text.Split(SpaceChar);

        for (int i = 0; i < words.Length; i++)       
        {
            var word = words[i];

            if (IsWordWiderThanRect(word, font, rect.Width))
                result.Append(SplitWordToFitWidth(word, font, rect.Width));
            else
                result.Append(word);

            if (i < words.Length - 1)
                result.Append(SpaceChar);
        }

        return result.ToString();
    }

    /// <summary>
    /// Check if the word is wider than the specified maxX when drawn with the specified font.
    /// </summary>
    private bool IsWordWiderThanRect(string word, XFont font, double maxWidth)
    {
        return pdfGfx.MeasureString(word, font).Width > maxWidth;
    }

    /// <summary>
    /// Split the word into multiple lines by adding line breaks so that each line fits within the 
    /// specified maxX when drawn with the specified font.
    /// </summary>
    private string SplitWordToFitWidth(string word, XFont font, double maxWidth)
    {
        var builder = new StringBuilder(word.Length + 1);
        double currentWidth = 0;

        foreach (var ch in word)
        {
            var charWidth = pdfGfx.MeasureString(ch.ToString(), font).Width;

            if (currentWidth + charWidth > maxWidth)
            {
                builder.Append(SpaceChar);
                currentWidth = 0;
            }

            builder.Append(ch);
            currentWidth += charWidth;
        }

        return builder.ToString();
    }
}
