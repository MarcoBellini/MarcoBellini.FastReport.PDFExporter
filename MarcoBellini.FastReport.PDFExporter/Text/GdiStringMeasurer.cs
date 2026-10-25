using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Text;

/// <summary>
/// <see cref="IStringMeasurer"/> backed by a 96 DPI GDI+ measurement surface.
/// </summary>
internal sealed class GdiStringMeasurer : IStringMeasurer, IDisposable
{
    private readonly Bitmap surface;
    private readonly Graphics graphics;

    public GdiStringMeasurer()
    {
        surface = new Bitmap(1, 1);
        surface.SetResolution(PageUnits.PixelsPerInch, PageUnits.PixelsPerInch);

        graphics = Graphics.FromImage(surface);
        graphics.PageUnit = GraphicsUnit.Pixel;
    }

    public SizeF MeasureString(string text, Font font) => graphics.MeasureString(text, font);

    public SizeF MeasureString(string text, Font font, SizeF layoutArea) => graphics.MeasureString(text, font, layoutArea);

    public SizeF MeasureString(string text, Font font, int width, StringFormat format) => graphics.MeasureString(text, font, width, format);

    public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat format) => graphics.MeasureString(text, font, layoutArea, format);

    public void MeasureString(string text, Font font, SizeF layoutArea, StringFormat format, out int charsFit, out int linesFit) =>
        graphics.MeasureString(text, font, layoutArea, format, out charsFit, out linesFit);

    public Region[] MeasureCharacterRanges(string text, Font font, RectangleF layoutRect, StringFormat format) =>
        graphics.MeasureCharacterRanges(text, font, layoutRect, format);

    public void Dispose()
    {
        graphics.Dispose();
        surface.Dispose();
    }
}
