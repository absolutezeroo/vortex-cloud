using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.NewNavigator;

public record NavigatorDeleteSavedSearchMessage : IMessageEvent
{
    public int SearchId { get; init; }
}
