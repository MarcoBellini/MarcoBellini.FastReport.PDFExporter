
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
        Assert.InRange(PdfUtils.PixelToPoints(10.0), 7.4999, 7.500);
    }

    [Fact]
    public void TestPointsToPixel()
    {
        Assert.InRange(PdfUtils.PointsToPixel(7.5), 9.9999, 10.000);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(10.5)]
    [InlineData(210.0)]
    public void TestMmToPoints_And_Back(double mm)
    {
        double points = PdfUtils.MmToPoints(mm);
        double mmBack = PdfUtils.PointsToMm(points);

        Assert.InRange(mmBack, mm - Tolerance, mm + Tolerance);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(12.75)]
    [InlineData(72.0)]
    public void TestPointsToPixel_And_Back(double points)
    {
        double pixels = PdfUtils.PointsToPixel(points);
        double pointsBack = PdfUtils.PixelToPoints(pixels);

        Assert.InRange(pointsBack, points - Tolerance, points + Tolerance);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(10.0)]
    [InlineData(55.0)]
    public void TestPixelToMm_And_Back(double pixels)
    {
        double mm = PdfUtils.PixelToMm(pixels);
        double pixelsBack = PdfUtils.MmToPixel(mm);

        Assert.InRange(pixelsBack, pixels - Tolerance, pixels + Tolerance);
    }
}
