using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Events.RoomObject;

public abstract record RoomObjectEvent : RoomEvent
{
    public required RoomObjectId ObjectId { get; init; }
}
