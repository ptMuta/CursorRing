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

internal readonly record struct CastTimeline(
    bool IsActive,
    float Elapsed,
    float Total,
    float SlideStart,
    float CastEnd,
    bool IsConfirmed)
{
    internal static CastTimeline Inactive => default;
    internal float Progress => IsActive ? Math.Clamp(Elapsed / Total, 0f, 1f) : 0f;
}

internal sealed class CastSegmentationTracker
{
    private const float CycleTolerance = 0.05f;
    private const float StartTolerance = 0.25f;
    private bool wasGcdActive;
    private bool attached;
    private bool confirmed;
    private uint sourceSequence;
    private float previousGcdElapsed;
    private float previousGcdTotal;
    private float castStart;
    private float castElapsed;
    private float castTotal;
    private float observedSlideStart;

    internal bool NeedsCast(GcdState gcd)
    {
        return !attached || gcd.IsActive && IsNewCycle(gcd);
    }

    internal bool IsTracking => attached;

    internal CastTimeline Update(GcdState gcd, CastSample cast, float predictionMilliseconds)
    {
        var newCycle = gcd.IsActive && IsNewCycle(gcd);
        if (newCycle)
        {
            ClearCast();
        }

        if (gcd.IsActive)
        {
            wasGcdActive = true;
            previousGcdElapsed = gcd.Elapsed;
            previousGcdTotal = gcd.Total;
        }
        else
        {
            wasGcdActive = false;
        }

        if (!attached && IsCandidate(gcd, cast))
        {
            Attach(gcd, cast);
        }

        if (!attached)
        {
            return CastTimeline.Inactive;
        }

        if (cast.SourceSequence != sourceSequence)
        {
            if (!cast.IsCasting && confirmed && gcd.IsActive)
            {
                return CreateTimeline(gcd, predictionMilliseconds);
            }

            ClearCast();
            return CastTimeline.Inactive;
        }

        if (!confirmed
            && sourceSequence != 0
            && cast.ResponseSourceSequence == sourceSequence
            && IsValidResponseElapsed(cast.Elapsed))
        {
            observedSlideStart = castStart + cast.Elapsed;
            confirmed = true;
        }

        if (cast.IsCasting)
        {
            if (!IsValidCastTime(cast))
            {
                ClearCast();
                return CastTimeline.Inactive;
            }

            castTotal = cast.Total;
            castElapsed = cast.Elapsed;
        }
        else if (!confirmed || !gcd.IsActive)
        {
            ClearCast();
            return CastTimeline.Inactive;
        }

        return CreateTimeline(gcd, predictionMilliseconds);
    }

    internal void Reset()
    {
        wasGcdActive = false;
        previousGcdElapsed = 0f;
        previousGcdTotal = 0f;
        ClearCast();
    }

    private static bool IsCandidate(GcdState gcd, CastSample cast)
    {
        if (!gcd.IsActive || !cast.IsCasting || cast.SourceSequence == 0 || !IsValidCastTime(cast))
        {
            return false;
        }

        var start = gcd.Elapsed - cast.Elapsed;
        var usesGlobalCooldown = cast.RecastGroup == 57 || cast.AdditionalRecastGroup == 57;
        return usesGlobalCooldown && MathF.Abs(start) <= StartTolerance;
    }

    private bool IsNewCycle(GcdState gcd)
    {
        return !wasGcdActive
            || gcd.Elapsed + CycleTolerance < previousGcdElapsed
            || MathF.Abs(gcd.Total - previousGcdTotal) > CycleTolerance && gcd.Elapsed < StartTolerance;
    }

    private static bool IsValidCastTime(CastSample cast)
    {
        return float.IsFinite(cast.Elapsed)
            && float.IsFinite(cast.Total)
            && cast.Elapsed >= 0f
            && cast.Total > 0f
            && cast.Elapsed <= cast.Total + StartTolerance;
    }

    private bool IsValidResponseElapsed(float elapsed)
    {
        return float.IsFinite(elapsed) && elapsed >= 0f && elapsed <= castTotal + StartTolerance;
    }

    private void Attach(GcdState gcd, CastSample cast)
    {
        attached = true;
        sourceSequence = cast.SourceSequence;
        castStart = gcd.Elapsed - cast.Elapsed;
        castElapsed = cast.Elapsed;
        castTotal = cast.Total;
    }

    private CastTimeline CreateTimeline(GcdState gcd, float predictionMilliseconds)
    {
        var castEndSeconds = castStart + castTotal;
        var total = MathF.Max(gcd.IsActive ? gcd.Total : 0f, castEndSeconds);
        var elapsed = gcd.IsActive ? gcd.Elapsed : castStart + castElapsed;

        var graceSeconds = Math.Clamp(predictionMilliseconds, 0f, 1000f) / 1000f;
        var predicted = Math.Clamp(castEndSeconds - graceSeconds, 0f, castEndSeconds);
        var slideSeconds = confirmed ? observedSlideStart : predicted;
        return new CastTimeline(
            true,
            Math.Clamp(elapsed, 0f, total),
            total,
            Math.Clamp(slideSeconds / total, 0f, 1f),
            Math.Clamp(castEndSeconds / total, 0f, 1f),
            confirmed);
    }

    private void ClearCast()
    {
        attached = false;
        confirmed = false;
        sourceSequence = 0;
        castStart = 0f;
        castElapsed = 0f;
        castTotal = 0f;
        observedSlideStart = 0f;
    }
}
