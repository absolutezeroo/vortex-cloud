using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Tracking;

namespace Vortex.PacketHandlers.Tracking;

public class LagWarningReportMessageHandler : IMessageHandler<LagWarningReportMessage>
{
    public async ValueTask HandleAsync(
        LagWarningReportMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
