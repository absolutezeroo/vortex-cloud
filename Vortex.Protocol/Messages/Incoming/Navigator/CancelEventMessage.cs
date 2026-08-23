using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record CancelEventMessage : IMessageEvent
{
    public int AdvertisementId { get; init; }
}
