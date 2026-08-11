namespace CursorRing.Tests;

public sealed class VisibilityRulesTests
{
    [Theory]
    [InlineData(VisibilityMode.Always, false, false, true)]
    [InlineData(VisibilityMode.CombatOnly, true, false, true)]
    [InlineData(VisibilityMode.CombatOnly, false, true, false)]
    [InlineData(VisibilityMode.DutyOnly, false, true, true)]
    [InlineData(VisibilityMode.DutyOnly, true, false, false)]
    [InlineData(VisibilityMode.DutyCombat, true, true, true)]
    [InlineData(VisibilityMode.DutyCombat, true, false, false)]
    [InlineData(VisibilityMode.DutyCombat, false, true, false)]
    [InlineData(VisibilityMode.CombatOrDuty, true, false, true)]
    [InlineData(VisibilityMode.CombatOrDuty, false, true, true)]
    [InlineData(VisibilityMode.CombatOrDuty, false, false, false)]
    public void PresetsMatchExpectedState(VisibilityMode mode, bool inCombat, bool inDuty, bool expected)
    {
        var settings = new CursorSettings { Visibility = mode };
        Assert.Equal(expected, VisibilityRules.IsVisible(settings, inCombat, inDuty, false));
    }

    [Fact]
    public void MouseLookCombatOnlyNarrowsBaseVisibility()
    {
        var settings = new CursorSettings { Visibility = VisibilityMode.DutyOnly, MouseLook = MouseLookVisibility.CombatOnly };
        Assert.False(VisibilityRules.IsVisible(settings, true, false, true));
        Assert.False(VisibilityRules.IsVisible(settings, false, true, true));
        Assert.True(VisibilityRules.IsVisible(settings, true, true, true));
    }

    [Fact]
    public void MouseLookCanFollowOrHide()
    {
        var settings = new CursorSettings { Visibility = VisibilityMode.Always };
        Assert.True(VisibilityRules.IsVisible(settings, false, false, true));
        settings.MouseLook = MouseLookVisibility.Hidden;
        Assert.False(VisibilityRules.IsVisible(settings, true, true, true));
    }
}
