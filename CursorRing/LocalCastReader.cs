using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace CursorRing;

internal static class LocalCastReader
{
    internal static unsafe CastSample Read(bool resolveRecastGroups)
    {
        try
        {
            var player = Control.GetLocalPlayer();
            var cast = player is null ? null : player->GetCastInfo();
            if (cast is null)
            {
                return CastSample.Inactive;
            }

            if (!cast->IsCasting || !resolveRecastGroups)
            {
                return new CastSample(
                    cast->IsCasting,
                    cast->SourceSequence,
                    cast->CurrentCastTime,
                    cast->TotalCastTime,
                    cast->ResponseSourceSequence);
            }

            var manager = ActionManager.Instance();
            var recastGroup = manager is null ? -1 : manager->GetRecastGroup(cast->ActionType, cast->ActionId);
            var additionalRecastGroup = manager is null
                ? -1
                : manager->GetAdditionalRecastGroup((ActionType)cast->ActionType, cast->ActionId);
            return new CastSample(
                cast->IsCasting,
                cast->SourceSequence,
                cast->CurrentCastTime,
                cast->TotalCastTime,
                cast->ResponseSourceSequence,
                recastGroup,
                additionalRecastGroup);
        }
        catch
        {
            return CastSample.Inactive;
        }
    }
}
