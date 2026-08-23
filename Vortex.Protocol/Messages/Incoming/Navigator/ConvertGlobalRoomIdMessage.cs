using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Navigator;

public record ConvertGlobalRoomIdMessage : IMessageEvent
{
    public required string FlatId { get; init; }
}
