using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Badges;
using Vortex.Primitives.Messages.Outgoing.Inventory.Badges;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Inventory.Badges;

/// <summary>See RequestABadgeMessageHandler -- same truthful "never fulfilled" answer, since no
/// achievement/progress-tracking system exists yet.</summary>
public class GetIsBadgeRequestFulfilledMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetIsBadgeRequestFulfilledMessage>
{
    public async ValueTask HandleAsync(
        GetIsBadgeRequestFulfilledMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || string.IsNullOrEmpty(message.RequestCode))
        {
            return;
        }

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new IsBadgeRequestFulfilledEventMessageComposer
                {
                    RequestCode = message.RequestCode,
                    Fulfilled = false,
                }
            )
            .ConfigureAwait(false);
    }
}
