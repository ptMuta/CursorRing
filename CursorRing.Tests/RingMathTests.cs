using System.Numerics;

namespace CursorRing.Tests;

public sealed class RingMathTests
{
    [Theory]
    [InlineData(72f, 56.5f, 72f, 56f)]
    [InlineData(72.49f, 56.51f, 72f, 57f)]
    [InlineData(72.5f, 57.5f, 72f, 58f)]
    public void CenterSnapsToWholePixels(float x, float y, float expectedX, float expectedY)
    {
        Assert.Equal(new Vector2(expectedX, expectedY), RingMath.SnapCenter(new Vector2(x, y)));
    }

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

        Assert.True(geometry.Inner - (geometry.InnerThickness / 2f) - geometry.InnerBorderThickness >= 0f);
        Assert.True(geometry.Inner + (geometry.InnerThickness / 2f) + geometry.InnerBorderThickness <= geometry.Main - (settings.RingThickness / 2f));
        Assert.True(geometry.Outer > geometry.Main);
        Assert.True(geometry.Pie > 0f);
    }

    [Fact]
    public void OutlinesPreserveConfiguredRingSpacing()
    {
        var settings = new CursorSettings
        {
            RingDiameter = 80f,
            RingThickness = 4f,
            ShowRingBorder = true,
            RingBorderThickness = 3f,
            GcdThickness = 6f,
            GcdSpacing = 5f,
            ShowGcdBorder = true,
            GcdBorderThickness = 2f
        };

        var geometry = RingMath.GetGeometry(settings);
        var mainOuterEdge = geometry.Main + (settings.RingThickness / 2f) + settings.RingBorderThickness;
        var outerInnerEdge = geometry.Outer - (settings.GcdThickness / 2f) - settings.GcdBorderThickness;
        var mainInnerEdge = geometry.Main - (settings.RingThickness / 2f) - settings.RingBorderThickness;
        var innerOuterEdge = geometry.Inner + (geometry.InnerThickness / 2f) + geometry.InnerBorderThickness;

        Assert.Equal(settings.GcdSpacing, outerInnerEdge - mainOuterEdge, 5);
        Assert.Equal(settings.GcdSpacing, mainInnerEdge - innerOuterEdge, 5);
    }

    [Fact]
    public void OversizedInnerOutlineIsClampedInsideMainRing()
    {
        var settings = new CursorSettings
        {
            RingDiameter = 8f,
            RingThickness = 4f,
            GcdThickness = 20f,
            ShowGcdBorder = true,
            GcdBorderThickness = 20f
        };

        var geometry = RingMath.GetGeometry(settings);

        Assert.True(geometry.Inner - (geometry.InnerThickness / 2f) - geometry.InnerBorderThickness >= 0f);
        Assert.True(geometry.Inner + (geometry.InnerThickness / 2f) + geometry.InnerBorderThickness <= geometry.Pie);
        Assert.True(geometry.InnerBorderThickness < settings.GcdBorderThickness);
    }

    [Theory]
    [InlineData(ProgressBehavior.Fill, 0f, 0.4f)]
    [InlineData(ProgressBehavior.Drain, 0.4f, 1f)]
    public void VisibleRangeMatchesProgressBehavior(ProgressBehavior behavior, float expectedStart, float expectedEnd)
    {
        var range = RingMath.GetVisibleRange(0.4f, behavior);

        Assert.Equal(expectedStart, range.Start);
        Assert.Equal(expectedEnd, range.End);
    }

    [Fact]
    public void SegmentRangesAreClippedToVisibleProgress()
    {
        var visible = new ProgressRange(0.4f, 1f);

        var cast = RingMath.Intersect(visible, new ProgressRange(0f, 0.55f));
        var slidecast = RingMath.Intersect(visible, new ProgressRange(0.55f, 0.75f));
        var recovery = RingMath.Intersect(visible, new ProgressRange(0.75f, 1f));

        Assert.Equal(new ProgressRange(0.4f, 0.55f), cast);
        Assert.Equal(new ProgressRange(0.55f, 0.75f), slidecast);
        Assert.Equal(new ProgressRange(0.75f, 1f), recovery);
    }

    [Fact]
    public void DisjointIntersectionIsNotVisible()
    {
        var range = RingMath.Intersect(new ProgressRange(0f, 0.2f), new ProgressRange(0.5f, 0.8f));

        Assert.False(range.IsVisible);
    }

    [Fact]
    public void ArbitraryRangeMapsCounterclockwise()
    {
        var arc = RingMath.GetArc(0.25f, 0.75f, RotationDirection.Counterclockwise);

        Assert.Equal(RingMath.Top - (RingMath.FullTurn * 0.25f), arc.Start, 5);
        Assert.Equal(RingMath.Top - (RingMath.FullTurn * 0.75f), arc.End, 5);
    }

    [Fact]
    public void OverlayOutlinesCannotCrossCenter()
    {
        Assert.Equal(2f, RingMath.ClampStrokeBorder(4f, 4f, 20f));
        Assert.Equal(3.5f, RingMath.ClampPieBorder(4f, 20f));
        Assert.Equal(0f, RingMath.ClampStrokeBorder(1f, 4f, 20f));
    }
}
