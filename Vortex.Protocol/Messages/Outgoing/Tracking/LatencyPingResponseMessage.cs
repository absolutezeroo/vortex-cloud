using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Tracking;

public sealed record LatencyPingResponseMessage : IComposer
{
    public int RequestId { get; init; }
}
