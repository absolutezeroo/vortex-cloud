using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Navigator;

public record RateFlatMessage : IMessageEvent
{
    public int Points { get; init; }
}
