using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Navigator;

public record RoomsWithHighestScoreSearchMessage : IMessageEvent
{
    public int AdIndex { get; init; }
}
