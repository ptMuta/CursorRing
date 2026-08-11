using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace CursorRing;

internal static class HoveredEntityReader
{
    internal static unsafe bool Read()
    {
        try
        {
            var targets = TargetSystem.Instance();
            return targets is not null
                && (targets->MouseOverTarget is not null || targets->MouseOverNameplateTarget is not null);
        }
        catch
        {
            return false;
        }
    }
}
