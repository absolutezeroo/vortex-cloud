using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.NewNavigator;

public record NewNavigatorSearchMessage : IMessageEvent
{
    public required string SearchCodeOriginal { get; init; }
    public required string FilteringData { get; init; }
}
