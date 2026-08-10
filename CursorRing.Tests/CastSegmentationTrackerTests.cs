namespace CursorRing.Tests;

public sealed class CastSegmentationTrackerTests
{
    [Fact]
    public void PredictionUsesLiveCastOffsetAndDuration()
    {
        var tracker = new CastSegmentationTracker();
        var gcd = new GcdState(true, 0.2f, 2.5f);
        var cast = Cast(true, 10, 0.15f, 1.5f);

        var segments = tracker.Update(gcd, cast, 500f);

        Assert.True(segments.IsActive);
        Assert.Equal(0.42f, segments.SlideStart, 5);
        Assert.Equal(0.62f, segments.CastEnd, 5);
        Assert.False(segments.IsConfirmed);
    }

    [Fact]
    public void MatchingResponseReplacesPredictedBoundary()
    {
        var tracker = new CastSegmentationTracker();
        var pending = tracker.Update(
            new GcdState(true, 0.2f, 2.5f),
            Cast(true, 10, 0.2f, 1.5f),
            500f);
        var confirmed = tracker.Update(
            new GcdState(true, 1.1f, 2.5f),
            Cast(true, 10, 1.1f, 1.5f, 10),
            500f);

        Assert.Equal(0.4f, pending.SlideStart, 5);
        Assert.True(confirmed.IsConfirmed);
        Assert.Equal(0.44f, confirmed.SlideStart, 5);
        Assert.Equal(0.6f, confirmed.CastEnd, 5);
    }

    [Fact]
    public void MatchingResponseProducesOneUnifiedBoundary()
    {
        var first = new CastSegmentationTracker();
        var second = new CastSegmentationTracker();
        var startGcd = new GcdState(true, 0.2f, 2.5f);
        var startCast = Cast(true, 10, 0.2f, 1.5f);

        var firstBefore = first.Update(startGcd, startCast, 500f);
        second.Update(startGcd, startCast, 500f);
        var responseGcd = new GcdState(true, 1.1f, 2.5f);
        var responseCast = Cast(true, 10, 1.1f, 1.5f, 10);
        var firstAfter = first.Update(responseGcd, responseCast, 500f);
        var secondAfter = second.Update(responseGcd, responseCast, 500f);

        Assert.Equal(0.4f, firstBefore.SlideStart, 5);
        Assert.Equal(0.44f, firstAfter.SlideStart, 5);
        Assert.Equal(firstAfter, secondAfter);
        Assert.True(secondAfter.IsConfirmed);
    }

    [Fact]
    public void LiveTotalUpdatesBoundariesButKeepsLatchedStart()
    {
        var tracker = new CastSegmentationTracker();
        var initial = tracker.Update(
            new GcdState(true, 0.2f, 2.5f),
            Cast(true, 10, 0.2f, 1.5f),
            500f);

        var later = tracker.Update(
            new GcdState(true, 0.9f, 2.5f),
            Cast(true, 10, 0.85f, 1.6f),
            500f);

        Assert.Equal(0.44f, later.SlideStart, 5);
        Assert.Equal(0.64f, later.CastEnd, 5);
        Assert.Equal(initial.Total, later.Total);
    }

    [Fact]
    public void ConfirmedCastPersistsUntilGcdEnds()
    {
        var tracker = new CastSegmentationTracker();
        tracker.Update(
            new GcdState(true, 1f, 2.5f),
            Cast(true, 10, 1f, 1.5f, 10),
            500f);

        var retained = tracker.Update(new GcdState(true, 1.6f, 2.5f), CastSample.Inactive, 500f);
        var cleared = tracker.Update(GcdState.Inactive, CastSample.Inactive, 500f);

        Assert.True(retained.IsActive);
        Assert.Equal(CastTimeline.Inactive, cleared);
    }

    [Fact]
    public void InterruptedCastClearsImmediately()
    {
        var tracker = new CastSegmentationTracker();
        tracker.Update(
            new GcdState(true, 0.2f, 2.5f),
            Cast(true, 10, 0.2f, 1.5f),
            500f);

        var segments = tracker.Update(new GcdState(true, 0.5f, 2.5f), CastSample.Inactive, 500f);

        Assert.Equal(CastTimeline.Inactive, segments);
    }

    [Fact]
    public void StaleResponseDoesNotConfirmCast()
    {
        var tracker = new CastSegmentationTracker();

        var segments = tracker.Update(
            new GcdState(true, 0.8f, 2.5f),
            Cast(true, 20, 0.8f, 1.5f, 19),
            500f);

        Assert.False(segments.IsConfirmed);
        Assert.Equal(0.4f, segments.SlideStart, 5);
    }

    [Fact]
    public void InvalidMatchingResponseDoesNotConfirmCast()
    {
        var tracker = new CastSegmentationTracker();
        tracker.Update(
            new GcdState(true, 0.1f, 2.5f),
            Cast(true, 20, 0.1f, 1.5f),
            500f);

        var segments = tracker.Update(
            new GcdState(true, 0.8f, 2.5f),
            Cast(false, 20, float.NaN, 1.5f, 20),
            500f);

        Assert.Equal(CastTimeline.Inactive, segments);
    }

    [Fact]
    public void NewGcdCycleReplacesCommittedTimeline()
    {
        var tracker = new CastSegmentationTracker();
        tracker.Update(
            new GcdState(true, 1f, 2.5f),
            Cast(true, 10, 1f, 1.5f, 10),
            500f);

        var replacement = tracker.Update(
            new GcdState(true, 0.1f, 2.5f),
            Cast(true, 11, 0.1f, 2f),
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
        tracker.Update(gcd, Cast(true, 10, 1f, 1.5f, 10), 500f);

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
            500f);

        Assert.Equal(CastTimeline.Inactive, segments);
    }

