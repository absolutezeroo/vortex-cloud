using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Incoming.Room.Session;

public record OpenFlatConnectionMessage : IMessageEvent
{
    public RoomId RoomId { get; init; }
    public string Password { get; init; } = string.Empty;
    public int Unknown1 { get; init; }
}
