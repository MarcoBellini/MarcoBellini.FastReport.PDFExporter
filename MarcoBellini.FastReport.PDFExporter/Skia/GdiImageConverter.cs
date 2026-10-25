using SkiaSharp;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;

namespace MarcoBellini.FastReport.PDFExporter.Skia;

/// <summary>
/// GDI+ based <see cref="IImageConverter"/>. JPEG images are kept JPEG-encoded (embedded as DCT in the PDF),
/// every other image is copied losslessly. Converted images are cached per <see cref="Image"/> instance so that
/// an image drawn many times (e.g. a watermark on every page) is embedded only once.
/// </summary>
internal sealed class GdiImageConverter : IImageConverter, IDisposable
{
    private const long JpegQuality = 90L;

    private readonly ConditionalWeakTable<Image, SKImage> cache = new();
    private readonly List<SKImage> converted = [];

    public SKImage Convert(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (cache.TryGetValue(image, out var cached))
            return cached;

        var skImage = IsJpeg(image) ? EncodeAsJpeg(image) : CopyPixels(image);

        cache.AddOrUpdate(image, skImage);
        converted.Add(skImage);

        return skImage;
    }

    public SKImage ConvertWithAttributes(Image image, Rectangle sourceRect, ImageAttributes attributes)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(attributes);

        var width = Math.Max(1, sourceRect.Width);
        var height = Math.Max(1, sourceRect.Height);

        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        using (var gfx = Graphics.FromImage(bitmap))
        {
            gfx.DrawImage(image, new Rectangle(0, 0, width, height),
                sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height, GraphicsUnit.Pixel, attributes);
        }

        return CopyPixels(bitmap);
    }

    public void Dispose()
    {
        foreach (var image in converted)
            image.Dispose();

        converted.Clear();
        cache.Clear();
    }

    private static bool IsJpeg(Image image) =>
        image is Bitmap && image.RawFormat.Guid == ImageFormat.Jpeg.Guid;

    private static SKImage EncodeAsJpeg(Image image)
    {
        var encoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, JpegQuality);

        using var stream = new MemoryStream();
        image.Save(stream, encoder, parameters);

        return SKImage.FromEncodedData(stream.ToArray()) ?? CopyPixels(image);
    }

    /// <summary>
    /// Lossless pixel copy (premultiplied BGRA). Vector images (metafiles) are rasterized at their pixel size.
    /// </summary>
    internal static SKImage CopyPixels(Image image)
    {
        using var bitmap = new Bitmap(Math.Max(1, image.Width), Math.Max(1, image.Height), PixelFormat.Format32bppPArgb);
        using (var gfx = Graphics.FromImage(bitmap))
        {
            gfx.DrawImage(image, 0, 0, bitmap.Width, bitmap.Height);
        }

        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);

        try
        {
            var info = new SKImageInfo(data.Width, data.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

            return SKImage.FromPixelCopy(info, data.Scan0, data.Stride)
                ?? throw new InvalidOperationException("Unable to convert image to SKImage.");
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
