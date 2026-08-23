using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Navigator;

public record CancelEventMessage : IMessageEvent
{
    public int AdvertisementId { get; init; }
}
