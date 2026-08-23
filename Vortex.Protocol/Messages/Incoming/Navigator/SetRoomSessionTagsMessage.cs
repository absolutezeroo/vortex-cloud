using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Navigator;

public record SetRoomSessionTagsMessage : IMessageEvent
{
    public string? Tag1 { get; init; }
    public string? Tag2 { get; init; }
}
