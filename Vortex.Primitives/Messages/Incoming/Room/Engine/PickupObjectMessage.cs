using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

public record PickupObjectMessage : IMessageEvent
{
    public required int CategoryId { get; init; }
    public required RoomObjectId ObjectId { get; init; }
    public required bool Confirm { get; init; }
}
