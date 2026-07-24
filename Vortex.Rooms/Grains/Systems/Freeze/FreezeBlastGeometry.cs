using System.Collections.Generic;

namespace Vortex.Rooms.Grains.Systems.Freeze;

/// <summary>
/// Pure geometry of a snowball blast: the tiles it covers given the centre, the boost radius and
/// whether it was an X-blast. The centre plus one arm of <c>radius + 1</c> tiles along each of the four
/// cardinal directions, and — for an X-blast — the four diagonals too. No room state, so it is
/// unit-tested directly; <see cref="RoomFreezeSystem"/> filters these tiles down to the ones that are
/// actually arena tiles.
/// </summary>
public static class FreezeBlastGeometry
{
    // Cardinal then diagonal, matching the room's Rotation order (N, E, S, W, NE, SE, SW, NW).
    private static readonly (int Dx, int Dy)[] Cardinal = [(0, -1), (1, 0), (0, 1), (-1, 0)];
    private static readonly (int Dx, int Dy)[] Diagonal = [(1, -1), (1, 1), (-1, 1), (-1, -1)];

    public static IEnumerable<(int X, int Y)> AffectedTiles(int x, int y, int radius, bool diagonal)
    {
        yield return (x, y);

        foreach ((int dx, int dy) in Cardinal)
        {
            for (int step = 1; step <= radius + 1; step++)
            {
                yield return (x + (dx * step), y + (dy * step));
            }
        }

        if (!diagonal)
        {
            yield break;
        }

        foreach ((int dx, int dy) in Diagonal)
        {
            for (int step = 1; step <= radius + 1; step++)
            {
                yield return (x + (dx * step), y + (dy * step));
            }
        }
    }
}
