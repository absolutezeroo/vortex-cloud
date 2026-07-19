using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Tracking;
using Vortex.Primitives.Messages.Outgoing.Tracking;

namespace Vortex.PacketHandlers.Tracking;

public class LatencyPingRequestMessageHandler : IMessageHandler<LatencyPingRequestMessage>
{
    public async ValueTask HandleAsync(
        LatencyPingRequestMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ctx.SendComposerAsync(
                new LatencyPingResponseMessage { RequestId = message.RequestId },
                ct
            )
            .ConfigureAwait(false);
    }
}
