using Sitrep.Core;

namespace Sitrep.Tests;

public sealed class FiringTableTests
{
    private static FiringTable LoadL81()
    {
        var (table, error) = FiringTable.LoadApollyonL81(Path.Combine(AppContext.BaseDirectory, "data", "l81-apollyon.json"));
        Assert.Null(error);
        Assert.NotNull(table);
        return table;
    }

    [Fact]
    public void L81LimitsAndCoverage()
    {
        var t = LoadL81();
        Assert.Equal("L81", t.WeaponId);
        Assert.Equal(132.0, t.MinRangeMeters);
        Assert.Equal(684.0, t.MaxRangeMeters);
        Assert.True(t.Samples.Count >= 50);
    }

    [Theory]
    [InlineData(132.0, 850.0)]
    [InlineData(229.0, 760.0)]
    [InlineData(239.0, 750.0)]
    [InlineData(300.0, 690.0)]
    [InlineData(684.0, 150.0)]
    public void ExactSamples(double range, double expectedMil)
    {
        var t = LoadL81();
        Assert.True(t.TryInterpolate(range, out double mil, out _));
        Assert.Equal(expectedMil, mil, 9);
    }

    [Fact]
    public void FixtureInterpolation()
    {
        var t = LoadL81();
        Assert.True(t.TryInterpolate(233.36237914454, out double mil, out _));
        Assert.Equal(755.63762085546, mil, 6);
    }

    [Theory]
    [InlineData(400.0, 583.33333333333)]
    [InlineData(500.0, 461.42857142857)]
    public void InteriorPoints(double range, double expectedMil)
    {
        var t = LoadL81();
        Assert.True(t.TryInterpolate(range, out double mil, out _));
        Assert.Equal(expectedMil, mil, 5);
    }

    [Theory]
    [InlineData(131.9)]
    [InlineData(684.1)]
    [InlineData(50.0)]
    [InlineData(800.0)]
    public void RejectsOutsideWeaponLimits(double range)
    {
        var t = LoadL81();
        Assert.False(t.TryInterpolate(range, out _, out string reason));
        Assert.Equal("OUT_OF_RANGE", reason);
    }

    [Fact]
    public void RejectsNonFiniteRange()
    {
        var t = LoadL81();
        Assert.False(t.TryInterpolate(double.NaN, out _, out string r));
        Assert.Equal("NON_FINITE_RANGE", r);
    }

    [Fact]
    public void RejectsDuplicateRanges()
    {
        var (_, err) = FiringTable.TryCreate("L81", 100, 500,
            [new FiringSample(100, 800), new FiringSample(100, 790)]);
        Assert.Equal("DUPLICATE_OR_UNORDERED_RANGE", err);
    }

    [Fact]
    public void RejectsUnorderedRanges()
    {
        // BUILD_SPEC requires strictly increasing input; sorting used to hide corrupt source data.
        var (table, err) = FiringTable.TryCreate("L81", 100, 500,
            [new FiringSample(200, 700), new FiringSample(150, 750)]);
        Assert.Null(table);
        Assert.Equal("DUPLICATE_OR_UNORDERED_RANGE", err);
    }

    [Fact]
    public void RejectsInvalidLimits()
    {
        var (_, err) = FiringTable.TryCreate("L81", 500, 100,
            [new FiringSample(100, 800), new FiringSample(200, 700)]);
        Assert.Equal("INVALID_LIMITS", err);
    }

    [Fact]
    public void RejectsTooFewSamples()
    {
        var (_, err) = FiringTable.TryCreate("L81", 100, 500, [new FiringSample(100, 800)]);
        Assert.Equal("TOO_FEW_SAMPLES", err);
    }

    [Fact]
    public void RejectsCorruptFile()
    {
        var (t, err) = FiringTable.LoadApollyonL81(Path.Combine(Path.GetTempPath(), "no-such-l81.json"));
        Assert.Null(t);
        Assert.Equal("CORRUPT_DATA", err);
    }
}
