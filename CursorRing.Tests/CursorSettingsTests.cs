using System.Numerics;

namespace CursorRing.Tests;

public sealed class CursorSettingsTests
{
    [Fact]
    public void DefaultsMatchRequestedBehavior()
    {
        var settings = new CursorSettings();

        Assert.Equal(VisibilityMode.CombatOnly, settings.Visibility);
        Assert.Equal(MouseLookVisibility.CombatOnly, settings.MouseLook);
        Assert.True(settings.ShowGcd);
        Assert.Equal(GcdPlacement.Outer, settings.GcdPlacement);
        Assert.Equal(ProgressBehavior.Drain, settings.ProgressBehavior);
        Assert.Equal(RotationDirection.Clockwise, settings.Rotation);
        Assert.True(settings.ShowGcdTrack);
        Assert.False(settings.ShowRingBorder);
        Assert.False(settings.ShowDotBorder);
        Assert.False(settings.ShowGcdBorder);
        Assert.False(settings.ShowCastSegments);
        Assert.Equal(SlidecastTimingMode.Hybrid, settings.SlidecastTiming);
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
            SlidecastTiming = (SlidecastTimingMode)999,
            SlidecastPredictionMilliseconds = float.PositiveInfinity,
            CastSegmentColor = new Vector4(2f, -1f, float.NaN, 0.5f),
            SlidecastSegmentColor = new Vector4(-1f, 2f, 0.5f, float.NaN),
            SegmentDividerThickness = 100f,
            SegmentDividerColor = new Vector4(2f, -1f, float.NaN, 0.5f)
        };

        Assert.True(settings.Normalize());
        Assert.Equal(1, settings.Version);
        Assert.Equal(VisibilityMode.CombatOnly, settings.Visibility);
        Assert.Equal(MouseLookVisibility.CombatOnly, settings.MouseLook);
        Assert.Equal(48f, settings.RingDiameter);
        Assert.Equal(3f, settings.RingThickness);
        Assert.Equal(1f, settings.DotDiameter);
        Assert.Equal(new Vector4(1f, 0f, 1f, 0.5f), settings.RingColor);
        Assert.Equal(1f, settings.RingBorderThickness);
        Assert.Equal(new Vector4(0f, 1f, 0f, 0.5f), settings.RingBorderColor);
        Assert.Equal(20f, settings.DotBorderThickness);
        Assert.Equal(new Vector4(1f, 0f, 0.5f, 1f), settings.DotBorderColor);
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
        Assert.Equal(SlidecastTimingMode.Hybrid, settings.SlidecastTiming);
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
            ShowRingBorder = true,
            RingBorderThickness = 10f,
            RingBorderColor = Vector4.One,
            ShowDotBorder = true,
            DotBorderThickness = 10f,
            DotBorderColor = Vector4.One,
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
            ShowGcdBorder = true,
            GcdBorderThickness = 10f,
            GcdBorderColor = Vector4.One,
            ShowCastSegments = true,
            SlidecastTiming = SlidecastTimingMode.Confirmed,
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
