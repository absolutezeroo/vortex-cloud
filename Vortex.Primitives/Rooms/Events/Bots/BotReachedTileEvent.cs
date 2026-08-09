using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Events.Bots;

/// <summary>
/// A bot finished a step onto a tile. The "bot reaches furni" wired listens for this and decides
/// for itself whether the tile is one it cares about, because the room does not know which furni a
/// given wired stack has selected.
/// </summary>
public sealed record BotReachedTileEvent : RoomEvent
{
    public required int BotId { get; init; }

    /// <summary>Carried along because wired addresses bots by name rather than by id.</summary>
    public required string BotName { get; init; }

    public required RoomObjectId ObjectId { get; init; }

    public required int TileIdx { get; init; }
}
