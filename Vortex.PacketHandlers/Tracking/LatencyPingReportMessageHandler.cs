using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Tracking;

namespace Vortex.PacketHandlers.Tracking;

public class LatencyPingReportMessageHandler : IMessageHandler<LatencyPingReportMessage>
{
    public async ValueTask HandleAsync(
        LatencyPingReportMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
