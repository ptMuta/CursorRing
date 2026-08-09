namespace CursorRing.Tests;

public sealed class CastSegmentationTrackerTests
{
    [Fact]
    public void PredictionUsesLiveCastOffsetAndDuration()
    {
        var tracker = new CastSegmentationTracker();
        var gcd = new GcdState(true, 0.2f, 2.5f);
        var cast = Cast(true, 10, 0.15f, 1.5f);

        var segments = tracker.Update(gcd, cast, SlidecastTimingMode.Predicted, 500f);

        Assert.True(segments.IsActive);
        Assert.Equal(0.42f, segments.SlideStart, 5);
        Assert.Equal(0.62f, segments.CastEnd, 5);
        Assert.False(segments.IsConfirmed);
    }

    [Fact]
    public void ConfirmedModeRevealsExactObservedBoundary()
    {
        var tracker = new CastSegmentationTracker();
        var pending = tracker.Update(
            new GcdState(true, 0.2f, 2.5f),
            Cast(true, 10, 0.2f, 1.5f),
            SlidecastTimingMode.Confirmed,
            500f);
        var confirmed = tracker.Update(
            new GcdState(true, 1.1f, 2.5f),
            Cast(true, 10, 1.1f, 1.5f, 10),
            SlidecastTimingMode.Confirmed,
            500f);

        Assert.Equal(pending.CastEnd, pending.SlideStart);
        Assert.True(confirmed.IsConfirmed);
        Assert.Equal(0.44f, confirmed.SlideStart, 5);
        Assert.Equal(0.6f, confirmed.CastEnd, 5);
    }

    [Fact]
    public void HybridSnapsButPredictionRemainsStable()
    {
        var hybrid = new CastSegmentationTracker();
        var predicted = new CastSegmentationTracker();
        var startGcd = new GcdState(true, 0.2f, 2.5f);
        var startCast = Cast(true, 10, 0.2f, 1.5f);

        var hybridBefore = hybrid.Update(startGcd, startCast, SlidecastTimingMode.Hybrid, 500f);
        predicted.Update(startGcd, startCast, SlidecastTimingMode.Predicted, 500f);
        var responseGcd = new GcdState(true, 1.1f, 2.5f);
        var responseCast = Cast(true, 10, 1.1f, 1.5f, 10);
        var hybridAfter = hybrid.Update(responseGcd, responseCast, SlidecastTimingMode.Hybrid, 500f);
        var predictedAfter = predicted.Update(responseGcd, responseCast, SlidecastTimingMode.Predicted, 500f);

        Assert.Equal(0.4f, hybridBefore.SlideStart, 5);
        Assert.Equal(0.44f, hybridAfter.SlideStart, 5);
        Assert.Equal(0.4f, predictedAfter.SlideStart, 5);
        Assert.True(predictedAfter.IsConfirmed);
    }

    [Fact]
    public void LiveTimerSkewDoesNotMoveLatchedBoundaries()
    {
        var tracker = new CastSegmentationTracker();
        var initial = tracker.Update(
            new GcdState(true, 0.2f, 2.5f),
            Cast(true, 10, 0.2f, 1.5f),
            SlidecastTimingMode.Predicted,
            500f);

        var later = tracker.Update(
            new GcdState(true, 0.9f, 2.5f),
            Cast(true, 10, 0.85f, 1.6f),
            SlidecastTimingMode.Predicted,
            500f);

        Assert.Equal(initial.SlideStart, later.SlideStart);
        Assert.Equal(initial.CastEnd, later.CastEnd);
    }

    [Fact]
    public void ConfirmedCastPersistsUntilGcdEnds()
    {
        var tracker = new CastSegmentationTracker();
        tracker.Update(
            new GcdState(true, 1f, 2.5f),
            Cast(true, 10, 1f, 1.5f, 10),
            SlidecastTimingMode.Hybrid,
            500f);

        var retained = tracker.Update(new GcdState(true, 1.6f, 2.5f), CastSample.Inactive, SlidecastTimingMode.Hybrid, 500f);
        var cleared = tracker.Update(GcdState.Inactive, CastSample.Inactive, SlidecastTimingMode.Hybrid, 500f);

        Assert.True(retained.IsActive);
        Assert.Equal(GcdSegments.Inactive, cleared);
    }

    [Fact]
    public void InterruptedCastClearsImmediately()
    {
        var tracker = new CastSegmentationTracker();
        tracker.Update(
            new GcdState(true, 0.2f, 2.5f),
            Cast(true, 10, 0.2f, 1.5f),
            SlidecastTimingMode.Hybrid,
            500f);

        var segments = tracker.Update(new GcdState(true, 0.5f, 2.5f), CastSample.Inactive, SlidecastTimingMode.Hybrid, 500f);

        Assert.Equal(GcdSegments.Inactive, segments);
    }

