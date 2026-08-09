namespace CursorRing.Tests;

public sealed class RingMathTests
{
    [Fact]
    public void ClockwiseFillStartsAtTopAndGrows()
    {
        var arc = RingMath.GetArc(0.25f, ProgressBehavior.Fill, RotationDirection.Clockwise);

        Assert.Equal(RingMath.Top, arc.Start, 5);
        Assert.Equal(RingMath.Top + (RingMath.FullTurn * 0.25f), arc.End, 5);
    }

    [Fact]
    public void CounterclockwiseDrainRemovesElapsedRegion()
    {
        var arc = RingMath.GetArc(0.25f, ProgressBehavior.Drain, RotationDirection.Counterclockwise);

        Assert.Equal(RingMath.Top - (RingMath.FullTurn * 0.25f), arc.Start, 5);
        Assert.Equal(RingMath.Top - RingMath.FullTurn, arc.End, 5);
    }

    [Fact]
    public void ClockwiseDrainRemovesElapsedRegion()
    {
        var arc = RingMath.GetArc(0.25f, ProgressBehavior.Drain, RotationDirection.Clockwise);

        Assert.Equal(RingMath.Top + (RingMath.FullTurn * 0.25f), arc.Start, 5);
        Assert.Equal(RingMath.Top + RingMath.FullTurn, arc.End, 5);
    }

    [Fact]
    public void CounterclockwiseFillStartsAtTopAndGrows()
    {
        var arc = RingMath.GetArc(0.25f, ProgressBehavior.Fill, RotationDirection.Counterclockwise);

        Assert.Equal(RingMath.Top, arc.Start, 5);
        Assert.Equal(RingMath.Top - (RingMath.FullTurn * 0.25f), arc.End, 5);
    }

    [Fact]
    public void ArcProgressIsClamped()
    {
        var below = RingMath.GetArc(-1f, ProgressBehavior.Fill, RotationDirection.Clockwise);
        var above = RingMath.GetArc(2f, ProgressBehavior.Fill, RotationDirection.Clockwise);

        Assert.Equal(below.Start, below.End);
        Assert.Equal(RingMath.FullTurn, above.End - above.Start, 5);
    }

    [Fact]
    public void InnerRadiusNeverCrossesCenter()
    {
        var settings = new CursorSettings
        {
            RingDiameter = 8f,
            RingThickness = 4f,
            GcdThickness = 20f,
            GcdSpacing = 40f
        };

        var geometry = RingMath.GetGeometry(settings);

        Assert.True(geometry.Inner - (geometry.InnerThickness / 2f) >= 0f);
        Assert.True(geometry.Inner + (geometry.InnerThickness / 2f) <= geometry.Main - (settings.RingThickness / 2f));
        Assert.True(geometry.Outer > geometry.Main);
        Assert.True(geometry.Pie > 0f);
    }
}
