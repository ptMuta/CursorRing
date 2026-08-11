namespace CursorRing;

internal static class VisibilityRules
{
    internal static bool IsVisible(CursorSettings settings, bool inCombat, bool inDuty, bool mouseLook)
    {
        var visible = settings.Visibility switch
        {
            VisibilityMode.Always => true,
            VisibilityMode.CombatOnly => inCombat,
            VisibilityMode.DutyOnly => inDuty,
            VisibilityMode.DutyCombat => inDuty && inCombat,
            VisibilityMode.CombatOrDuty => inCombat || inDuty,
            _ => false
        };
        if (!mouseLook)
        {
            return visible;
        }
        return settings.MouseLook switch
        {
            MouseLookVisibility.FollowVisibility => visible,
            MouseLookVisibility.CombatOnly => visible && inCombat,
            _ => false
        };
    }
}