    [Fact]
    public void KnownOffGlobalCooldownCastIsRejectedAtCycleStart()
    {
        var tracker = new CastSegmentationTracker();
        var cast = new CastSample(true, 10, 0.1f, 1f, 0, 10, -1);

        var segments = tracker.Update(new GcdState(true, 0.1f, 2.5f), cast, 500f);

        Assert.Equal(CastTimeline.Inactive, segments);
    }

    [Fact]
    public void UnknownRecastGroupsFailClosed()
    {
        var tracker = new CastSegmentationTracker();
        var cast = new CastSample(true, 10, 0.1f, 1f, 0);

        var segments = tracker.Update(new GcdState(true, 0.1f, 2.5f), cast, 500f);

        Assert.Equal(CastTimeline.Inactive, segments);
    }

    [Theory]
    [InlineData(57, -1)]
    [InlineData(10, 57)]
    public void EitherGlobalCooldownGroupIsAccepted(int group, int additionalGroup)
    {
        var tracker = new CastSegmentationTracker();
        var cast = new CastSample(true, 10, 0.1f, 1f, 0, group, additionalGroup);

        var segments = tracker.Update(new GcdState(true, 0.1f, 2.5f), cast, 500f);

        Assert.True(segments.IsActive);
    }

    [Fact]
    public void LongCastScalesTheCompleteTimeline()
    {
        var tracker = new CastSegmentationTracker();

        var segments = tracker.Update(
            new GcdState(true, 0.1f, 2.5f),
            Cast(true, 10, 0.1f, 4f),
            5000f);

        Assert.Equal(1f, segments.CastEnd);
        Assert.Equal(4f, segments.Total, 5);
        Assert.Equal(0.75f, segments.SlideStart, 5);
    }

    [Fact]
    public void EightSecondCastContinuesAfterGcdCompletes()
    {
        var tracker = new CastSegmentationTracker();
        var initial = tracker.Update(
            new GcdState(true, 0.1f, 2.5f),
            Cast(true, 10, 0.1f, 8f),
            500f);
        var continued = tracker.Update(
            GcdState.Inactive,
            Cast(true, 10, 3f, 8f),
            500f);

        Assert.Equal(8f, initial.Total, 5);
        Assert.Equal(0.9375f, initial.SlideStart, 5);
        Assert.Equal(1f, initial.CastEnd, 5);
        Assert.True(continued.IsActive);
        Assert.Equal(3f, continued.Elapsed, 5);
        Assert.Equal(0.375f, continued.Progress, 5);
    }

    [Fact]
    public void ConfirmedLongCastBoundaryRenormalizesAfterGcdCompletes()
    {
        var tracker = new CastSegmentationTracker();
        tracker.Update(
            new GcdState(true, 0.1f, 2.5f),
            Cast(true, 10, 0.1f, 8f),
            500f);

        var confirmed = tracker.Update(
            GcdState.Inactive,
            Cast(true, 10, 7.4f, 8f, 10),
            500f);
        var resized = tracker.Update(
            GcdState.Inactive,
            Cast(true, 10, 7.4f, 10f, 10),
            500f);

        Assert.True(confirmed.IsConfirmed);
        Assert.Equal(0.925f, confirmed.SlideStart, 5);
        Assert.Equal(0.74f, resized.SlideStart, 5);
    }

    [Fact]
    public void LongCastDisappearsWhenCastingCompletes()
    {
        var tracker = new CastSegmentationTracker();
        tracker.Update(
            new GcdState(true, 0.1f, 2.5f),
            Cast(true, 10, 0.1f, 8f, 10),
            500f);

        var completed = tracker.Update(GcdState.Inactive, Cast(false, 10, 8f, 8f, 10), 500f);

        Assert.Equal(CastTimeline.Inactive, completed);
    }

    [Fact]
    public void ActiveLongCastTrackingDoesNotAllocate()
    {
        var tracker = new CastSegmentationTracker();
        tracker.Update(
            new GcdState(true, 0.1f, 2.5f),
            Cast(true, 10, 0.1f, 8f),
            500f);
        for (var index = 0; index < 100; index++)
        {
            tracker.Update(GcdState.Inactive, Cast(true, 10, 3f, 8f), 500f);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            tracker.Update(GcdState.Inactive, Cast(true, 10, 3f, 8f), 500f);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    [Theory]
    [InlineData(8f, 10f, 10f, 0.95f)]
    [InlineData(8f, 6f, 6f, 0.9166667f)]
    public void LiveLongCastTotalRescalesTimeline(float initialTotal, float updatedTotal, float expectedTimeline, float expectedSlide)
    {
        var tracker = new CastSegmentationTracker();
        tracker.Update(
            new GcdState(true, 0.1f, 2.5f),
            Cast(true, 10, 0.1f, initialTotal),
            500f);

        var updated = tracker.Update(
            GcdState.Inactive,
            Cast(true, 10, 3f, updatedTotal),
            500f);

        Assert.Equal(expectedTimeline, updated.Total, 5);
        Assert.Equal(expectedSlide, updated.SlideStart, 5);
    }

    [Theory]
    [InlineData(float.NaN, 1f)]
    [InlineData(0f, 0f)]
    [InlineData(-1f, 1f)]
    public void InvalidCastTimesAreRejected(float elapsed, float total)
    {
        var tracker = new CastSegmentationTracker();
        var cast = Cast(true, 10, elapsed, total);

        var segments = tracker.Update(new GcdState(true, 0.1f, 2.5f), cast, 500f);

        Assert.Equal(CastTimeline.Inactive, segments);
    }

    private static CastSample Cast(bool isCasting, uint sequence, float elapsed, float total, uint responseSequence = 0)
    {
        return new CastSample(isCasting, sequence, elapsed, total, responseSequence, 57);
    }
}
