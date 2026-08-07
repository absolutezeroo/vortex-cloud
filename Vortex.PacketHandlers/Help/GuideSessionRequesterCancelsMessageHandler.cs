using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// The requester giving up. Also clears a request that never found a guide, so an offer does not sit
/// in front of a guide for somebody who has walked away.
/// </summary>
public class GuideSessionRequesterCancelsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GuideSessionRequesterCancelsMessage>
{
    public async ValueTask HandleAsync(
        GuideSessionRequesterCancelsMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await GuideSessionEnd
            .CloseAsync(grainFactory, ctx.PlayerId, GuideSessionEnd.ReasonRequesterCancelled, ct)
            .ConfigureAwait(false);
}
