using System;

namespace CursorRing;

internal readonly record struct ArcAngles(float Start, float End);

internal readonly record struct RingGeometry(float Main, float Inner, float Outer, float Pie, float InnerThickness);

internal static class RingMath
{
    internal const float Top = -MathF.PI / 2f;
    internal const float FullTurn = MathF.PI * 2f;

    internal static ArcAngles GetArc(float elapsedProgress, ProgressBehavior behavior, RotationDirection direction)
    {
        var progress = Math.Clamp(elapsedProgress, 0f, 1f);
        var rotation = direction == RotationDirection.Clockwise ? 1f : -1f;
        var turn = rotation * FullTurn;
        return behavior == ProgressBehavior.Fill
            ? new ArcAngles(Top, Top + (turn * progress))
            : new ArcAngles(Top + (turn * progress), Top + turn);
    }

    internal static RingGeometry GetGeometry(CursorSettings settings)
    {
        var main = settings.RingDiameter / 2f;
        var offset = ((settings.RingThickness + settings.GcdThickness) / 2f) + settings.GcdSpacing;
        var outer = main + offset;
        var pie = MathF.Max(0.5f, main - (settings.RingThickness / 2f));
        var innerLimit = MathF.Max(0.5f, main - (settings.RingThickness / 2f));
        var innerThickness = MathF.Min(settings.GcdThickness, innerLimit);
        var innerSpacing = MathF.Min(settings.GcdSpacing, innerLimit - innerThickness);
        var inner = innerLimit - innerSpacing - (innerThickness / 2f);
        return new RingGeometry(main, inner, outer, pie, innerThickness);
    }
}
