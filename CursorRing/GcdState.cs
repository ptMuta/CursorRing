using System;

namespace CursorRing;

internal readonly record struct GcdState(bool IsActive, float Elapsed, float Total)
{
    internal static readonly GcdState Inactive = new(false, 0f, 0f);

    internal float Progress => IsActive && float.IsFinite(Elapsed) && float.IsFinite(Total) && Total > 0f
        ? Math.Clamp(Elapsed / Total, 0f, 1f)
        : 0f;

    internal static GcdState Create(bool nativeActive, float elapsed, float total)
    {
        if (!float.IsFinite(elapsed)
            || !float.IsFinite(total)
            || elapsed < 0f
            || total <= 0f
            || elapsed >= total
            || (!nativeActive && elapsed == 0f))
        {
            return Inactive;
        }

        return new GcdState(true, elapsed, total);
    }
}
