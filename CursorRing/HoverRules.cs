namespace CursorRing;

internal static class HoverRules
{
    internal static bool IsEnabled(CursorSettings settings, bool inCombat, bool mouseLook)
    {
        if (!settings.ShowHoverIndicator || mouseLook)
        {
            return false;
        }

        return settings.HoverVisibility switch
        {
            HoverVisibilityMode.WheneverVisible => true,
            HoverVisibilityMode.OutOfCombatOnly => !inCombat,
            HoverVisibilityMode.InCombatOnly => inCombat,
            _ => false
        };
    }
}
