using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Avatar;

public record SignMessage : IMessageEvent
{
    public required int SignId { get; init; }
}
