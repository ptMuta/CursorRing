using System;
using System.Collections.Generic;
using System.Numerics;

namespace CursorRing;

public enum VisibilityMode
{
    Always,
    CombatOnly
}

public enum MouseLookVisibility
{
    FollowVisibility,
    CombatOnly,
    Hidden
}

public enum GcdPlacement
{
    Outer,
    Inner,
    Overlay
}

public enum OverlayFillStyle
{
    Stroke,
    Pie
}

public enum ProgressBehavior
{
    Fill,
    Drain
}

public enum RotationDirection
{
    Clockwise,
    Counterclockwise
}

public enum SlidecastTimingMode
{
    Predicted,
    Confirmed,
    Hybrid
}

public class CursorSettings
{
    private static readonly Vector4 DefaultRingColor = Vector4.One;
    private static readonly Vector4 DefaultDotColor = Vector4.One;
    private static readonly Vector4 DefaultBorderColor = new(0f, 0f, 0f, 1f);
    private static readonly Vector4 DefaultGcdColor = new(1f, 0.75f, 0.1f, 1f);
    private static readonly Vector4 DefaultTrackColor = new(0f, 0f, 0f, 0.35f);
    private static readonly Vector4 DefaultCastColor = new(0.25f, 0.65f, 1f, 1f);
    private static readonly Vector4 DefaultSlidecastColor = new(0.25f, 1f, 0.45f, 1f);

    public int Version { get; set; } = 1;
    public VisibilityMode Visibility { get; set; } = VisibilityMode.CombatOnly;
    public MouseLookVisibility MouseLook { get; set; } = MouseLookVisibility.CombatOnly;
    public float RingDiameter { get; set; } = 48f;
    public float RingThickness { get; set; } = 3f;
    public float DotDiameter { get; set; } = 4f;
    public Vector4 RingColor { get; set; } = DefaultRingColor;
    public Vector4 DotColor { get; set; } = DefaultDotColor;
    public bool ShowRingBorder { get; set; }
    public float RingBorderThickness { get; set; } = 1f;
    public Vector4 RingBorderColor { get; set; } = DefaultBorderColor;
    public bool ShowDotBorder { get; set; }
    public float DotBorderThickness { get; set; } = 1f;
    public Vector4 DotBorderColor { get; set; } = DefaultBorderColor;
    public GcdPlacement GcdPlacement { get; set; } = GcdPlacement.Outer;
    public OverlayFillStyle OverlayFill { get; set; } = OverlayFillStyle.Stroke;
    public ProgressBehavior ProgressBehavior { get; set; } = ProgressBehavior.Drain;
    public RotationDirection Rotation { get; set; } = RotationDirection.Clockwise;
    public float GcdThickness { get; set; } = 3f;
    public float GcdSpacing { get; set; } = 3f;
    public Vector4 GcdColor { get; set; } = DefaultGcdColor;
    public bool ShowGcdTrack { get; set; } = true;
    public Vector4 GcdTrackColor { get; set; } = DefaultTrackColor;
    public bool ShowGcdBorder { get; set; }
    public float GcdBorderThickness { get; set; } = 1f;
    public Vector4 GcdBorderColor { get; set; } = DefaultBorderColor;
    public bool ShowCastSegments { get; set; }
    public SlidecastTimingMode SlidecastTiming { get; set; } = SlidecastTimingMode.Hybrid;
    public float SlidecastPredictionMilliseconds { get; set; } = 500f;
    public Vector4 CastSegmentColor { get; set; } = DefaultCastColor;
    public Vector4 SlidecastSegmentColor { get; set; } = DefaultSlidecastColor;
    public bool ShowSegmentDividers { get; set; }
    public float SegmentDividerThickness { get; set; } = 1f;
    public Vector4 SegmentDividerColor { get; set; } = DefaultBorderColor;

