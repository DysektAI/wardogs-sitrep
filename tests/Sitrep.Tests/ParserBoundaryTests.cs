using Sitrep.Core;

namespace Sitrep.Tests;

public sealed class ParserBoundaryTests
{
    [Theory]
    [InlineData("_x101.53 y107.77", "MISSING_X")]
    [InlineData("éx101.53 y107.77", "MISSING_X")]
    [InlineData("x101.53_ y107.77", "PARTIAL_TOKEN")]
    [InlineData("x101.53\u0301 y107.77", "PARTIAL_TOKEN")]
    [InlineData("\u0301x101.53 y107.77", "MISSING_X")]
    [InlineData("x101.53\u203F y107.77", "PARTIAL_TOKEN")]
    [InlineData("x101.53 y107.77\u0903", "PARTIAL_TOKEN")]
    [InlineData("x101.53 y107.77\u20DD", "PARTIAL_TOKEN")]
    [InlineData("\U00010400x101.53 y107.77", "MISSING_X")]
    [InlineData("x101.53\U00010400 y107.77", "PARTIAL_TOKEN")]
    [InlineData("xBAD1 x101.53 y107.77", "PARTIAL_TOKEN")]
    [InlineData("x101.53 yBAD1 y107.77", "PARTIAL_TOKEN")]
    [InlineData("xBAD y107.77", "MISSING_X")]
    [InlineData("x101.53 yBAD", "MISSING_Y")]
    [InlineData("x.77 x101.53 y107.77", "BAD_X_PRECISION")]
    [InlineData("x+ 101.53 x101.53 y107.77", "PARTIAL_TOKEN")]
    [InlineData("x + 101.53 x101.53 y107.77", "PARTIAL_TOKEN")]
    [InlineData("x .77 x101.53 y107.77", "BAD_X_PRECISION")]
    [InlineData("x −101.53 x101.53 y107.77", "PARTIAL_TOKEN")]
    [InlineData("x101.53-5 y107.77", "PARTIAL_TOKEN")]
    [InlineData("x101.53+5 y107.77", "PARTIAL_TOKEN")]
    [InlineData("x101.53−5 y107.77", "PARTIAL_TOKEN")]
    [InlineData("-x101.53 y107.77", "MISSING_X")]
    [InlineData("+x101.53 y107.77", "MISSING_X")]
    [InlineData("x101.53² y107.77", "PARTIAL_TOKEN")]
    [InlineData("x101.53 y107.77z", "PARTIAL_TOKEN")]
    [InlineData("x101.53y107.77", "PARTIAL_TOKEN")]
    [InlineData("x101.53.5 y107.77", "BAD_X_PRECISION")]
    [InlineData("x101.53,5 y107.77", "BAD_X_PRECISION")]
    [InlineData("x+101.53 y107.77", "UNSUPPORTED_SIGN")]
    [InlineData("x-101.53 y107.77", "UNSUPPORTED_SIGN")]
    [InlineData("x101.53\u2012 y107.77", "PARTIAL_TOKEN")]
    [InlineData("x\u2012101.53 y107.77", "PARTIAL_TOKEN")]
    [InlineData("x\u0661\u0660\u0661.53 y107.77", "BAD_X_PRECISION")]
    [InlineData("x101.53\u0661 y107.77", "BAD_X_PRECISION")]
    [InlineData("x101.53 y107.77\u0662", "BAD_Y_PRECISION")]
    public void RejectsEmbeddedMalformedOrSignedTokens(string text, string expectedReason)
    {
        Assert.False(CoordinateParser.TryParse(text, out var coordinate, out var reason));
        Assert.Equal(default, coordinate);
        Assert.Equal(expectedReason, reason);
    }

    [Theory]
    [InlineData("(x101.53) [y107.77]", 101.53, 107.77)]
    [InlineData("X \t 1.53\r\nY 7.77", 1.53, 7.77)]
    [InlineData("y 107,77\n x 101,53", 101.53, 107.77)]
    [InlineData("PING: x101.53; y107.77!", 101.53, 107.77)]
    [InlineData("100 101 102\nx101.53 y107.77", 101.53, 107.77)]
    [InlineData("y110.10\n\nx101.66\n\n2\n\ny\n", 101.66, 110.10)]
    [InlineData("y\n\nx101.53 y107.77", 101.53, 107.77)]
    // Alphabetic words are not extra coordinate labels. Real unfiltered MapPing OCR includes "YZ".
    [InlineData("y110.10\n\niz\n\n=\n\nx101.66\n\nL\n\nYZ\n\nPING\n\nDysekt\n", 101.66, 110.10)]
    [InlineData("xBAD x101.53 y107.77", 101.53, 107.77)]
    [InlineData("x101.53 yBAD y107.77", 101.53, 107.77)]
    [InlineData("Yellow marker: x101.53 y107.77", 101.53, 107.77)]
    public void PreservesIsolatedPrecisionOrderWhitespaceAndComma(string text, double x, double y)
    {
        Assert.True(CoordinateParser.TryParse(text, out var coordinate, out _));
        Assert.Equal(new MapCoordinate(x, y), coordinate);
    }

    [Theory]
    [InlineData(CaptureRole.Origin)]
    [InlineData(CaptureRole.Target)]
    public void InvalidRecipesCannotBecomeAUsableStateCompletion(CaptureRole role)
    {
        var state = new AssistantState(null);
        var origin = state.BeginOrigin(1, 0, 0, 0, 0, 40, 40);
        Assert.True(state.Complete(new OcrCompletion(origin.Sequence, origin.Generation, origin.Role,
            origin.OriginRevision, true, new MapCoordinate(100, 100), "", "")));
        var request = role == CaptureRole.Origin
            ? state.BeginOrigin(1, 0, 0, 0, 0, 40, 40)
            : state.BeginTarget(1, 0, 0, 0, 0, 40, 40).Request!;
        bool success = RecognitionConsensus.TryAccept(["_x101.53 y107.77", "x101.53_ y107.77"], out var coordinate, out var reason);
        Assert.False(success);
        Assert.True(state.Complete(new OcrCompletion(request.Sequence, request.Generation, role,
            request.OriginRevision, success, coordinate, "invalid boundary recipes", reason)));
        Assert.Equal(role == CaptureRole.Target, state.ConfirmedOrigin.HasValue);
        Assert.Null(state.ActiveTarget);
        Assert.Null(state.RangeMeters);
        Assert.Null(state.ElevationMil);
        Assert.Null(state.Pending);
        Assert.StartsWith(role == CaptureRole.Origin ? "ORIGIN OCR FAILED" : "TARGET OCR FAILED", state.Status);
    }
}
