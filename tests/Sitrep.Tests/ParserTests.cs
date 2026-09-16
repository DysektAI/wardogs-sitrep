using Sitrep.Core;

namespace Sitrep.Tests;

public sealed class ParserTests
{
    [Theory]
    [InlineData("x101.53\ny107.77", 101.53, 107.77)]
    [InlineData("y107.77\nx101.53", 101.53, 107.77)]
    [InlineData("x101.66 y110.10", 101.66, 110.10)]
    [InlineData("  X101.53   Y107.77  ", 101.53, 107.77)]
    [InlineData("x101,53\ny107,77", 101.53, 107.77)]
    [InlineData("PING\nx101.66\ny110.10\nDysekt", 101.66, 110.10)]
    public void AcceptsValidPairs(string text, double ex, double ey)
    {
        Assert.True(CoordinateParser.TryParse(text, out var c, out _));
        Assert.Equal(ex, c.X, 9);
        Assert.Equal(ey, c.Y, 9);
    }

    [Theory]
    [InlineData("x101.53")]
    [InlineData("y107.77")]
    [InlineData("101.53 107.77")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("PING\nDysekt")]
    [InlineData("100 101 102")]
    public void RejectsMissingAxis(string text)
    {
        Assert.False(CoordinateParser.TryParse(text, out _, out string reason));
        Assert.False(string.IsNullOrWhiteSpace(reason));
    }

    [Theory]
    [InlineData("x10166\ny107.77")]
    [InlineData("x101.5\ny107.77")]
    [InlineData("x101.666\ny107.77")]
    [InlineData("x101.\ny107.77")]
    [InlineData("x.77\ny107.77")]
    [InlineData("x1016.6\ny107.77")]
    public void RejectsBadPrecision(string text)
    {
        Assert.False(CoordinateParser.TryParse(text, out _, out _));
    }

    [Fact]
    public void RejectsConflictingPair()
    {
        Assert.False(CoordinateParser.TryParse("x101.53 x101.66\ny107.77", out _, out string r));
        Assert.Equal("CONFLICTING_PAIR", r);
    }

    [Fact]
    public void RejectsDuplicateAxisSameValue()
    {
        Assert.False(CoordinateParser.TryParse("x101.53 x101.53\ny107.77", out _, out string r));
        Assert.Equal("DUPLICATE_AXIS", r);
    }

    [Fact]
    public void RejectsUnsupportedSign()
    {
        Assert.False(CoordinateParser.TryParse("x-101.53\ny107.77", out _, out string r));
        Assert.Equal("UNSUPPORTED_SIGN", r);
    }

    [Fact]
    public void RejectsPartialWhenLettersGlueNumbers()
    {
        Assert.False(CoordinateParser.TryParse("ax101.53y107.77", out _, out _));
    }
}
