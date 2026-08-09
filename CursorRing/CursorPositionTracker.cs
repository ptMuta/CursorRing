using System.Numerics;

namespace CursorRing;

internal sealed class CursorPositionTracker
{
    private Vector2 lastPosition;
    private bool hasLastPosition;

    internal bool TryResolve(Vector2 mouse, Vector2 minimum, Vector2 maximum, bool captured, out Vector2 position)
    {
        if (!captured)
        {
            if (!IsInside(mouse, minimum, maximum))
            {
                position = default;
                return false;
            }

            lastPosition = mouse;
            hasLastPosition = true;
            position = mouse;
            return true;
        }

        position = hasLastPosition && IsInside(lastPosition, minimum, maximum)
            ? lastPosition
            : minimum + ((maximum - minimum) / 2f);
        return true;
    }

    private static bool IsInside(Vector2 position, Vector2 minimum, Vector2 maximum)
    {
        return float.IsFinite(position.X)
            && float.IsFinite(position.Y)
            && position.X >= minimum.X
            && position.Y >= minimum.Y
            && position.X < maximum.X
            && position.Y < maximum.Y;
    }
}
