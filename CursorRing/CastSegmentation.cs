using System;

namespace CursorRing;

internal readonly record struct CastSample(
    bool IsCasting,
    uint SourceSequence,
    float Elapsed,
    float Total,
    uint ResponseSourceSequence,
    int RecastGroup = -1,
    int AdditionalRecastGroup = -1)
{
    internal static CastSample Inactive => default;
}

internal readonly record struct GcdSegments(bool IsActive, float SlideStart, float CastEnd, bool IsConfirmed)
{
    internal static GcdSegments Inactive => default;
}

internal sealed class CastSegmentationTracker
{
    private const float CycleTolerance = 0.05f;
    private const float StartTolerance = 0.25f;
    private bool wasGcdActive;
    private bool attached;
    private bool confirmed;
    private uint sourceSequence;
    private float previousElapsed;
    private float previousTotal;
    private float predictedSlideStart;
    private float observedSlideStart;
    private float castEnd;

    internal bool NeedsCast(GcdState gcd)
    {
        return !attached || IsNewCycle(gcd);
    }

    internal GcdSegments Update(GcdState gcd, CastSample cast, SlidecastTimingMode mode, float predictionMilliseconds)
    {
        if (!gcd.IsActive)
        {
            Reset();
            return GcdSegments.Inactive;
        }

        var newCycle = IsNewCycle(gcd);
        if (newCycle)
        {
            ClearCast();
        }

        wasGcdActive = true;
        previousElapsed = gcd.Elapsed;
        previousTotal = gcd.Total;

        if (!attached && IsCandidate(gcd, cast))
        {
            Attach(gcd, cast, predictionMilliseconds);
        }

        if (!attached)
        {
            return GcdSegments.Inactive;
        }

        if (cast.SourceSequence == sourceSequence)
        {
            if (!confirmed
                && cast.SourceSequence != 0
                && cast.ResponseSourceSequence == cast.SourceSequence)
            {
                observedSlideStart = Math.Clamp(gcd.Progress, 0f, castEnd);
                confirmed = true;
            }

            if (!cast.IsCasting && !confirmed)
            {
                ClearCast();
                return GcdSegments.Inactive;
            }
        }
        else if (!confirmed)
        {
            ClearCast();
            return GcdSegments.Inactive;
        }

        var slideStart = mode switch
        {
            SlidecastTimingMode.Confirmed => confirmed ? observedSlideStart : castEnd,
            SlidecastTimingMode.Hybrid when confirmed => observedSlideStart,
            _ => predictedSlideStart
        };
        return new GcdSegments(true, Math.Clamp(slideStart, 0f, castEnd), castEnd, confirmed);
    }

    internal void Reset()
    {
        wasGcdActive = false;
        previousElapsed = 0f;
        previousTotal = 0f;
        ClearCast();
    }

    private static bool IsCandidate(GcdState gcd, CastSample cast)
    {
        if (!cast.IsCasting || cast.SourceSequence == 0 || !IsValidCastTime(cast))
        {
            return false;
        }

        var castStart = gcd.Elapsed - cast.Elapsed;
        var usesGlobalCooldown = cast.RecastGroup == 57 || cast.AdditionalRecastGroup == 57;
        return usesGlobalCooldown && MathF.Abs(castStart) <= StartTolerance;
    }

    private bool IsNewCycle(GcdState gcd)
    {
        return !wasGcdActive
            || gcd.Elapsed + CycleTolerance < previousElapsed
            || MathF.Abs(gcd.Total - previousTotal) > CycleTolerance && gcd.Elapsed < StartTolerance;
    }

    private static bool IsValidCastTime(CastSample cast)
    {
        return float.IsFinite(cast.Elapsed)
            && float.IsFinite(cast.Total)
            && cast.Elapsed >= 0f
            && cast.Total > 0f
            && cast.Elapsed <= cast.Total + StartTolerance;
    }

    private void Attach(GcdState gcd, CastSample cast, float predictionMilliseconds)
    {
        attached = true;
        sourceSequence = cast.SourceSequence;
        UpdateBoundaries(gcd, cast, predictionMilliseconds);
    }

    private void UpdateBoundaries(GcdState gcd, CastSample cast, float predictionMilliseconds)
    {
        var castStart = gcd.Elapsed - cast.Elapsed;
        var endSeconds = castStart + cast.Total;
        castEnd = Math.Clamp(endSeconds / gcd.Total, 0f, 1f);
        var graceSeconds = Math.Clamp(predictionMilliseconds, 0f, 1000f) / 1000f;
        predictedSlideStart = Math.Clamp((endSeconds - graceSeconds) / gcd.Total, 0f, castEnd);
    }

    private void ClearCast()
    {
        attached = false;
        confirmed = false;
        sourceSequence = 0;
        predictedSlideStart = 0f;
        observedSlideStart = 0f;
        castEnd = 0f;
    }
}
