using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Help;
using Vortex.Primitives.Help.Grains;
using Vortex.Protocol.Messages.Incoming.Help;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// A guide taking the request in front of them, or passing it on. Declining is not the end of the
/// request: it moves to the next guide who has not already seen it.
/// </summary>
public class GuideSessionGuideDecidesMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GuideSessionGuideDecidesMessage>
{
    public async ValueTask HandleAsync(
        GuideSessionGuideDecidesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        GuideRequestOutcome outcome = await grainFactory
            .GetGuideDirectoryGrain()
            .GuideDecidesAsync(ctx.PlayerId, message.Accepted, ct)
            .ConfigureAwait(false);

        await GuideSessionDispatch.DeliverAsync(grainFactory, outcome, ct).ConfigureAwait(false);
    }
}
