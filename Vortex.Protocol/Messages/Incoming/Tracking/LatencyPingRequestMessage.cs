using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Tracking;

public record LatencyPingRequestMessage : IMessageEvent
{
    public int RequestId { get; init; }
}
