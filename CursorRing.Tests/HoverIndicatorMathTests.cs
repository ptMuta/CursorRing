namespace CursorRing.Tests;

public sealed class HoverIndicatorMathTests
{
    [Fact]
    public void GeometryStartsOutsideVisibleDot()
    {
        var settings = new CursorSettings
        {
            DotDiameter = 8f,
            ShowDotBorder = true,
            DotBorderThickness = 2f,
            HoverIndicatorOffset = 3f,
            HoverIndicatorSize = 6f,
            HoverIndicatorThickness = 1.5f
        };

        var geometry = HoverIndicatorMath.GetGeometry(settings);

        Assert.Equal(6f, geometry.DotExtent);
        Assert.Equal(9f, geometry.InnerRadius);
        Assert.Equal(15f, geometry.OuterRadius);
        Assert.Equal(6f, geometry.CaretHalfWidth);
        Assert.Equal(1.5f, geometry.Thickness);
    }

    [Fact]
    public void DisabledDotOutlineDoesNotAffectPosition()
    {
        var settings = new CursorSettings
        {
            DotDiameter = 8f,
            ShowDotBorder = false,
            DotBorderThickness = 20f,
            HoverIndicatorOffset = 2f
        };

        Assert.Equal(6f, HoverIndicatorMath.GetGeometry(settings).InnerRadius);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(45f, 0.7853982f)]
    [InlineData(-90f, -1.5707964f)]
    public void RotationConvertsToRadians(float degrees, float expected)
    {
        var settings = new CursorSettings { HoverIndicatorRotationDegrees = degrees };

        Assert.Equal(expected, HoverIndicatorMath.GetGeometry(settings).RotationRadians, 6);
    }

    [Fact]
    public void CornerBracketsReserveTheirDiagonalExtent()
    {
        var settings = new CursorSettings { HoverIndicatorStyle = HoverIndicatorStyle.CornerBrackets };
        var geometry = HoverIndicatorMath.GetGeometry(settings);

        Assert.Equal(MathF.Sqrt(2f) * geometry.OuterRadius, geometry.Extent, 6);
    }

    [Fact]
    public void CaretsUseSymmetricFortyFiveDegreeArms()
    {
        var settings = new CursorSettings
        {
            HoverIndicatorStyle = HoverIndicatorStyle.InwardCarets,
            HoverIndicatorSize = 8f
        };
        var geometry = HoverIndicatorMath.GetGeometry(settings);

        Assert.Equal(8f, geometry.OuterRadius - geometry.InnerRadius);
        Assert.Equal(8f, geometry.CaretHalfWidth);
    }
}
