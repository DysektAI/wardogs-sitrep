using Sitrep.Core;

namespace Sitrep.Tests;

public sealed class GeoMathTests
{
    [Fact]
    public void NumericalFixture()
    {
        var origin = new MapCoordinate(101.53, 107.77);
        var target = new MapCoordinate(101.66, 110.10);
        Assert.True(GeoMath.TryCompute(origin, target, out double range, out double bearing, out _));
        Assert.Equal(13.0, (target.X - origin.X) * 100.0, 9);
        Assert.Equal(233.0, (target.Y - origin.Y) * 100.0, 9);
        Assert.Equal(233.36237914454, range, 6);
        Assert.Equal(3.19344927328, bearing, 6);
    }

    [Theory]
    [InlineData(0, 0, 0, 1, 100.0, 0.0)]
    [InlineData(0, 0, 1, 0, 100.0, 90.0)]
    [InlineData(0, 0, 0, -1, 100.0, 180.0)]
    [InlineData(0, 0, -1, 0, 100.0, 270.0)]
    [InlineData(0, 0, 1, 1, 141.42135623731, 45.0)]
    [InlineData(0, 0, 1, -1, 141.42135623731, 135.0)]
    [InlineData(0, 0, -1, -1, 141.42135623731, 225.0)]
    [InlineData(0, 0, -1, 1, 141.42135623731, 315.0)]
    public void Cardinals(double ox, double oy, double tx, double ty, double er, double eb)
    {
        Assert.True(GeoMath.TryCompute(new MapCoordinate(ox, oy), new MapCoordinate(tx, ty), out double r, out double b, out _));
        Assert.Equal(er, r, 6);
        Assert.Equal(eb, b, 6);
    }

    [Fact]
    public void ReciprocalBearingsDifferBy180()
    {
        var a = new MapCoordinate(100, 100);
        var b = new MapCoordinate(101, 102);
        Assert.True(GeoMath.TryCompute(a, b, out double rab, out double bab, out _));
        Assert.True(GeoMath.TryCompute(b, a, out double rba, out double bba, out _));
        Assert.Equal(rab, rba, 9);
        Assert.Equal(GeoMath.NormalizeBearing(bab + 180.0), bba, 9);
    }

    [Fact]
    public void ZeroRangeHasNoBearing()
    {
        var p = new MapCoordinate(101.5, 107.7);
        Assert.False(GeoMath.TryCompute(p, p, out double r, out double b, out string reason));
        Assert.Equal(0.0, r);
        Assert.True(double.IsNaN(b));
        Assert.Equal("ZERO_RANGE", reason);
    }

    [Theory]
    [InlineData(double.NaN, 0, 0, 0)]
    [InlineData(0, double.PositiveInfinity, 0, 0)]
    [InlineData(0, 0, double.NaN, 0)]
    public void RejectsNonFinite(double ox, double oy, double tx, double ty)
    {
        Assert.False(GeoMath.TryCompute(new MapCoordinate(ox, oy), new MapCoordinate(tx, ty), out _, out _, out string reason));
        Assert.Equal("NON_FINITE_INPUT", reason);
    }

    [Fact]
    public void NormalizesWrapping()
    {
        Assert.Equal(0.0, GeoMath.NormalizeBearing(360.0), 9);
        Assert.Equal(90.0, GeoMath.NormalizeBearing(450.0), 9);
        Assert.Equal(270.0, GeoMath.NormalizeBearing(-90.0), 9);
    }
}
