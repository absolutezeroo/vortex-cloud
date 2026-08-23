using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Navigator;

public record ForwardToARandomPromotedRoomMessage : IMessageEvent
{
    public required string Category { get; init; }
}
