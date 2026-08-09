using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Events.Bots;

/// <summary>
/// A bot stepped up beside somebody — what the "bot reaches habbo" wired fires on. The player it
/// reached is also the event's cause, so a stack can go on to act on them as its triggered user.
/// </summary>
public sealed record BotReachedAvatarEvent : RoomEvent
{
    public required int BotId { get; init; }

    /// <summary>Carried along because wired addresses bots by name rather than by id.</summary>
    public required string BotName { get; init; }

    public required RoomObjectId ObjectId { get; init; }

    public required PlayerId ReachedPlayerId { get; init; }
}
