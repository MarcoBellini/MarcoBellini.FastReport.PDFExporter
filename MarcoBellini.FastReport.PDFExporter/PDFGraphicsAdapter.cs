using FastReport;
using MarcoBellini.FastReport.PDFExporter.Skia;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace MarcoBellini.FastReport.PDFExporter;

/// <summary>
/// FastReport <see cref="IGraphics"/> implementation that draws on a Skia PDF page canvas.
/// All coordinates are FastReport pixels (96 DPI); the canvas base matrix converts them to PDF points.
/// </summary>
internal sealed class PDFGraphicsAdapter : IGraphics
{
    private readonly SKCanvas canvas;
    private readonly PdfRenderContext context;
    private readonly CanvasStateManager state;

    public PDFGraphicsAdapter(SKCanvas canvas, PdfRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(context);

        this.canvas = canvas;
        this.context = context;
        state = new CanvasStateManager(canvas);
    }

    #region Properties

    public Graphics Graphics => throw new NotSupportedException("A GDI+ Graphics object is not available for PDF output.");

    public float DpiX => PageUnits.PixelsPerInch;

    public float DpiY => PageUnits.PixelsPerInch;

    public bool IsClipEmpty => canvas.IsClipEmpty;

    public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.SystemDefault;

    public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.Default;

    public CompositingQuality CompositingQuality { get; set; } = CompositingQuality.Default;

    public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.Default;

    public Matrix Transform
    {
        get => SkiaConvert.ToGdiMatrix(state.Transform);
        set => state.Transform = SkiaConvert.ToSKMatrix(value);
    }

    public GraphicsUnit PageUnit
    {
        get => GraphicsUnit.Pixel;
        set
        {
            if (value != GraphicsUnit.Pixel)
                throw new NotSupportedException($"GraphicsUnit '{value}' is not supported.");
        }
    }

    public Region Clip
    {
        get => new(SkiaConvert.ToRectangleF(canvas.LocalClipBounds));
        set
        {
            using var path = PathConverter.FromRegion(value);
            ApplyClip(path, CombineMode.Replace);
        }
    }

    #endregion

    public void Dispose()
    {
        state.RestorePage();
    }

    #region Lines and outlines

    public void DrawArc(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
    {
        using var path = PathConverter.Arc(SKRect.Create(x, y, width, height), startAngle, sweepAngle);
        DrawShape(path, pen, null);
    }

    public void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments, float tension)
    {
        using var path = PathConverter.Curve(points, offset, numberOfSegments, tension);
        DrawShape(path, pen, null);
    }

    public void DrawEllipse(Pen pen, float left, float top, float width, float height) =>
        FillAndDrawEllipse(pen, null, left, top, width, height);

    public void DrawEllipse(Pen pen, RectangleF rect) =>
        FillAndDrawEllipse(pen, null, rect.X, rect.Y, rect.Width, rect.Height);

    public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
    {
        using var paint = context.PaintFactory.CreateStroke(pen);
        canvas.DrawLine(x1, y1, x2, y2, paint);
    }

