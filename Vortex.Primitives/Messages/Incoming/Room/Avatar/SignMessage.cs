using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Avatar;

public record SignMessage : IMessageEvent
{
    public required int SignId { get; init; }
}
