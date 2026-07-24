using System.Collections.Generic;

namespace Vortex.Primitives.Rooms.Events;

/// <summary>
/// Raised by the send-signal wired action. Receive-signal triggers fire on it, and the furni/user ids
/// it carries become the SignalItems / SignalUsers sources for the stack it fires. The send-signal
/// "split" options emit one event per furni and/or per user instead of a single batched one.
/// </summary>
public sealed record SignalRoomEvent : RoomEvent
{
    public IReadOnlyList<int> FurniIds { get; init; } = [];
    public IReadOnlyList<int> PlayerIds { get; init; } = [];
}
