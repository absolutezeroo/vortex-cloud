namespace Vortex.PacketHandlers.Configuration;

/// <summary>
/// Config keys and defaults for room ownership limits, served live from <c>IServerConfigGrain</c>.
/// The default is the fallback when a key has no admin override stored in the DB.
/// </summary>
public static class RoomsConfig
{
    /// <summary>Also doubles as the cap for "list all my rooms" dialogs: a player can never have
    /// more rooms than this, so a page listing them needs no separate limit.</summary>
    public const string MaxRoomsPerPlayerKey = "rooms.max_rooms_per_player";
    public const int MaxRoomsPerPlayerDefault = 50;
}
