using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record ConvertGlobalRoomIdMessage : IMessageEvent
{
    public required string FlatId { get; init; }
}