    public bool Normalize()
    {
        var changed = false;
        changed |= Update(Version, 1, value => Version = value);
        changed |= Update(Visibility, NormalizeEnum(Visibility, VisibilityMode.CombatOnly), value => Visibility = value);
        changed |= Update(MouseLook, NormalizeEnum(MouseLook, MouseLookVisibility.CombatOnly), value => MouseLook = value);
        changed |= Update(RingDiameter, NormalizeNumber(RingDiameter, 48f, 8f, 240f), value => RingDiameter = value);
        changed |= Update(RingThickness, NormalizeNumber(RingThickness, 3f, 1f, MathF.Min(20f, RingDiameter / 2f)), value => RingThickness = value);
        changed |= Update(DotDiameter, NormalizeNumber(DotDiameter, 4f, 1f, MathF.Min(64f, RingDiameter)), value => DotDiameter = value);
        changed |= Update(RingColor, NormalizeColor(RingColor, DefaultRingColor), value => RingColor = value);
        changed |= Update(DotColor, NormalizeColor(DotColor, DefaultDotColor), value => DotColor = value);
        changed |= Update(RingBorderThickness, NormalizeNumber(RingBorderThickness, 1f, 1f, 20f), value => RingBorderThickness = value);
        changed |= Update(RingBorderColor, NormalizeColor(RingBorderColor, DefaultBorderColor), value => RingBorderColor = value);
        changed |= Update(DotBorderThickness, NormalizeNumber(DotBorderThickness, 1f, 1f, 20f), value => DotBorderThickness = value);
        changed |= Update(DotBorderColor, NormalizeColor(DotBorderColor, DefaultBorderColor), value => DotBorderColor = value);
        changed |= Update(GcdPlacement, NormalizeEnum(GcdPlacement, GcdPlacement.Outer), value => GcdPlacement = value);
        changed |= Update(OverlayFill, NormalizeEnum(OverlayFill, OverlayFillStyle.Stroke), value => OverlayFill = value);
        changed |= Update(ProgressBehavior, NormalizeEnum(ProgressBehavior, ProgressBehavior.Drain), value => ProgressBehavior = value);
        changed |= Update(Rotation, NormalizeEnum(Rotation, RotationDirection.Clockwise), value => Rotation = value);
        changed |= Update(GcdThickness, NormalizeNumber(GcdThickness, 3f, 1f, 20f), value => GcdThickness = value);
        changed |= Update(GcdSpacing, NormalizeNumber(GcdSpacing, 3f, 0f, 40f), value => GcdSpacing = value);
        changed |= Update(GcdColor, NormalizeColor(GcdColor, DefaultGcdColor), value => GcdColor = value);
        changed |= Update(GcdTrackColor, NormalizeColor(GcdTrackColor, DefaultTrackColor), value => GcdTrackColor = value);
        changed |= Update(GcdBorderThickness, NormalizeNumber(GcdBorderThickness, 1f, 1f, 20f), value => GcdBorderThickness = value);
        changed |= Update(GcdBorderColor, NormalizeColor(GcdBorderColor, DefaultBorderColor), value => GcdBorderColor = value);
        changed |= Update(SlidecastTiming, NormalizeEnum(SlidecastTiming, SlidecastTimingMode.Hybrid), value => SlidecastTiming = value);
        changed |= Update(SlidecastPredictionMilliseconds, NormalizeNumber(SlidecastPredictionMilliseconds, 500f, 0f, 1000f), value => SlidecastPredictionMilliseconds = value);
        changed |= Update(CastSegmentColor, NormalizeColor(CastSegmentColor, DefaultCastColor), value => CastSegmentColor = value);
        changed |= Update(SlidecastSegmentColor, NormalizeColor(SlidecastSegmentColor, DefaultSlidecastColor), value => SlidecastSegmentColor = value);
        changed |= Update(SegmentDividerThickness, NormalizeNumber(SegmentDividerThickness, 1f, 1f, 10f), value => SegmentDividerThickness = value);
        changed |= Update(SegmentDividerColor, NormalizeColor(SegmentDividerColor, DefaultBorderColor), value => SegmentDividerColor = value);
        return changed;
    }

