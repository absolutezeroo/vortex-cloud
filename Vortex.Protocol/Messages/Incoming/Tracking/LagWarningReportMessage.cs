using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Tracking;

public record LagWarningReportMessage : IMessageEvent
{
    public int WarningCount { get; init; }
}
