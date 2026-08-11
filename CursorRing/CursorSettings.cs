using System;
using System.Collections.Generic;
using System.Numerics;

namespace CursorRing;

public enum VisibilityMode
{
    Always = 0,
    CombatOnly = 1,
    DutyOnly = 2,
    DutyCombat = 3,
    CombatOrDuty = 4
}

public enum MouseLookVisibility
{
    FollowVisibility,
    CombatOnly,
    Hidden
}

public enum HoverVisibilityMode
{
    WheneverVisible,
    OutOfCombatOnly,
    InCombatOnly
}

public enum HoverIndicatorStyle
{
    InwardCarets,
    Crosshair,
    CornerBrackets
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
    public MouseLookVisibility MouseLook { get; set; } = MouseLookVisibility.FollowVisibility;
    public float RingDiameter { get; set; } = 48f;
    public float RingThickness { get; set; } = 5f;
    public float DotDiameter { get; set; } = 6f;
    public Vector4 RingColor { get; set; } = DefaultRingColor;
    public Vector4 DotColor { get; set; } = DefaultDotColor;
    public bool ShowRingBorder { get; set; } = true;
    public float RingBorderThickness { get; set; } = 1f;
    public Vector4 RingBorderColor { get; set; } = DefaultBorderColor;
    public bool ShowDotBorder { get; set; } = true;
    public float DotBorderThickness { get; set; } = 1f;
    public Vector4 DotBorderColor { get; set; } = DefaultBorderColor;
    public bool ShowHoverIndicator { get; set; } = true;
    public HoverVisibilityMode HoverVisibility { get; set; } = HoverVisibilityMode.WheneverVisible;
    public HoverIndicatorStyle HoverIndicatorStyle { get; set; } = HoverIndicatorStyle.InwardCarets;
    public float HoverIndicatorSize { get; set; } = 8f;
    public float HoverIndicatorThickness { get; set; } = 3f;
    public float HoverIndicatorOffset { get; set; } = 3f;
    public float HoverIndicatorRotationDegrees { get; set; }
    public Vector4 HoverIndicatorColor { get; set; } = DefaultGcdColor;
    public bool UseHoverRingColor { get; set; }
    public Vector4 HoverRingColor { get; set; } = DefaultGcdColor;
    public bool UseHoverDotColor { get; set; }
    public Vector4 HoverDotColor { get; set; } = DefaultGcdColor;
    public bool HideDotOnHover { get; set; }
    public bool ShowGcd { get; set; } = true;
    public GcdPlacement GcdPlacement { get; set; } = GcdPlacement.Outer;
    public OverlayFillStyle OverlayFill { get; set; } = OverlayFillStyle.Stroke;
    public ProgressBehavior ProgressBehavior { get; set; } = ProgressBehavior.Drain;
    public RotationDirection Rotation { get; set; } = RotationDirection.Clockwise;
    public float GcdThickness { get; set; } = 5f;
    public float GcdSpacing { get; set; } = 3f;
    public Vector4 GcdColor { get; set; } = DefaultGcdColor;
    public bool ShowGcdTrack { get; set; } = true;
    public Vector4 GcdTrackColor { get; set; } = DefaultTrackColor;
    public bool ShowGcdBorder { get; set; } = true;
    public float GcdBorderThickness { get; set; } = 1f;
    public Vector4 GcdBorderColor { get; set; } = DefaultBorderColor;
    public bool ShowCastSegments { get; set; }
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
        changed |= Update(MouseLook, NormalizeEnum(MouseLook, MouseLookVisibility.FollowVisibility), value => MouseLook = value);
        changed |= Update(RingDiameter, NormalizeNumber(RingDiameter, 48f, 8f, 240f), value => RingDiameter = value);
        changed |= Update(RingThickness, NormalizeNumber(RingThickness, 5f, 1f, MathF.Min(20f, RingDiameter / 2f)), value => RingThickness = value);
        changed |= Update(DotDiameter, NormalizeNumber(DotDiameter, 6f, 1f, MathF.Min(64f, RingDiameter)), value => DotDiameter = value);
        changed |= Update(RingColor, NormalizeColor(RingColor, DefaultRingColor), value => RingColor = value);
        changed |= Update(DotColor, NormalizeColor(DotColor, DefaultDotColor), value => DotColor = value);
        changed |= Update(RingBorderThickness, NormalizeNumber(RingBorderThickness, 1f, 1f, 20f), value => RingBorderThickness = value);
        changed |= Update(RingBorderColor, NormalizeColor(RingBorderColor, DefaultBorderColor), value => RingBorderColor = value);
        changed |= Update(DotBorderThickness, NormalizeNumber(DotBorderThickness, 1f, 1f, 20f), value => DotBorderThickness = value);
        changed |= Update(DotBorderColor, NormalizeColor(DotBorderColor, DefaultBorderColor), value => DotBorderColor = value);
        changed |= Update(HoverVisibility, NormalizeEnum(HoverVisibility, HoverVisibilityMode.WheneverVisible), value => HoverVisibility = value);
        changed |= Update(HoverIndicatorStyle, NormalizeEnum(HoverIndicatorStyle, HoverIndicatorStyle.InwardCarets), value => HoverIndicatorStyle = value);
        changed |= Update(HoverIndicatorSize, NormalizeNumber(HoverIndicatorSize, 8f, 2f, 32f), value => HoverIndicatorSize = value);
        changed |= Update(HoverIndicatorThickness, NormalizeNumber(HoverIndicatorThickness, 3f, 1f, MathF.Min(8f, HoverIndicatorSize)), value => HoverIndicatorThickness = value);
        changed |= Update(HoverIndicatorOffset, NormalizeNumber(HoverIndicatorOffset, 3f, 0f, 40f), value => HoverIndicatorOffset = value);
        changed |= Update(HoverIndicatorRotationDegrees, NormalizeNumber(HoverIndicatorRotationDegrees, 0f, -180f, 180f), value => HoverIndicatorRotationDegrees = value);
        changed |= Update(HoverIndicatorColor, NormalizeColor(HoverIndicatorColor, DefaultGcdColor), value => HoverIndicatorColor = value);
        changed |= Update(HoverRingColor, NormalizeColor(HoverRingColor, DefaultGcdColor), value => HoverRingColor = value);
        changed |= Update(HoverDotColor, NormalizeColor(HoverDotColor, DefaultGcdColor), value => HoverDotColor = value);
        changed |= Update(GcdPlacement, NormalizeEnum(GcdPlacement, GcdPlacement.Outer), value => GcdPlacement = value);
        changed |= Update(OverlayFill, NormalizeEnum(OverlayFill, OverlayFillStyle.Stroke), value => OverlayFill = value);
        changed |= Update(ProgressBehavior, NormalizeEnum(ProgressBehavior, ProgressBehavior.Drain), value => ProgressBehavior = value);
        changed |= Update(Rotation, NormalizeEnum(Rotation, RotationDirection.Clockwise), value => Rotation = value);
        changed |= Update(GcdThickness, NormalizeNumber(GcdThickness, 5f, 1f, 20f), value => GcdThickness = value);
        changed |= Update(GcdSpacing, NormalizeNumber(GcdSpacing, 3f, 0f, 40f), value => GcdSpacing = value);
        changed |= Update(GcdColor, NormalizeColor(GcdColor, DefaultGcdColor), value => GcdColor = value);
        changed |= Update(GcdTrackColor, NormalizeColor(GcdTrackColor, DefaultTrackColor), value => GcdTrackColor = value);
        changed |= Update(GcdBorderThickness, NormalizeNumber(GcdBorderThickness, 1f, 1f, 20f), value => GcdBorderThickness = value);
        changed |= Update(GcdBorderColor, NormalizeColor(GcdBorderColor, DefaultBorderColor), value => GcdBorderColor = value);
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
        changed |= Update(MouseLook, MouseLookVisibility.FollowVisibility, value => MouseLook = value);
        changed |= Update(RingDiameter, 48f, value => RingDiameter = value);
        changed |= Update(RingThickness, 5f, value => RingThickness = value);
        changed |= Update(DotDiameter, 6f, value => DotDiameter = value);
        changed |= Update(RingColor, DefaultRingColor, value => RingColor = value);
        changed |= Update(DotColor, DefaultDotColor, value => DotColor = value);
        changed |= Update(ShowRingBorder, true, value => ShowRingBorder = value);
        changed |= Update(RingBorderThickness, 1f, value => RingBorderThickness = value);
        changed |= Update(RingBorderColor, DefaultBorderColor, value => RingBorderColor = value);
        changed |= Update(ShowDotBorder, true, value => ShowDotBorder = value);
        changed |= Update(DotBorderThickness, 1f, value => DotBorderThickness = value);
        changed |= Update(DotBorderColor, DefaultBorderColor, value => DotBorderColor = value);
        changed |= Update(ShowHoverIndicator, true, value => ShowHoverIndicator = value);
        changed |= Update(HoverVisibility, HoverVisibilityMode.WheneverVisible, value => HoverVisibility = value);
        changed |= Update(HoverIndicatorStyle, HoverIndicatorStyle.InwardCarets, value => HoverIndicatorStyle = value);
        changed |= Update(HoverIndicatorSize, 8f, value => HoverIndicatorSize = value);
        changed |= Update(HoverIndicatorThickness, 3f, value => HoverIndicatorThickness = value);
        changed |= Update(HoverIndicatorOffset, 3f, value => HoverIndicatorOffset = value);
        changed |= Update(HoverIndicatorRotationDegrees, 0f, value => HoverIndicatorRotationDegrees = value);
        changed |= Update(HoverIndicatorColor, DefaultGcdColor, value => HoverIndicatorColor = value);
        changed |= Update(UseHoverRingColor, false, value => UseHoverRingColor = value);
        changed |= Update(HoverRingColor, DefaultGcdColor, value => HoverRingColor = value);
        changed |= Update(UseHoverDotColor, false, value => UseHoverDotColor = value);
        changed |= Update(HoverDotColor, DefaultGcdColor, value => HoverDotColor = value);
        changed |= Update(HideDotOnHover, false, value => HideDotOnHover = value);
        changed |= Update(ShowGcd, true, value => ShowGcd = value);
        changed |= Update(GcdPlacement, GcdPlacement.Outer, value => GcdPlacement = value);
        changed |= Update(OverlayFill, OverlayFillStyle.Stroke, value => OverlayFill = value);
        changed |= Update(ProgressBehavior, ProgressBehavior.Drain, value => ProgressBehavior = value);
        changed |= Update(Rotation, RotationDirection.Clockwise, value => Rotation = value);
        changed |= Update(GcdThickness, 5f, value => GcdThickness = value);
        changed |= Update(GcdSpacing, 3f, value => GcdSpacing = value);
        changed |= Update(GcdColor, DefaultGcdColor, value => GcdColor = value);
        changed |= Update(ShowGcdTrack, true, value => ShowGcdTrack = value);
        changed |= Update(GcdTrackColor, DefaultTrackColor, value => GcdTrackColor = value);
        changed |= Update(ShowGcdBorder, true, value => ShowGcdBorder = value);
        changed |= Update(GcdBorderThickness, 1f, value => GcdBorderThickness = value);
        changed |= Update(GcdBorderColor, DefaultBorderColor, value => GcdBorderColor = value);
        changed |= Update(ShowCastSegments, false, value => ShowCastSegments = value);
        changed |= Update(SlidecastPredictionMilliseconds, 500f, value => SlidecastPredictionMilliseconds = value);
        changed |= Update(CastSegmentColor, DefaultCastColor, value => CastSegmentColor = value);
        changed |= Update(SlidecastSegmentColor, DefaultSlidecastColor, value => SlidecastSegmentColor = value);
        changed |= Update(ShowSegmentDividers, false, value => ShowSegmentDividers = value);
        changed |= Update(SegmentDividerThickness, 1f, value => SegmentDividerThickness = value);
        changed |= Update(SegmentDividerColor, DefaultBorderColor, value => SegmentDividerColor = value);
        return changed;
    }

    public CursorSettings Copy()
    {
        var copy = new CursorSettings();
        copy.CopyFrom(this);
        return copy;
    }

    public void CopyFrom(CursorSettings source)
    {
        Version = source.Version;
        Visibility = source.Visibility;
        MouseLook = source.MouseLook;
        RingDiameter = source.RingDiameter;
        RingThickness = source.RingThickness;
        DotDiameter = source.DotDiameter;
        RingColor = source.RingColor;
        DotColor = source.DotColor;
        ShowRingBorder = source.ShowRingBorder;
        RingBorderThickness = source.RingBorderThickness;
        RingBorderColor = source.RingBorderColor;
        ShowDotBorder = source.ShowDotBorder;
        DotBorderThickness = source.DotBorderThickness;
        DotBorderColor = source.DotBorderColor;
        ShowHoverIndicator = source.ShowHoverIndicator;
        HoverVisibility = source.HoverVisibility;
        HoverIndicatorStyle = source.HoverIndicatorStyle;
        HoverIndicatorSize = source.HoverIndicatorSize;
        HoverIndicatorThickness = source.HoverIndicatorThickness;
        HoverIndicatorOffset = source.HoverIndicatorOffset;
        HoverIndicatorRotationDegrees = source.HoverIndicatorRotationDegrees;
        HoverIndicatorColor = source.HoverIndicatorColor;
        UseHoverRingColor = source.UseHoverRingColor;
        HoverRingColor = source.HoverRingColor;
        UseHoverDotColor = source.UseHoverDotColor;
        HoverDotColor = source.HoverDotColor;
        HideDotOnHover = source.HideDotOnHover;
        ShowGcd = source.ShowGcd;
        GcdPlacement = source.GcdPlacement;
        OverlayFill = source.OverlayFill;
        ProgressBehavior = source.ProgressBehavior;
        Rotation = source.Rotation;
        GcdThickness = source.GcdThickness;
        GcdSpacing = source.GcdSpacing;
        GcdColor = source.GcdColor;
        ShowGcdTrack = source.ShowGcdTrack;
        GcdTrackColor = source.GcdTrackColor;
        ShowGcdBorder = source.ShowGcdBorder;
        GcdBorderThickness = source.GcdBorderThickness;
        GcdBorderColor = source.GcdBorderColor;
        ShowCastSegments = source.ShowCastSegments;
        SlidecastPredictionMilliseconds = source.SlidecastPredictionMilliseconds;
        CastSegmentColor = source.CastSegmentColor;
        SlidecastSegmentColor = source.SlidecastSegmentColor;
        ShowSegmentDividers = source.ShowSegmentDividers;
        SegmentDividerThickness = source.SegmentDividerThickness;
        SegmentDividerColor = source.SegmentDividerColor;
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

public enum AssignmentScope
{
    Territory = 0,
    Duty = 1,
    PvP = 2,
    DutyAny = 3,
    PvPAny = 4
}

public sealed class CursorProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public CursorSettings Settings { get; set; } = new();
}

public sealed class CursorAssignment
{
    public AssignmentScope Scope { get; set; }
    public uint TargetId { get; set; }
    public Guid ProfileId { get; set; }
}
