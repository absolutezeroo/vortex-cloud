namespace Vortex.Primitives.Rooms.Events.Player;

/// <summary>
/// Raised when a player speaks publicly in a room (normal talk / shout, not whispers). Carries the
/// spoken text so wired "avatar says (keyword)" triggers can match against it.
/// </summary>
public sealed record PlayerChatEvent : PlayerEvent
{
    public required string Message { get; init; }
}
