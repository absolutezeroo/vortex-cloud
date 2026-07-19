using System.Collections.Generic;

namespace Vortex.Primitives.Rooms.Events;

public sealed record WiredVariableBoxChangedEvent : RoomEvent
{
    public required List<int> BoxIds { get; init; }
}
