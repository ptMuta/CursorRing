using System;

namespace CursorRing;

internal readonly record struct GcdState(bool IsActive, float Progress)
{
    internal static readonly GcdState Inactive = new(false, 0f);

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

        return new GcdState(true, Math.Clamp(elapsed / total, 0f, 1f));
    }
}
