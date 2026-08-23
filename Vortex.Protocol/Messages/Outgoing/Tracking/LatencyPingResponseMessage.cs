using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Tracking;

public sealed record LatencyPingResponseMessage : IComposer
{
    public int RequestId { get; init; }
}
