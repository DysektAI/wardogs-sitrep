using Sitrep.Core;

namespace Sitrep.Tests;

public sealed class OverlayPlacementTests
{
    [Theory]
    [InlineData(-1920, -3840, 7680, 420, -1920)] // Monitor left of the primary is kept.
    [InlineData(-5000, -3840, 7680, 420, -3840)]
    [InlineData(1e6, -3840, 7680, 420, 3420)] // Right edge stays on screen.
    [InlineData(1700, 0, 1920, 420, 1500)] // Wider overlay than when the position was saved.
    [InlineData(50, 0, 300, 420, 0)] // Cannot fit: pinned to origin.
    public void ClampsToVirtualScreen(double value, double origin, double extent, double size, double expected) =>
        Assert.Equal(expected, OverlayPlacement.Clamp(value, origin, extent, size));
}
