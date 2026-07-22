using Vortex.Primitives.Players;

namespace Vortex.Primitives.Rooms.Events.Player;

/// <summary>
/// Raised when a player clicks another player's avatar. <c>CausedBy</c> / <see cref="PlayerEvent.PlayerId"/>
/// is the clicker — the triggering user — while <see cref="TargetPlayerId"/> is the clicked avatar.
/// Drives the wired "avatar clicks avatar" (USER_CLICKS_USER) trigger.
/// </summary>
public sealed record PlayerClickedPlayerEvent : PlayerEvent
{
    public required PlayerId TargetPlayerId { get; init; }
}
