using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Incoming.Room.Furniture;

public record SetMannequinNameMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
    public required string Name { get; init; }
}