    public bool Reset()
    {
        var changed = false;
        changed |= Update(Version, 1, value => Version = value);
        changed |= Update(Visibility, VisibilityMode.CombatOnly, value => Visibility = value);
        changed |= Update(MouseLook, MouseLookVisibility.CombatOnly, value => MouseLook = value);
        changed |= Update(RingDiameter, 48f, value => RingDiameter = value);
        changed |= Update(RingThickness, 3f, value => RingThickness = value);
        changed |= Update(DotDiameter, 4f, value => DotDiameter = value);
        changed |= Update(RingColor, DefaultRingColor, value => RingColor = value);
        changed |= Update(DotColor, DefaultDotColor, value => DotColor = value);
        changed |= Update(ShowRingBorder, false, value => ShowRingBorder = value);
        changed |= Update(RingBorderThickness, 1f, value => RingBorderThickness = value);
        changed |= Update(RingBorderColor, DefaultBorderColor, value => RingBorderColor = value);
        changed |= Update(ShowDotBorder, false, value => ShowDotBorder = value);
        changed |= Update(DotBorderThickness, 1f, value => DotBorderThickness = value);
        changed |= Update(DotBorderColor, DefaultBorderColor, value => DotBorderColor = value);
        changed |= Update(GcdPlacement, GcdPlacement.Outer, value => GcdPlacement = value);
        changed |= Update(OverlayFill, OverlayFillStyle.Stroke, value => OverlayFill = value);
        changed |= Update(ProgressBehavior, ProgressBehavior.Drain, value => ProgressBehavior = value);
        changed |= Update(Rotation, RotationDirection.Clockwise, value => Rotation = value);
        changed |= Update(GcdThickness, 3f, value => GcdThickness = value);
        changed |= Update(GcdSpacing, 3f, value => GcdSpacing = value);
        changed |= Update(GcdColor, DefaultGcdColor, value => GcdColor = value);
        changed |= Update(ShowGcdTrack, true, value => ShowGcdTrack = value);
        changed |= Update(GcdTrackColor, DefaultTrackColor, value => GcdTrackColor = value);
        changed |= Update(ShowGcdBorder, false, value => ShowGcdBorder = value);
        changed |= Update(GcdBorderThickness, 1f, value => GcdBorderThickness = value);
        changed |= Update(GcdBorderColor, DefaultBorderColor, value => GcdBorderColor = value);
        changed |= Update(ShowCastSegments, false, value => ShowCastSegments = value);
        changed |= Update(SlidecastTiming, SlidecastTimingMode.Hybrid, value => SlidecastTiming = value);
        changed |= Update(SlidecastPredictionMilliseconds, 500f, value => SlidecastPredictionMilliseconds = value);
        changed |= Update(CastSegmentColor, DefaultCastColor, value => CastSegmentColor = value);
        changed |= Update(SlidecastSegmentColor, DefaultSlidecastColor, value => SlidecastSegmentColor = value);
        changed |= Update(ShowSegmentDividers, false, value => ShowSegmentDividers = value);
        changed |= Update(SegmentDividerThickness, 1f, value => SegmentDividerThickness = value);
        changed |= Update(SegmentDividerColor, DefaultBorderColor, value => SegmentDividerColor = value);
        return changed;
    }

    private static T NormalizeEnum<T>(T value, T fallback) where T : struct, Enum
    {
        return Enum.IsDefined(value) ? value : fallback;
    }

    private static float NormalizeNumber(float value, float fallback, float minimum, float maximum)
    {
        return float.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;
    }

    private static Vector4 NormalizeColor(Vector4 value, Vector4 fallback)
    {
        return new Vector4(
            NormalizeComponent(value.X, fallback.X),
            NormalizeComponent(value.Y, fallback.Y),
            NormalizeComponent(value.Z, fallback.Z),
            NormalizeComponent(value.W, fallback.W));
    }

    private static float NormalizeComponent(float value, float fallback)
    {
        return float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : fallback;
    }

    private static bool Update<T>(T current, T value, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
        {
            return false;
        }

        setter(value);
        return true;
    }
}
