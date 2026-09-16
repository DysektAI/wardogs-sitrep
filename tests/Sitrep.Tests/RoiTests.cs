using Sitrep.Core;

namespace Sitrep.Tests;

public sealed class RoiTests
{
    [Fact]
    public void BuildsCursorRelativeDefaultSize()
    {
        var roi = RoiBuilder.BuildCursorRelative(1000, 700);
        Assert.Equal(360, roi.Width);
        Assert.Equal(200, roi.Height);
        Assert.Equal(960, roi.X);
        Assert.Equal(524, roi.Y);
    }

    [Fact]
    public void IntersectsWithBounds()
    {
        var roi = new CaptureRegion(900, 640, 300, 180);
        var bounds = new CaptureRegion(0, 0, 2048, 1152);
        var hit = RoiBuilder.Intersect(roi, bounds);
        Assert.NotNull(hit);
        Assert.Equal(roi, hit!.Value);
    }

    [Fact]
    public void ClippedReturnsNullWhenOutside()
    {
        var roi = new CaptureRegion(5000, 5000, 100, 100);
        var bounds = new CaptureRegion(0, 0, 2048, 1152);
        Assert.Null(RoiBuilder.Intersect(roi, bounds));
    }

    [Fact]
    public void DetectsOverlayOverlap()
    {
        var roi = new CaptureRegion(100, 100, 200, 200);
        var overlay = new CaptureRegion(150, 150, 300, 110);
        Assert.True(RoiBuilder.Overlaps(roi, overlay));
        Assert.False(RoiBuilder.Overlaps(roi, new CaptureRegion(500, 500, 10, 10)));
    }
}
