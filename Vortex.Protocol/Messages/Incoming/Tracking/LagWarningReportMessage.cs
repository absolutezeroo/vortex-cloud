using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Tracking;

public record LagWarningReportMessage : IMessageEvent
{
    public int WarningCount { get; init; }
}
