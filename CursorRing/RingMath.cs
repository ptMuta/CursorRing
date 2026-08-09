using System;

namespace CursorRing;

internal readonly record struct ArcAngles(float Start, float End);

internal readonly record struct ProgressRange(float Start, float End)
{
    internal bool IsVisible => End - Start > 0.0001f;
}

internal readonly record struct RingGeometry(
    float Main,
    float Inner,
    float Outer,
    float Pie,
    float InnerThickness,
    float InnerBorderThickness);

internal static class RingMath
{
    internal const float Top = -MathF.PI / 2f;
    internal const float FullTurn = MathF.PI * 2f;

    internal static ArcAngles GetArc(float elapsedProgress, ProgressBehavior behavior, RotationDirection direction)
    {
        var range = GetVisibleRange(elapsedProgress, behavior);
        return GetArc(range.Start, range.End, direction);
    }

    internal static ArcAngles GetArc(float startProgress, float endProgress, RotationDirection direction)
    {
        var start = Math.Clamp(startProgress, 0f, 1f);
        var end = Math.Clamp(endProgress, start, 1f);
        var rotation = direction == RotationDirection.Clockwise ? 1f : -1f;
        var turn = rotation * FullTurn;
        return new ArcAngles(Top + (turn * start), Top + (turn * end));
    }

    internal static ProgressRange GetVisibleRange(float elapsedProgress, ProgressBehavior behavior)
    {
        var progress = Math.Clamp(elapsedProgress, 0f, 1f);
        return behavior == ProgressBehavior.Fill
            ? new ProgressRange(0f, progress)
            : new ProgressRange(progress, 1f);
    }

    internal static ProgressRange Intersect(ProgressRange left, ProgressRange right)
    {
        return new ProgressRange(MathF.Max(left.Start, right.Start), MathF.Min(left.End, right.End));
    }

    internal static float ClampStrokeBorder(float radius, float thickness, float border)
    {
        return Math.Clamp(border, 0f, MathF.Max(0f, radius - (thickness / 2f)));
    }

    internal static float ClampPieBorder(float radius, float border)
    {
        return Math.Clamp(border, 0f, MathF.Max(0f, radius - 0.5f));
    }

    internal static RingGeometry GetGeometry(CursorSettings settings)
    {
        var main = settings.RingDiameter / 2f;
        var mainBorder = settings.ShowRingBorder ? settings.RingBorderThickness : 0f;
        var gcdBorder = settings.ShowGcdBorder ? settings.GcdBorderThickness : 0f;
        var mainExtent = (settings.RingThickness / 2f) + mainBorder;
        var gcdExtent = (settings.GcdThickness / 2f) + gcdBorder;
        var outer = main + mainExtent + settings.GcdSpacing + gcdExtent;
        var pie = MathF.Max(0.5f, main - mainExtent);
        var innerLimit = pie;
        var innerBorder = MathF.Min(gcdBorder, MathF.Max(0f, (innerLimit - 0.5f) / 2f));
        var innerThickness = MathF.Min(settings.GcdThickness, MathF.Max(0.5f, innerLimit - (innerBorder * 2f)));
        var visualWidth = innerThickness + (innerBorder * 2f);
        var innerSpacing = MathF.Min(settings.GcdSpacing, MathF.Max(0f, innerLimit - visualWidth));
        var inner = innerLimit - innerSpacing - (visualWidth / 2f);
        return new RingGeometry(main, inner, outer, pie, innerThickness, innerBorder);
    }
}
