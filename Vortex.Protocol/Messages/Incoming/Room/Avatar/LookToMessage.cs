using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Avatar;

public record LookToMessage : IMessageEvent
{
    public required int X { get; init; }
    public required int Y { get; init; }
}
