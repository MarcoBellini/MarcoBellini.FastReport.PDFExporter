using SkiaSharp;
using System.Drawing;
using System.Runtime.InteropServices;

namespace MarcoBellini.FastReport.PDFExporter.Skia;

/// <summary>
/// Loads the font file GDI selects for a <see cref="Font"/>. GDI matches legacy family names
/// (e.g. "Arial Narrow", "Segoe UI Semibold") that DirectWrite groups under their typographic family.
/// </summary>
internal static class GdiFontLoader
{
    private const uint GdiError = 0xFFFFFFFF;

    // 'ttcf' as the little-endian DWORD GetFontData expects.
    private const uint TtcfTable = 0x66637474;

    public static SKTypeface? TryLoad(Font font)
    {
        var hfont = font.ToHfont();
        var hdc = CreateCompatibleDC(IntPtr.Zero);
        var previous = SelectObject(hdc, hfont);

        try
        {
            // shortcut: faces inside .ttc collections are skipped (table 0 offsets point into the collection), add face lookup if a legacy family ships as .ttc.
            if (GetFontData(hdc, TtcfTable, 0, null, 0) != GdiError)
                return null;

            var size = GetFontData(hdc, 0, 0, null, 0);

            if (size is 0 or GdiError)
                return null;

            var data = new byte[size];

            if (GetFontData(hdc, 0, 0, data, size) != size)
                return null;

            return SKTypeface.FromData(SKData.CreateCopy(data));
        }
        finally
        {
            SelectObject(hdc, previous);
            DeleteDC(hdc);
            DeleteObject(hfont);
        }
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern uint GetFontData(IntPtr hdc, uint table, uint offset, byte[]? buffer, uint length);
}
