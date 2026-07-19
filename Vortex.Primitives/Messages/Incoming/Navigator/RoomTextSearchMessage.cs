using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record RoomTextSearchMessage : IMessageEvent
{
    public string? Query { get; init; }
}
