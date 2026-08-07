using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Help;
using Vortex.Primitives.Help.Grains;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// A player asking a guide for help. The request is offered to one on-duty guide, or refused
/// outright when nobody is covering that queue.
/// </summary>
public class GuideSessionCreateMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GuideSessionCreateMessage>
{
    public async ValueTask HandleAsync(
        GuideSessionCreateMessage message,
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
            .CreateRequestAsync(ctx.PlayerId, message.HelpRequestType, message.Description, ct)
            .ConfigureAwait(false);

        await GuideSessionDispatch.DeliverAsync(grainFactory, outcome, ct).ConfigureAwait(false);
    }
}
