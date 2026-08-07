using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;

namespace Vortex.PacketHandlers.Help;

/// <summary>The guide marking the session finished, which is what asks the requester to rate it.</summary>
public class GuideSessionResolvedMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GuideSessionResolvedMessage>
{
    public async ValueTask HandleAsync(
        GuideSessionResolvedMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await GuideSessionEnd
            .CloseAsync(grainFactory, ctx.PlayerId, GuideSessionEnd.ReasonGuideResolved, ct)
            .ConfigureAwait(false);
}
