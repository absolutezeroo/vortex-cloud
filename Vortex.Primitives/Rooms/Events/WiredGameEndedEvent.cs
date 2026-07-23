namespace Vortex.Primitives.Rooms.Events;

/// <summary>Raised when the room's wired game transitions to stopped (its counter is stopped/reset or
/// runs out). Drives the wired GAME_ENDS trigger.</summary>
public sealed record WiredGameEndedEvent : RoomEvent;
