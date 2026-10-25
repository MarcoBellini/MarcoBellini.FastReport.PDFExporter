using FastReport.Utils;
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter;

/// <summary>
/// Unit conversions between FastReport pixels (96 DPI), millimeters and PDF points.
/// </summary>
internal static class PageUnits
{
    public const double PointsPerPixel = 0.75;
    public const double PointsPerInch = 72.0;
    public const double MillimetersPerInch = 25.4;
    public const float PixelsPerInch = 96.0f;

    public static double MmToPoints(double mm) => mm * PointsPerInch / MillimetersPerInch;

    public static double PointsToMm(double points) => points * MillimetersPerInch / PointsPerInch;

    public static double PixelToMm(double pixels) => pixels / Units.Millimeters;

    public static double MmToPixel(double mm) => mm * Units.Millimeters;

    public static double PixelToPoints(double pixels) => pixels * PointsPerPixel;

    public static double PointsToPixel(double points) => points / PointsPerPixel;

    /// <summary>
    /// Em size of a font in FastReport pixels, independent of the GDI+ font unit.
    /// </summary>
    public static float FontPixelSize(Font font) => font.SizeInPoints * PixelsPerInch / (float)PointsPerInch;
}
