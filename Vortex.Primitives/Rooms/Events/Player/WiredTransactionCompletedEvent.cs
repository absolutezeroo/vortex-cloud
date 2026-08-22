namespace Vortex.Primitives.Rooms.Events.Player;

/// <summary>A contract transaction that went through, for the wired boxes that wait on one.</summary>
public sealed record WiredTransactionCompletedEvent : PlayerEvent;
