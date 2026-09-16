using Sitrep.Core;

namespace Sitrep.Tests;

public sealed class ForegroundSessionTests
{
    private static OcrCompletion Ok(CaptureRequest req, MapCoordinate point) =>
        new(req.Sequence, req.Generation, req.Role, req.OriginRevision, true, point, "", "");

    [Fact]
    public void PollWithoutAnyHotkeyInvalidatesSolutionAndPreservesOriginOnAltTab()
    {
        var state = new AssistantState(null, true);
        var foreground = new ForegroundSession(state);
        Assert.True(foreground.Observe(10, true, _ => true));
        var origin = state.BeginOrigin(10, 0, 0, 0, 0, 10, 10);
        state.Complete(Ok(origin, new MapCoordinate(100, 100)));
        var target = state.BeginTarget(10, 0, 0, 0, 0, 10, 10).Request!;
        state.Complete(Ok(target, new MapCoordinate(101, 102)));
        Assert.NotNull(state.RangeMeters);
        Assert.True(foreground.Observe(20, false, _ => true));
        Assert.False(foreground.IsForeground);
        Assert.Null(state.RangeMeters);
        Assert.Null(state.ActiveTarget);
        Assert.NotNull(state.ConfirmedOrigin);
        Assert.Equal(DisplayStatuses.WindowLost, state.Status);
        Assert.True(foreground.Observe(10, true, _ => true));
        Assert.Null(state.RangeMeters);
    }

    [Fact]
    public void SwitchingToAnotherMatchingWindowCannotInheritOrigin()
    {
        var state = new AssistantState(null, true);
        var foreground = new ForegroundSession(state);
        foreground.Observe(10, true, _ => true);
        var origin = state.BeginOrigin(10, 0, 0, 0, 0, 10, 10);
        state.Complete(Ok(origin, new MapCoordinate(100, 100)));
        foreground.Observe(20, true, _ => true);
        Assert.Equal(10, foreground.AttachedWindow);
        Assert.False(foreground.IsForeground);
        foreground.Observe(20, true, h => h != 10);
        Assert.Equal(20, foreground.AttachedWindow);
        Assert.True(foreground.IsForeground);
        Assert.Null(state.ConfirmedOrigin);
    }

    [Fact]
    public void ClosureWhileDisabledStillClearsPositionsAndDelayedResult()
    {
        var state = new AssistantState(null, true);
        var foreground = new ForegroundSession(state);
        foreground.Observe(10, true, _ => true);
        var origin = state.BeginOrigin(10, 0, 0, 0, 0, 10, 10);
        state.Complete(Ok(origin, new MapCoordinate(100, 100)));
        var pending = state.BeginTarget(10, 0, 0, 0, 0, 10, 10).Request!;
        state.SetLiveEnabled(false);
        foreground.Observe(20, false, _ => false);
        Assert.Null(state.ConfirmedOrigin);
        Assert.False(state.Complete(Ok(pending, new MapCoordinate(101, 102))));
        Assert.Equal(DisplayStatuses.SetMortar, state.Status);
    }
}