    [Fact]
    public void StaleResponseDoesNotConfirmCast()
    {
        var tracker = new CastSegmentationTracker();

        var segments = tracker.Update(
            new GcdState(true, 0.8f, 2.5f),
            Cast(true, 20, 0.8f, 1.5f, 19),
            SlidecastTimingMode.Confirmed,
            500f);

        Assert.False(segments.IsConfirmed);
        Assert.Equal(segments.CastEnd, segments.SlideStart);
    }

    [Fact]
    public void NewGcdCycleReplacesCommittedTimeline()
    {
        var tracker = new CastSegmentationTracker();
        tracker.Update(
            new GcdState(true, 1f, 2.5f),
            Cast(true, 10, 1f, 1.5f, 10),
            SlidecastTimingMode.Hybrid,
            500f);

        var replacement = tracker.Update(
            new GcdState(true, 0.1f, 2.5f),
            Cast(true, 11, 0.1f, 2f),
            SlidecastTimingMode.Predicted,
            500f);

        Assert.True(replacement.IsActive);
        Assert.False(replacement.IsConfirmed);
        Assert.Equal(0.6f, replacement.SlideStart, 5);
        Assert.Equal(0.8f, replacement.CastEnd, 5);
    }

    [Fact]
    public void NewCycleRequestsRecastGroupResolutionBeforeUpdate()
    {
        var tracker = new CastSegmentationTracker();
        var gcd = new GcdState(true, 1f, 2.5f);
        tracker.Update(gcd, Cast(true, 10, 1f, 1.5f, 10), SlidecastTimingMode.Hybrid, 500f);

        Assert.False(tracker.NeedsCast(gcd));
        Assert.True(tracker.NeedsCast(new GcdState(true, 0.1f, 2.5f)));
    }

    [Fact]
    public void CastStartingLateInExistingGcdIsRejected()
    {
        var tracker = new CastSegmentationTracker();

        var segments = tracker.Update(
            new GcdState(true, 1.5f, 2.5f),
            Cast(true, 10, 0.1f, 1f),
            SlidecastTimingMode.Hybrid,
            500f);

        Assert.Equal(GcdSegments.Inactive, segments);
    }

    [Fact]
    public void KnownOffGlobalCooldownCastIsRejectedAtCycleStart()
    {
        var tracker = new CastSegmentationTracker();
        var cast = new CastSample(true, 10, 0.1f, 1f, 0, 10, -1);

        var segments = tracker.Update(new GcdState(true, 0.1f, 2.5f), cast, SlidecastTimingMode.Hybrid, 500f);

        Assert.Equal(GcdSegments.Inactive, segments);
    }

    [Fact]
    public void UnknownRecastGroupsFailClosed()
    {
        var tracker = new CastSegmentationTracker();
        var cast = new CastSample(true, 10, 0.1f, 1f, 0);

        var segments = tracker.Update(new GcdState(true, 0.1f, 2.5f), cast, SlidecastTimingMode.Hybrid, 500f);

        Assert.Equal(GcdSegments.Inactive, segments);
    }

    [Theory]
    [InlineData(57, -1)]
    [InlineData(10, 57)]
    public void EitherGlobalCooldownGroupIsAccepted(int group, int additionalGroup)
    {
        var tracker = new CastSegmentationTracker();
        var cast = new CastSample(true, 10, 0.1f, 1f, 0, group, additionalGroup);

        var segments = tracker.Update(new GcdState(true, 0.1f, 2.5f), cast, SlidecastTimingMode.Hybrid, 500f);

        Assert.True(segments.IsActive);
    }

    [Fact]
    public void LongCastAndOversizedPredictionAreClamped()
    {
        var tracker = new CastSegmentationTracker();

        var segments = tracker.Update(
            new GcdState(true, 0.1f, 2.5f),
            Cast(true, 10, 0.1f, 4f),
            SlidecastTimingMode.Predicted,
            5000f);

        Assert.Equal(1f, segments.CastEnd);
        Assert.Equal(1f, segments.SlideStart);
    }

    [Theory]
    [InlineData(float.NaN, 1f)]
    [InlineData(0f, 0f)]
    [InlineData(-1f, 1f)]
    public void InvalidCastTimesAreRejected(float elapsed, float total)
    {
        var tracker = new CastSegmentationTracker();
        var cast = Cast(true, 10, elapsed, total);

        var segments = tracker.Update(new GcdState(true, 0.1f, 2.5f), cast, SlidecastTimingMode.Hybrid, 500f);

        Assert.Equal(GcdSegments.Inactive, segments);
    }

    private static CastSample Cast(bool isCasting, uint sequence, float elapsed, float total, uint responseSequence = 0)
    {
        return new CastSample(isCasting, sequence, elapsed, total, responseSequence, 57);
    }
}
