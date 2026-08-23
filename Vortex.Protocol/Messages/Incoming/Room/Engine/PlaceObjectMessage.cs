using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Engine;

public record PlaceObjectMessage : IMessageEvent
{
    public required string Data { get; init; }
}
