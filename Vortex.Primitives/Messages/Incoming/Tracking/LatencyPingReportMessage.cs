using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Tracking;

public record LatencyPingReportMessage : IMessageEvent
{
    public int AverageLatency { get; init; }
    public int ValidPingAverage { get; init; }
    public int NumPings { get; init; }
}
