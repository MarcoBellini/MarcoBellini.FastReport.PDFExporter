using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Text;

namespace FastReport.Export.PdfExporter;

internal class PDFGraphicsAdapter : IGraphics
{
    private const char Whitespace = ' ';

    private XGraphics pdfGfx;
    private Graphics gdiGfx;
    private Bitmap gdiBitmap;

    private bool DrawGdiBitmapOnDispose = false; 

    public Graphics Graphics => gdiGfx;   
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
                (float)m.M21, 
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
            throw new NotSupportedException("Cannot Set/Get Clip Region");
        }
        set
        {
            throw new NotSupportedException("Cannot Set/Get Clip Region");
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


    public PDFGraphicsAdapter(XGraphics xgraphics, XSize PageMargins)
    {
        pdfGfx = xgraphics;

        // Reduce size of the page subtracting margins
        var Width = pdfGfx.PageSize.Width - PageMargins.Width;
        var Height = pdfGfx.PageSize.Height - PageMargins.Height;            

        Width = PdfUtils.PointsToPixel(Width);
        Height = PdfUtils.PointsToPixel(Height);

        gdiBitmap = new Bitmap((int)Width, (int)Height);
        gdiGfx = Graphics.FromImage(gdiBitmap);
    }

    public void Dispose()
    {
        // Draw Bitmap drawn using GDI+
        if(DrawGdiBitmapOnDispose)
            DrawImage(gdiBitmap, 0, 0, gdiBitmap.Width, gdiBitmap.Height);

        gdiGfx.Dispose();
        gdiBitmap.Dispose();
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
        // Switch to GDI+ to draw image
        gdiGfx.DrawImage(image, points);

        DrawGdiBitmapOnDispose = true;

        Debug.WriteLine("DrawImage with GDI functions");
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
        var destRect = PdfUtils.XRectFromGdiRect(textRect);
        var textFormatter = new XTextFormatter(pdfGfx);
        var state = pdfGfx.Save();           

        textFormatter.Alignment = PdfUtils.GetParagraphAlignment(format);

        // Align text vertically and fit words to sourceRect WordWidth
        var s = FitStringToRectWidth(text, xFont, destRect);
        AlignRectVertically(s, xFont, format, ref destRect);

        pdfGfx.IntersectClip(destRect);
        textFormatter.DrawString(s, xFont, xBrush, destRect);

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
        gdiGfx.FillRegion(brush, region);

        DrawGdiBitmapOnDispose = true;

        Debug.Write("FillRegion with GDI functions");
    }

    public bool IsVisible(RectangleF rect)
    {
        var rc = PdfUtils.XRectFromGdiRect(rect);
        var page = new XRect(0, 0, pdfGfx.PageSize.Width, pdfGfx.PageSize.Height);

        return page.Contains(rc);
    }

    public Region[] MeasureCharacterRanges(string text, Font font, RectangleF textRect, StringFormat format)
    {
        Debug.Write("MeasureCharacterRanges with GDI functions");
        return gdiGfx.MeasureCharacterRanges(text, font, textRect, format);
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
        gdiGfx.MeasureString(text, font, size, format, out charsFit, out linesFit);

        Debug.Write("MeasureString with GDI functions");
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
        pdfGfx.ScaleTransform(PdfUtils.PixelToPoints(scaleX),
                            PdfUtils.PixelToPoints(scaleY));          
    }

    public void SetClip(RectangleF rect)
    {       
        pdfGfx.IntersectClip(rect);
    }

    public void SetClip(RectangleF rect, CombineMode combineMode)
    {        
        pdfGfx.IntersectClip(rect);    
    }

    public void SetClip(GraphicsPath path, CombineMode combineMode)
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
    /// Size the rectangle to align text vertically
    /// </summary>
    /// <param name="text">Text to align</param>
    /// <param name="font">Font used to draw</param>
    /// <param name="format">GDI string format (used to check LineAlignment)</param>
    /// <param name="sourceRect">Ref to current drawing rectangle</param>
    internal void AlignRectVertically(string text, XFont font, StringFormat format, ref XRect sourceRect)
    {           
        double verticalOffset;
        var noWrapChecked = format.FormatFlags.HasFlag(StringFormatFlags.NoWrap);
        var textSize = pdfGfx.MeasureString(text, font);
        var textLines = Math.Ceiling(textSize.Width / sourceRect.Width);

        // If Word Warp is true, use only one line and cut string to sourceRect Width 
        // with IntersectClip
        if (noWrapChecked)
            textLines = 1;

        var textHeight = Math.Min(textSize.Height * textLines, sourceRect.Height);

        switch (format.LineAlignment)
        {
            case StringAlignment.Near:
                verticalOffset = 0.0;
                break;
            case StringAlignment.Center:
                verticalOffset = (sourceRect.Height - textHeight) / 2.0;
                break;
            case StringAlignment.Far:
                verticalOffset = sourceRect.Height - textHeight;
                break;
            default:
                verticalOffset = 0.0;
                break;
        }                   

        sourceRect.Y += verticalOffset;
        sourceRect.Height -= verticalOffset;           
    }

    /// <summary>
    /// Fit every word of the string to fit the rectangle WordWidth (clip height with IntersectRect)
    /// </summary>
    /// <param name="text">Input string</param>
    /// <param name="font">XFont used to draw the string</param>
    /// <param name="rect">Layout rectangle</param>
    /// <returns>A string where every words fits into rectangle WordWidth</returns>
    internal string FitStringToRectWidth(string text, XFont font, XRect rect)
    {
        var TextBuilder = new StringBuilder(text.Length);      
        var Words = text.Split(Whitespace);  

        // Find words with WordWidth > sourceRect.Width and add a space to 
        // wrap word by PDFSharp TextFormatter
        for (int i = 0; i < Words.Length; i++)
        {
            var Word = Words[i];
            var WordWidth = pdfGfx.MeasureString(Word, font).Width;
            var AddWhiteSpace = (i < Words.Length - 1);

            if (WordWidth > rect.Width)
            {
                // Append char by char and add a space when new word width
                // is greater than sourceRect.Width
                var WordBuilder = new StringBuilder(Word.Length + 1);
                var Index = 0;
                var NewWordWidth = 0.0;

                while (Index < Word.Length)
                {
                    var chr = Word[Index];
                    NewWordWidth += pdfGfx.MeasureString(chr.ToString(), font).Width;

                    if (NewWordWidth > rect.Width)
                    {
                        WordBuilder.Append(Whitespace);
                        NewWordWidth = 0.0;
                    }

                    WordBuilder.Append(chr);
                    Index++;
                }

                TextBuilder.Append(WordBuilder);
            }
            else
            {
                TextBuilder.Append(Word);     
            }

            if (AddWhiteSpace)
                TextBuilder.Append(Whitespace);

        }

        return TextBuilder.ToString();
    }
}
