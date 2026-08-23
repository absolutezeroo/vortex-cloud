using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Tracking;

public record LatencyPingRequestMessage : IMessageEvent
{
    public int RequestId { get; init; }
}
