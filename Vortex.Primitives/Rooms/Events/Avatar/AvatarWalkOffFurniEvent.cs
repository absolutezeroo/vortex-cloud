using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Events.Avatar;

public sealed record AvatarWalkOffFurniEvent : AvatarEvent
{
    public required RoomObjectId FurniId { get; init; }
}
