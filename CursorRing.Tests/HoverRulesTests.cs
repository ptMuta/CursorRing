namespace CursorRing.Tests;

public sealed class HoverRulesTests
{
    [Theory]
    [InlineData(HoverVisibilityMode.WheneverVisible, false, true)]
    [InlineData(HoverVisibilityMode.WheneverVisible, true, true)]
    [InlineData(HoverVisibilityMode.OutOfCombatOnly, false, true)]
    [InlineData(HoverVisibilityMode.OutOfCombatOnly, true, false)]
    [InlineData(HoverVisibilityMode.InCombatOnly, false, false)]
    [InlineData(HoverVisibilityMode.InCombatOnly, true, true)]
    public void ModesMatchCombatState(HoverVisibilityMode mode, bool inCombat, bool expected)
    {
        var settings = new CursorSettings { HoverVisibility = mode };

        Assert.Equal(expected, HoverRules.IsEnabled(settings, inCombat, false));
    }

    [Fact]
    public void DisabledAndMouseLookStatesSuppressHover()
    {
        var settings = new CursorSettings { ShowHoverIndicator = false };

        Assert.False(HoverRules.IsEnabled(settings, false, false));
        settings.ShowHoverIndicator = true;
        Assert.False(HoverRules.IsEnabled(settings, false, true));
    }
}
