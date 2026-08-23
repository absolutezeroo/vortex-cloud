using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.NewNavigator;

public record NavigatorDeleteSavedSearchMessage : IMessageEvent
{
    public int SearchId { get; init; }
}
