namespace Vortex.Primitives.Rooms;

/// <summary>
/// Config keys and defaults for room ownership limits, served live from <c>IServerConfigGrain</c>.
/// The default is the fallback when a key has no admin override stored in the DB.
/// </summary>
/// <remarks>
/// Here rather than beside the handlers that read it, because the limit is not a packet-layer
/// concern: the navigator's "can I create a room" screen asks the question, and the room service is
/// what has to answer it when the room is actually created. It lived only where the screen could see
/// it, and the creation path enforced nothing at all as a result.
/// </remarks>
public static class RoomsConfig
{
    /// <summary>Also doubles as the cap for "list all my rooms" dialogs: a player can never have
    /// more rooms than this, so a page listing them needs no separate limit.</summary>
    public const string MaxRoomsPerPlayerKey = "rooms.max_rooms_per_player";
    public const int MaxRoomsPerPlayerDefault = 50;

    /// <summary>Ceiling on the population limit a room may be created with. The client's own dialog
    /// offers a short list topping out well below this; the packet carries a raw int.</summary>
    public const int MaxPlayersCeiling = 250;
}
