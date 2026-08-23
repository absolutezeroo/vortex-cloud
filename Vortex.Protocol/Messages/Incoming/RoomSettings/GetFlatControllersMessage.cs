using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Incoming.RoomSettings;

public record GetFlatControllersMessage : IMessageEvent
{
    public RoomId RoomId { get; init; }
}
