using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Navigator;

public record PopularRoomsSearchMessage : IMessageEvent
{
    public required string Query { get; init; }
    public int AdIndex { get; init; }
}
