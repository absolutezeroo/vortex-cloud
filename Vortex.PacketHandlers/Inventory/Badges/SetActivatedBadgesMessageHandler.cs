using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Badges;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Inventory.Badges;

public class SetActivatedBadgesMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetActivatedBadgesMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SetActivatedBadgesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetPlayerBadgeGrain(ctx.PlayerId)
            .SetActivatedBadgesAsync(message.Slots, ct)
            .ConfigureAwait(false);
    }
}
