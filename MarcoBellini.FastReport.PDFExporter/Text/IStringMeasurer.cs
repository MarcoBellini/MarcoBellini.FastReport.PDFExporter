using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Text;

/// <summary>
/// Measurement services exposed by <see cref="FastReport.IGraphics"/>. FastReport computes its layout with
/// GDI+ metrics, so these results must match <see cref="Graphics"/> exactly.
/// </summary>
internal interface IStringMeasurer
{
    SizeF MeasureString(string text, Font font);

    SizeF MeasureString(string text, Font font, SizeF layoutArea);

    SizeF MeasureString(string text, Font font, int width, StringFormat format);

    SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat format);

    void MeasureString(string text, Font font, SizeF layoutArea, StringFormat format, out int charsFit, out int linesFit);

    Region[] MeasureCharacterRanges(string text, Font font, RectangleF layoutRect, StringFormat format);
}
