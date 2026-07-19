using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record RoomsWithHighestScoreSearchMessage : IMessageEvent
{
    public int AdIndex { get; init; }
}
