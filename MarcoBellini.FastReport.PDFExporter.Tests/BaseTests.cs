
using System.Drawing;

namespace MarcoBellini.FastReport.PDFExporter.Tests;

public class BaseTests
{
    private const double Tolerance = 1e-9;
    public static float Millimeters = 3.78f; // From FastReport.Utils.Units.Millimeters


    [Fact]
    public void TestPixelToPointConversionFactor()
    {
        double factor = 1.0 / Millimeters * 72.0 / 25.4;
        Assert.InRange(factor, 0.74990, 0.7500);
    }

    [Fact]
    public void TestPixelToPoints()
    {
        Assert.InRange(PageUnits.PixelToPoints(10.0), 7.4999, 7.500);
    }

    [Fact]
    public void TestPointsToPixel()
    {
        Assert.InRange(PageUnits.PointsToPixel(7.5), 9.9999, 10.000);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(10.5)]
    [InlineData(210.0)]
    public void TestMmToPoints_And_Back(double mm)
    {
        double points = PageUnits.MmToPoints(mm);
        double mmBack = PageUnits.PointsToMm(points);

        Assert.InRange(mmBack, mm - Tolerance, mm + Tolerance);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(12.75)]
    [InlineData(72.0)]
    public void TestPointsToPixel_And_Back(double points)
    {
        double pixels = PageUnits.PointsToPixel(points);
        double pointsBack = PageUnits.PixelToPoints(pixels);

        Assert.InRange(pointsBack, points - Tolerance, points + Tolerance);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(10.0)]
    [InlineData(55.0)]
    public void TestPixelToMm_And_Back(double pixels)
    {
        double mm = PageUnits.PixelToMm(pixels);
        double pixelsBack = PageUnits.MmToPixel(mm);

        Assert.InRange(pixelsBack, pixels - Tolerance, pixels + Tolerance);
    }

    [Fact]
    public void TestFontPixelSizeIsIndependentFromFontUnit()
    {
        using var pointFont = new Font("Arial", 12, GraphicsUnit.Point);
        using var pixelFont = new Font("Arial", 16, GraphicsUnit.Pixel);

        Assert.Equal(16f, PageUnits.FontPixelSize(pointFont), 3);
        Assert.Equal(16f, PageUnits.FontPixelSize(pixelFont), 3);
    }
}
