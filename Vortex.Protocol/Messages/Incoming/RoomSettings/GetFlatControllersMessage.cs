using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Protocol.Messages.Incoming.RoomSettings;

public record GetFlatControllersMessage : IMessageEvent
{
    public RoomId RoomId { get; init; }
}
