using SkiaSharp;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MarcoBellini.FastReport.PDFExporter.Skia.Brushes;

/// <summary>
/// GDI+ hatch patterns are 8x8 pixel tiles: the tile is rendered by GDI+ itself (exact pattern for every
/// <see cref="HatchStyle"/>) and repeated with an image shader.
/// </summary>
internal sealed class HatchBrushStrategy : BrushStrategy<HatchBrush>
{
    private const int TileSize = 8;

    protected override void Apply(HatchBrush brush, SKPaint paint)
    {
        using var tile = new Bitmap(TileSize, TileSize, PixelFormat.Format32bppPArgb);
        using (var gfx = Graphics.FromImage(tile))
        {
            gfx.FillRectangle(brush, 0, 0, TileSize, TileSize);
        }

        using var tileImage = GdiImageConverter.CopyPixels(tile);

        paint.Color = SKColors.Black;
        paint.Shader = tileImage.ToShader(SKShaderTileMode.Repeat, SKShaderTileMode.Repeat,
            new SKSamplingOptions(SKFilterMode.Nearest));
    }
}