    public void DrawLine(Pen pen, PointF p1, PointF p2) => DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y);

    public void DrawLines(Pen pen, PointF[] points)
    {
        using var path = PathConverter.Polygon(SkiaConvert.ToSKPoints(points), false);
        DrawShape(path, pen, null);
    }

    public void DrawPath(Pen outlinePen, GraphicsPath path) => FillAndDrawPath(outlinePen, null, path);

    public void DrawPie(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
    {
        using var path = PathConverter.Pie(SKRect.Create(x, y, width, height), startAngle, sweepAngle);
        DrawShape(path, pen, null);
    }

    public void DrawPolygon(Pen pen, PointF[] points) => FillAndDrawPolygon(pen, null, points);

    public void DrawPolygon(Pen pen, Point[] points) => FillAndDrawPolygon(pen, null, points);

    public void DrawRectangle(Pen pen, float left, float top, float width, float height) =>
        FillAndDrawRectangle(pen, null, left, top, width, height);

    public void DrawRectangle(Pen pen, Rectangle rectangle) =>
        FillAndDrawRectangle(pen, null, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

    #endregion

    #region Fills

    public void FillAndDrawEllipse(Pen pen, Brush brush, RectangleF rect) =>
        FillAndDrawEllipse(pen, brush, rect.X, rect.Y, rect.Width, rect.Height);

    public void FillAndDrawEllipse(Pen? pen, Brush? brush, float left, float top, float width, float height)
    {
        using var path = PathConverter.Ellipse(SKRect.Create(left, top, width, height));
        DrawShape(path, pen, brush);
    }

    public void FillAndDrawPath(Pen? pen, Brush? brush, GraphicsPath path)
    {
        using var skPath = PathConverter.FromGraphicsPath(path);
        DrawShape(skPath, pen, brush);
    }

    public void FillAndDrawPolygon(Pen? pen, Brush? brush, Point[] points)
    {
        using var path = PathConverter.Polygon(SkiaConvert.ToSKPoints(points), true);
        DrawShape(path, pen, brush);
    }

    public void FillAndDrawPolygon(Pen? pen, Brush? brush, PointF[] points)
    {
        using var path = PathConverter.Polygon(SkiaConvert.ToSKPoints(points), true);
        DrawShape(path, pen, brush);
    }

    public void FillAndDrawRectangle(Pen? pen, Brush? brush, float left, float top, float width, float height)
    {
        using var path = PathConverter.Rectangle(SKRect.Create(left, top, width, height));
        DrawShape(path, pen, brush);
    }

    public void FillEllipse(Brush brush, float left, float top, float width, float height) =>
        FillAndDrawEllipse(null, brush, left, top, width, height);

    public void FillEllipse(Brush brush, RectangleF rect) =>
        FillAndDrawEllipse(null, brush, rect.X, rect.Y, rect.Width, rect.Height);

    public void FillPath(Brush brush, GraphicsPath path) => FillAndDrawPath(null, brush, path);

    public void FillPie(Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle)
    {
        using var path = PathConverter.Pie(SKRect.Create(x, y, width, height), startAngle, sweepAngle);
        DrawShape(path, null, brush);
    }

    public void FillPolygon(Brush brush, PointF[] points) => FillAndDrawPolygon(null, brush, points);

    public void FillPolygon(Brush brush, Point[] points) => FillAndDrawPolygon(null, brush, points);

    public void FillRectangle(Brush brush, RectangleF rect) =>
        FillAndDrawRectangle(null, brush, rect.X, rect.Y, rect.Width, rect.Height);

    public void FillRectangle(Brush brush, float left, float top, float width, float height) =>
        FillAndDrawRectangle(null, brush, left, top, width, height);

    public void FillRegion(Brush brush, Region region)
    {
        using var path = PathConverter.FromRegion(region);
        DrawShape(path, null, brush);
    }

    /// <summary>
    /// Single drawing primitive for every shape: fill first, then outline (GDI+ order).
    /// </summary>
    private void DrawShape(SKPath path, Pen? pen, Brush? brush)
    {
        if (brush is not null)
        {
            using var fill = context.PaintFactory.CreateFill(brush);
            canvas.DrawPath(path, fill);
        }

        if (pen is not null)
        {
            using var stroke = context.PaintFactory.CreateStroke(pen);
            canvas.DrawPath(path, stroke);
        }
    }

    #endregion

    #region Images

    public void DrawImage(Image image, float x, float y)
    {
        var size = GetPhysicalSize(image);
        DrawImage(image, new RectangleF(x, y, size.Width, size.Height));
    }

    public void DrawImage(Image image, RectangleF rect) =>
        DrawImageCore(image, new RectangleF(0, 0, image.Width, image.Height), rect, null);

    public void DrawImage(Image image, float x, float y, float width, float height) =>
        DrawImage(image, new RectangleF(x, y, width, height));

    public void DrawImage(Image image, RectangleF src, RectangleF dst, GraphicsUnit srcUnit) =>
        DrawImageCore(image, ToSourcePixels(image, src, srcUnit), dst, null);

    public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttr) =>
        DrawImageCore(image, ToSourcePixels(image, new RectangleF(srcX, srcY, srcWidth, srcHeight), srcUnit), destRect, imageAttr);

    public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttrs) =>
        DrawImageCore(image, ToSourcePixels(image, new RectangleF(srcX, srcY, srcWidth, srcHeight), srcUnit), destRect, imageAttrs);

    /// <summary>
    /// Draw the image into the parallelogram defined by its upper-left, upper-right and lower-left corners.
    /// </summary>
    public void DrawImage(Image image, PointF[] points)
    {
        if (points.Length != 3)
            throw new ArgumentException("Points array must contain exactly 3 points for parallelogram transformation.", nameof(points));

        var count = canvas.Save();
        var affine = GetParallelogramMatrix(points, image.Width, image.Height);

        canvas.Concat(in affine);
        DrawImage(image, new RectangleF(0, 0, image.Width, image.Height));

        canvas.RestoreToCount(count);
    }

    public void DrawImageUnscaled(Image image, Rectangle rect)
    {
        var count = canvas.Save();
        canvas.ClipRect(SkiaConvert.ToSKRect(rect));

        DrawImage(image, rect.X, rect.Y);

        canvas.RestoreToCount(count);
    }

    /// <summary>
    /// Maps the image rectangle (0, 0, width, height) onto the destination parallelogram.
    /// </summary>
    internal static SKMatrix GetParallelogramMatrix(PointF[] points, float width, float height)
    {
        var (p0, p1, p2) = (points[0], points[1], points[2]);

        return new SKMatrix(
            (p1.X - p0.X) / width, (p2.X - p0.X) / height, p0.X,
            (p1.Y - p0.Y) / width, (p2.Y - p0.Y) / height, p0.Y,
            0, 0, 1);
    }

    private void DrawImageCore(Image image, RectangleF sourcePixels, RectangleF destination, ImageAttributes? attributes)
    {
        var dst = SkiaConvert.ToSKRect(destination);
        using var paint = new SKPaint { IsAntialias = true };

        if (attributes is null)
        {
            var skImage = context.ImageConverter.Convert(image);
            canvas.DrawImage(skImage, SkiaConvert.ToSKRect(sourcePixels), dst, GetSampling(), paint);
            return;
        }

        var source = Rectangle.Round(sourcePixels);
        using var adjusted = context.ImageConverter.ConvertWithAttributes(image, source, attributes);
        canvas.DrawImage(adjusted, SKRect.Create(adjusted.Width, adjusted.Height), dst, GetSampling(), paint);
    }

    private SKSamplingOptions GetSampling() =>
        InterpolationMode is InterpolationMode.NearestNeighbor
            ? new SKSamplingOptions(SKFilterMode.Nearest)
            : new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

    /// <summary>
    /// Image size at its own resolution, in 96 DPI pixels (GDI+ DrawImage(x, y) semantics).
    /// </summary>
    internal static SizeF GetPhysicalSize(Image image) => new(
        image.Width * PageUnits.PixelsPerInch / NormalizeResolution(image.HorizontalResolution),
        image.Height * PageUnits.PixelsPerInch / NormalizeResolution(image.VerticalResolution));

    /// <summary>
    /// Convert a source rectangle expressed in <paramref name="unit"/> into image pixels.
    /// </summary>
    internal static RectangleF ToSourcePixels(Image image, RectangleF rect, GraphicsUnit unit)
    {
        var dpiX = NormalizeResolution(image.HorizontalResolution);
        var dpiY = NormalizeResolution(image.VerticalResolution);

        var (scaleX, scaleY) = unit switch
        {
            GraphicsUnit.Point => (dpiX / 72f, dpiY / 72f),
            GraphicsUnit.Inch => (dpiX, dpiY),
            GraphicsUnit.Millimeter => (dpiX / 25.4f, dpiY / 25.4f),
            GraphicsUnit.Document => (dpiX / 300f, dpiY / 300f),
            GraphicsUnit.Display => (dpiX / PageUnits.PixelsPerInch, dpiY / PageUnits.PixelsPerInch),
            _ => (1f, 1f)
        };

        return new RectangleF(rect.X * scaleX, rect.Y * scaleY, rect.Width * scaleX, rect.Height * scaleY);
    }

    private static float NormalizeResolution(float dpi) => dpi > 0 ? dpi : PageUnits.PixelsPerInch;

    #endregion

    #region Text

    public void DrawString(string text, Font font, Brush brush, float left, float top) =>
        DrawString(text, font, brush, new RectangleF(left, top, 0, 0), StringFormat.GenericDefault);

    public void DrawString(string text, Font font, Brush brush, float left, float top, StringFormat format) =>
        DrawString(text, font, brush, new RectangleF(left, top, 0, 0), format);

    public void DrawString(string s, Font font, Brush brush, PointF point, StringFormat format) =>
        DrawString(s, font, brush, new RectangleF(point.X, point.Y, 0, 0), format);

    public void DrawString(string text, Font font, Brush brush, RectangleF textRect) =>
        DrawString(text, font, brush, textRect, StringFormat.GenericDefault);

    public void DrawString(string text, Font font, Brush brush, RectangleF textRect, StringFormat format)
    {
        if (string.IsNullOrEmpty(text))
            return;

        format ??= StringFormat.GenericDefault;

        var layout = context.TextLayoutEngine.Layout(text, font, textRect, format);
        var clip = textRect.Width > 0 && textRect.Height > 0 && !format.FormatFlags.HasFlag(StringFormatFlags.NoClip);

        using var paint = context.PaintFactory.CreateFill(brush);
        var count = canvas.Save();

        if (clip)
            canvas.ClipRect(SkiaConvert.ToSKRect(textRect));

        context.TextRenderer.Draw(canvas, layout, font, paint);

        canvas.RestoreToCount(count);
    }

    public Region[] MeasureCharacterRanges(string text, Font font, RectangleF textRect, StringFormat format) =>
        context.StringMeasurer.MeasureCharacterRanges(text, font, textRect, format);

    public SizeF MeasureString(string text, Font font) =>
        context.StringMeasurer.MeasureString(text, font);

    public SizeF MeasureString(string text, Font font, SizeF size) =>
        context.StringMeasurer.MeasureString(text, font, size);

    public SizeF MeasureString(string text, Font font, int v, StringFormat format) =>
        context.StringMeasurer.MeasureString(text, font, v, format);

    public void MeasureString(string text, Font font, SizeF size, StringFormat format, out int charsFit, out int linesFit) =>
        context.StringMeasurer.MeasureString(text, font, size, format, out charsFit, out linesFit);

    public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat) =>
        context.StringMeasurer.MeasureString(text, font, layoutArea, stringFormat);

    #endregion

    #region State, transform and clipping

    public IGraphicsState Save() => new PdfGraphicsState(state.Save());

    public void Restore(IGraphicsState graphicsState)
    {
        if (graphicsState is not PdfGraphicsState pdfState)
            throw new ArgumentException($"State must be created by {nameof(PDFGraphicsAdapter)}.", nameof(graphicsState));

        state.Restore(pdfState.SaveCount);
    }

    public void MultiplyTransform(Matrix matrix, MatrixOrder prepend) =>
        state.MultiplyTransform(SkiaConvert.ToSKMatrix(matrix), prepend == MatrixOrder.Prepend);

    public void RotateTransform(float angle) => canvas.RotateDegrees(angle);

    public void ScaleTransform(float scaleX, float scaleY) => canvas.Scale(scaleX, scaleY);

    public void TranslateTransform(float left, float top) => canvas.Translate(left, top);

    public bool IsVisible(RectangleF rect) => !canvas.QuickReject(SkiaConvert.ToSKRect(rect));

    public void ResetClip() => state.ResetClip();

    /// <summary>
    /// GDI+ <c>SetClip(RectangleF)</c> replaces the current clip.
    /// </summary>
    public void SetClip(RectangleF rect) => SetClip(rect, CombineMode.Replace);

    public void SetClip(RectangleF rect, CombineMode combineMode)
    {
        using var path = PathConverter.Rectangle(SkiaConvert.ToSKRect(rect));
        ApplyClip(path, combineMode);
    }

    public void SetClip(GraphicsPath path, CombineMode combineMode)
    {
        using var skPath = PathConverter.FromGraphicsPath(path);
        ApplyClip(skPath, combineMode);
    }

    /// <summary>
    /// Skia clips can only shrink: Replace resets the clip first, Exclude subtracts the path.
    /// Union / Xor / Complement cannot be expressed and leave the clip unchanged.
    /// </summary>
    private void ApplyClip(SKPath path, CombineMode combineMode)
    {
        switch (combineMode)
        {
            case CombineMode.Replace:
                state.ResetClip();
                canvas.ClipPath(path, SKClipOperation.Intersect, true);
                break;
            case CombineMode.Intersect:
                canvas.ClipPath(path, SKClipOperation.Intersect, true);
                break;
            case CombineMode.Exclude:
                canvas.ClipPath(path, SKClipOperation.Difference, true);
                break;
        }
    }

    #endregion
}
