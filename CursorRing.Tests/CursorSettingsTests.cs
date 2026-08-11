using System.Numerics;

namespace CursorRing.Tests;

public sealed class CursorSettingsTests
{
    [Fact]
    public void DefaultsMatchRequestedBehavior()
    {
        var settings = new CursorSettings();

        Assert.Equal(VisibilityMode.CombatOnly, settings.Visibility);
        Assert.Equal(MouseLookVisibility.FollowVisibility, settings.MouseLook);
        Assert.True(settings.ShowGcd);
        Assert.Equal(GcdPlacement.Outer, settings.GcdPlacement);
        Assert.Equal(ProgressBehavior.Drain, settings.ProgressBehavior);
        Assert.Equal(RotationDirection.Clockwise, settings.Rotation);
        Assert.True(settings.ShowGcdTrack);
        Assert.Equal(5f, settings.RingThickness);
        Assert.Equal(6f, settings.DotDiameter);
        Assert.Equal(5f, settings.GcdThickness);
        Assert.True(settings.ShowRingBorder);
        Assert.True(settings.ShowDotBorder);
        Assert.True(settings.ShowGcdBorder);
        Assert.Equal(1f, settings.RingBorderThickness);
        Assert.Equal(1f, settings.DotBorderThickness);
        Assert.Equal(1f, settings.GcdBorderThickness);
        Assert.True(settings.ShowHoverIndicator);
        Assert.Equal(HoverVisibilityMode.WheneverVisible, settings.HoverVisibility);
        Assert.Equal(HoverIndicatorStyle.InwardCarets, settings.HoverIndicatorStyle);
        Assert.Equal(8f, settings.HoverIndicatorSize);
        Assert.Equal(3f, settings.HoverIndicatorThickness);
        Assert.Equal(3f, settings.HoverIndicatorOffset);
        Assert.Equal(0f, settings.HoverIndicatorRotationDegrees);
        Assert.Equal(new Vector4(1f, 0.75f, 0.1f, 1f), settings.HoverIndicatorColor);
        Assert.False(settings.UseHoverRingColor);
        Assert.False(settings.UseHoverDotColor);
        Assert.False(settings.HideDotOnHover);
        Assert.Equal(new Vector4(0f, 0f, 0f, 1f), settings.RingBorderColor);
        Assert.Equal(new Vector4(0f, 0f, 0f, 1f), settings.DotBorderColor);
        Assert.Equal(new Vector4(0f, 0f, 0f, 1f), settings.GcdBorderColor);
        Assert.False(settings.ShowCastSegments);
        Assert.Equal(500f, settings.SlidecastPredictionMilliseconds);
        Assert.False(settings.ShowSegmentDividers);
    }

    [Fact]
    public void NormalizeRepairsInvalidValues()
    {
        var settings = new CursorSettings
        {
            Version = 99,
            Visibility = (VisibilityMode)999,
            MouseLook = (MouseLookVisibility)999,
            RingDiameter = float.NaN,
            RingThickness = float.PositiveInfinity,
            DotDiameter = -10f,
            RingColor = new Vector4(float.NaN, -1f, 2f, 0.5f),
            RingBorderThickness = float.NaN,
            RingBorderColor = new Vector4(-1f, 2f, float.NaN, 0.5f),
            DotBorderThickness = 100f,
            DotBorderColor = new Vector4(2f, -1f, 0.5f, float.NaN),
            HoverVisibility = (HoverVisibilityMode)999,
            HoverIndicatorStyle = (HoverIndicatorStyle)999,
            HoverIndicatorSize = float.PositiveInfinity,
            HoverIndicatorThickness = 100f,
            HoverIndicatorOffset = -1f,
            HoverIndicatorRotationDegrees = float.NaN,
            HoverIndicatorColor = new Vector4(-1f, 2f, float.NaN, 0.5f),
            HoverRingColor = new Vector4(2f, -1f, 0.5f, float.NaN),
            HoverDotColor = new Vector4(-1f, 2f, 0.5f, float.NaN),
            GcdPlacement = (GcdPlacement)999,
            OverlayFill = (OverlayFillStyle)999,
            ProgressBehavior = (ProgressBehavior)999,
            Rotation = (RotationDirection)999,
            GcdThickness = 100f,
            GcdSpacing = -1f,
            GcdColor = new Vector4(-1f, 2f, float.NaN, 0.5f),
            GcdTrackColor = new Vector4(2f, -1f, 0.5f, float.NaN),
            GcdBorderThickness = float.NaN,
            GcdBorderColor = new Vector4(-1f, 2f, float.NaN, 0.5f),
            SlidecastPredictionMilliseconds = float.PositiveInfinity,
            CastSegmentColor = new Vector4(2f, -1f, float.NaN, 0.5f),
            SlidecastSegmentColor = new Vector4(-1f, 2f, 0.5f, float.NaN),
            SegmentDividerThickness = 100f,
            SegmentDividerColor = new Vector4(2f, -1f, float.NaN, 0.5f)
        };

        Assert.True(settings.Normalize());
        Assert.Equal(1, settings.Version);
        Assert.Equal(VisibilityMode.CombatOnly, settings.Visibility);
        Assert.Equal(MouseLookVisibility.FollowVisibility, settings.MouseLook);
        Assert.Equal(48f, settings.RingDiameter);
        Assert.Equal(5f, settings.RingThickness);
        Assert.Equal(1f, settings.DotDiameter);
        Assert.Equal(new Vector4(1f, 0f, 1f, 0.5f), settings.RingColor);
        Assert.Equal(1f, settings.RingBorderThickness);
        Assert.Equal(new Vector4(0f, 1f, 0f, 0.5f), settings.RingBorderColor);
        Assert.Equal(20f, settings.DotBorderThickness);
        Assert.Equal(new Vector4(1f, 0f, 0.5f, 1f), settings.DotBorderColor);
        Assert.Equal(HoverVisibilityMode.WheneverVisible, settings.HoverVisibility);
        Assert.Equal(HoverIndicatorStyle.InwardCarets, settings.HoverIndicatorStyle);
        Assert.Equal(8f, settings.HoverIndicatorSize);
        Assert.Equal(8f, settings.HoverIndicatorThickness);
        Assert.Equal(0f, settings.HoverIndicatorOffset);
        Assert.Equal(0f, settings.HoverIndicatorRotationDegrees);
        Assert.Equal(new Vector4(0f, 1f, 0.1f, 0.5f), settings.HoverIndicatorColor);
        Assert.Equal(new Vector4(1f, 0f, 0.5f, 1f), settings.HoverRingColor);
        Assert.Equal(new Vector4(0f, 1f, 0.5f, 1f), settings.HoverDotColor);
        Assert.Equal(GcdPlacement.Outer, settings.GcdPlacement);
        Assert.Equal(OverlayFillStyle.Stroke, settings.OverlayFill);
        Assert.Equal(ProgressBehavior.Drain, settings.ProgressBehavior);
        Assert.Equal(RotationDirection.Clockwise, settings.Rotation);
        Assert.Equal(20f, settings.GcdThickness);
        Assert.Equal(0f, settings.GcdSpacing);
        Assert.Equal(new Vector4(0f, 1f, 0.1f, 0.5f), settings.GcdColor);
        Assert.Equal(new Vector4(1f, 0f, 0.5f, 0.35f), settings.GcdTrackColor);
        Assert.Equal(1f, settings.GcdBorderThickness);
        Assert.Equal(new Vector4(0f, 1f, 0f, 0.5f), settings.GcdBorderColor);
        Assert.Equal(500f, settings.SlidecastPredictionMilliseconds);
        Assert.Equal(new Vector4(1f, 0f, 1f, 0.5f), settings.CastSegmentColor);
        Assert.Equal(new Vector4(0f, 1f, 0.5f, 1f), settings.SlidecastSegmentColor);
        Assert.Equal(10f, settings.SegmentDividerThickness);
        Assert.Equal(new Vector4(1f, 0f, 0f, 0.5f), settings.SegmentDividerColor);
    }

