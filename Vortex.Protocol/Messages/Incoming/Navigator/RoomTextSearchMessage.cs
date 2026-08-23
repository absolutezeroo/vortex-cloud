using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Navigator;

public record RoomTextSearchMessage : IMessageEvent
{
    public string? Query { get; init; }
}
