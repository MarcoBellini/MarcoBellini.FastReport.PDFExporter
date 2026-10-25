using SkiaSharp;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Skia.Brushes;

/// <summary>
/// Texture brushes become an image shader honoring the brush transform and wrap mode.
/// </summary>
internal sealed class TextureBrushStrategy(IImageConverter imageConverter) : BrushStrategy<TextureBrush>
{
    protected override void Apply(TextureBrush brush, SKPaint paint)
    {
        var image = imageConverter.Convert(brush.Image);
        var tileMode = SkiaConvert.ToSKTileMode(brush.WrapMode);

        using var transform = brush.Transform;

        paint.Color = SKColors.Black;
        paint.Shader = image.ToShader(tileMode, tileMode, SKSamplingOptions.Default, SkiaConvert.ToSKMatrix(transform));
    }
}
