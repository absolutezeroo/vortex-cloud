namespace Vortex.Primitives.Rooms.Events.Player;

/// <summary>
/// A contract transaction that did not go through.
/// </summary>
/// <remarks>
/// Cancelled by a box, cancelled by the player, or timed out -- the client's trigger does not
/// distinguish, so neither does this.
/// </remarks>
public sealed record WiredTransactionFailedEvent : PlayerEvent;
