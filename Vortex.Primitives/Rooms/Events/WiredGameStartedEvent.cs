namespace Vortex.Primitives.Rooms.Events;

/// <summary>Raised when the room's wired game transitions to running (e.g. its counter is started).
/// Drives the wired GAME_STARTS trigger.</summary>
public sealed record WiredGameStartedEvent : RoomEvent;
