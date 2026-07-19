using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record RateFlatMessage : IMessageEvent
{
    public int Points { get; init; }
}
