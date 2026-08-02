using System.Collections.Generic;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Avatars;

namespace Vortex.Primitives.Rooms.Object;

/// <summary>
/// The slice of the room's map that object logic reads: tile arithmetic, walkability, and what is
/// standing or stacked on a tile. Not a grain contract, and synchronous throughout -- every answer
/// comes from live state already held by the activation the caller runs in.
/// </summary>
public interface IRoomMapAccess
{
    int Width { get; }
    int Height { get; }

    /// <summary>Number of tiles in the grid, and so the exclusive upper bound for every tile
    /// index taken by the members here.</summary>
    int TileCount { get; }

    /// <summary>Tile index for a coordinate pair, or a negative value when out of bounds.</summary>
    int ToIdx(int x, int y);

    /// <summary>The coordinate pair behind a tile index.</summary>
    (int x, int y) GetTileXY(int idx);

    /// <summary>Walking distance between two tile indices.</summary>
    int GetDistanceBetween(int a, int b);

    /// <summary>The unit step a rotation corresponds to.</summary>
    (int dx, int dy) GetDirectionOffset(Rotation dir);

    /// <summary>The tile one step along <paramref name="direction"/>, false if that leaves the map.</summary>
    bool TryGetTileInFront(int index, Rotation direction, out int nextIndex);

    /// <summary>Whether this avatar may step onto this tile.</summary>
    bool CanAvatarWalk(
        IRoomAvatar avatar,
        int tileIdx,
        bool isGoal = true,
        bool isDiagonalCheck = false
    );

    /// <summary>Flags on a tile. <see cref="RoomTileFlags.None"/> when out of range.</summary>
    RoomTileFlags TileFlagsAt(int tileIndex);

    /// <summary>Floor items stacked on a tile. Empty when the index is out of range.</summary>
    IReadOnlySet<RoomObjectId> FloorStackAt(int tileIndex);

    /// <summary>Avatars standing on a tile. Empty when the index is out of range.</summary>
    IReadOnlySet<RoomObjectId> AvatarStackAt(int tileIndex);
}
