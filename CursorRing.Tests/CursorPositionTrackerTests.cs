using System.Numerics;

namespace CursorRing.Tests;

public sealed class CursorPositionTrackerTests
{
    private static readonly Vector2 Minimum = new(100f, 200f);
    private static readonly Vector2 Maximum = new(900f, 800f);

    [Fact]
    public void CapturedPositionStaysAtLastFreePosition()
    {
        var tracker = new CursorPositionTracker();
        var freePosition = new Vector2(350f, 450f);
        Assert.True(tracker.TryResolve(freePosition, Minimum, Maximum, false, out _));

        Assert.True(tracker.TryResolve(new Vector2(500f, 500f), Minimum, Maximum, true, out var first));
        Assert.True(tracker.TryResolve(new Vector2(499f, 501f), Minimum, Maximum, true, out var second));
        Assert.True(tracker.TryResolve(new Vector2(501f, 499f), Minimum, Maximum, true, out var third));

        Assert.Equal(freePosition, first);
        Assert.Equal(first, second);
        Assert.Equal(first, third);
    }

    [Fact]
    public void ReleasedCaptureResumesLiveTracking()
    {
        var tracker = new CursorPositionTracker();
        Assert.True(tracker.TryResolve(new Vector2(300f, 400f), Minimum, Maximum, false, out _));
        Assert.True(tracker.TryResolve(new Vector2(500f, 500f), Minimum, Maximum, true, out _));

        var releasedPosition = new Vector2(600f, 700f);
        Assert.True(tracker.TryResolve(releasedPosition, Minimum, Maximum, false, out var resolved));

        Assert.Equal(releasedPosition, resolved);
    }

    [Fact]
    public void CapturedPositionFallsBackToViewportCenter()
    {
        var tracker = new CursorPositionTracker();

        Assert.True(tracker.TryResolve(new Vector2(300f, 400f), Minimum, Maximum, true, out var position));

        Assert.Equal(new Vector2(500f, 500f), position);
    }

    [Fact]
    public void CapturedPositionRecoversAfterViewportChange()
    {
        var tracker = new CursorPositionTracker();
        Assert.True(tracker.TryResolve(new Vector2(850f, 750f), Minimum, Maximum, false, out _));
        Assert.True(tracker.TryResolve(new Vector2(500f, 500f), Minimum, Maximum, true, out _));
        var resizedMaximum = new Vector2(700f, 600f);

        Assert.True(tracker.TryResolve(new Vector2(500f, 500f), Minimum, resizedMaximum, true, out var position));

        Assert.Equal(new Vector2(400f, 400f), position);
    }

    [Fact]
    public void FreePositionMustBeInsideHalfOpenViewport()
    {
        var tracker = new CursorPositionTracker();

        Assert.False(tracker.TryResolve(Maximum, Minimum, Maximum, false, out _));
        Assert.False(tracker.TryResolve(new Vector2(float.PositiveInfinity, 400f), Minimum, Maximum, false, out _));
    }
}
