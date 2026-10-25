using SkiaSharp;
using System.Drawing;
using System.Drawing.Imaging;

namespace MarcoBellini.FastReport.PDFExporter.Skia;

/// <summary>
/// Converts GDI+ images into Skia images that can be drawn on a PDF canvas.
/// </summary>
internal interface IImageConverter
{
    /// <summary>
    /// Convert the image. The result is owned by the converter and must not be disposed by the caller.
    /// </summary>
    SKImage Convert(Image image);

    /// <summary>
    /// Convert the <paramref name="sourceRect"/> portion of the image after applying the GDI+ image attributes
    /// (color matrix, transparency, gamma...). The caller owns and must dispose the result.
    /// </summary>
    SKImage ConvertWithAttributes(Image image, Rectangle sourceRect, ImageAttributes attributes);
}