    [Fact]
    public void NormalizeReportsStableSettings()
    {
        var settings = new CursorSettings();

        Assert.False(settings.Normalize());
    }

    [Fact]
    public void LegacyVisibilityValuesRemainCompatible()
    {
        var always = new CursorSettings { Visibility = (VisibilityMode)0 };
        var combat = new CursorSettings { Visibility = (VisibilityMode)1 };

        Assert.False(always.Normalize());
        Assert.False(combat.Normalize());
        Assert.Equal(VisibilityMode.Always, always.Visibility);
        Assert.Equal(VisibilityMode.CombatOnly, combat.Visibility);
    }

    [Fact]
    public void ResetRestoresEveryDefault()
    {
        var settings = new CursorSettings
        {
            Version = 99,
            Visibility = VisibilityMode.Always,
            MouseLook = MouseLookVisibility.Hidden,
            RingDiameter = 200f,
            RingThickness = 10f,
            DotDiameter = 30f,
            RingColor = Vector4.Zero,
            DotColor = Vector4.Zero,
            ShowRingBorder = false,
            RingBorderThickness = 10f,
            RingBorderColor = Vector4.One,
            ShowDotBorder = false,
            DotBorderThickness = 10f,
            DotBorderColor = Vector4.One,
            ShowHoverIndicator = false,
            HoverVisibility = HoverVisibilityMode.InCombatOnly,
            HoverIndicatorStyle = HoverIndicatorStyle.CornerBrackets,
            HoverIndicatorSize = 20f,
            HoverIndicatorThickness = 5f,
            HoverIndicatorOffset = 10f,
            HoverIndicatorRotationDegrees = 45f,
            HoverIndicatorColor = Vector4.Zero,
            UseHoverRingColor = true,
            HoverRingColor = Vector4.Zero,
            UseHoverDotColor = true,
            HoverDotColor = Vector4.Zero,
            HideDotOnHover = true,
            ShowGcd = false,
            GcdPlacement = GcdPlacement.Inner,
            OverlayFill = OverlayFillStyle.Pie,
            ProgressBehavior = ProgressBehavior.Fill,
            Rotation = RotationDirection.Counterclockwise,
            GcdThickness = 10f,
            GcdSpacing = 20f,
            GcdColor = Vector4.Zero,
            ShowGcdTrack = false,
            GcdTrackColor = Vector4.One,
            ShowGcdBorder = false,
            GcdBorderThickness = 10f,
            GcdBorderColor = Vector4.One,
            ShowCastSegments = true,
            SlidecastPredictionMilliseconds = 100f,
            CastSegmentColor = Vector4.Zero,
            SlidecastSegmentColor = Vector4.Zero,
            ShowSegmentDividers = true,
            SegmentDividerThickness = 8f,
            SegmentDividerColor = Vector4.One
        };

        Assert.True(settings.Reset());

        Assert.Equivalent(new CursorSettings(), settings, true);
        Assert.False(settings.Reset());
    }
}
