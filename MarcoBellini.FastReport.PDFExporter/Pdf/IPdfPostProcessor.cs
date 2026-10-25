namespace MarcoBellini.FastReport.PDFExporter.Pdf;

/// <summary>
/// Transforms the complete PDF produced by Skia before it is written to the export stream.
/// </summary>
internal interface IPdfPostProcessor
{
    byte[] Process(byte[] pdf);
}
