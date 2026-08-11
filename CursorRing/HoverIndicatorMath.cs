using System;

namespace CursorRing;

internal readonly record struct HoverIndicatorGeometry(
    float DotExtent,
    float InnerRadius,
    float OuterRadius,
    float CaretHalfWidth,
    float Thickness,
    float RotationRadians,
    float Extent);

internal static class HoverIndicatorMath
{
    internal static HoverIndicatorGeometry GetGeometry(CursorSettings settings)
    {
        var dotExtent = (settings.DotDiameter / 2f) + (settings.ShowDotBorder ? settings.DotBorderThickness : 0f);
        var innerRadius = dotExtent + settings.HoverIndicatorOffset;
        var outerRadius = innerRadius + settings.HoverIndicatorSize;
        var caretHalfWidth = settings.HoverIndicatorSize;
        var rotation = settings.HoverIndicatorRotationDegrees * MathF.PI / 180f;
        var extent = settings.HoverIndicatorStyle == HoverIndicatorStyle.CornerBrackets
            ? MathF.Sqrt(2f) * outerRadius
            : settings.HoverIndicatorStyle == HoverIndicatorStyle.InwardCarets
                ? MathF.Sqrt((outerRadius * outerRadius) + (caretHalfWidth * caretHalfWidth))
                : outerRadius;
        return new HoverIndicatorGeometry(dotExtent, innerRadius, outerRadius, caretHalfWidth, settings.HoverIndicatorThickness, rotation, extent);
    }
}
