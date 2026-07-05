using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Badges;
using Turbo.Primitives.Messages.Outgoing.Inventory.Badges;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Inventory.Badges;

/// <summary>
/// No achievement/progress-tracking system exists yet (deliberately deferred), so a requested badge
/// can never actually be fulfilled -- reporting Fulfilled = false is the correct, truthful answer,
/// not a stub.
/// </summary>
public class RequestABadgeMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RequestABadgeMessage>
{
    public async ValueTask HandleAsync(
        RequestABadgeMessage message,
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
