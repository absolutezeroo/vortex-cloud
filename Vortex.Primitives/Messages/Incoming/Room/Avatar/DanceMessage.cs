using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Avatar;

public record DanceMessage : IMessageEvent
{
    public int DanceId { get; init; }
}
