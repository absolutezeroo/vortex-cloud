using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Badges;
using Turbo.Primitives.Messages.Outgoing.Inventory.Badges;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Snapshots;

namespace Turbo.PacketHandlers.Inventory.Badges;

public class GetBadgesMessageHandler(IGrainFactory grainFactory) : IMessageHandler<GetBadgesMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetBadgesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ImmutableArray<PlayerBadgeSnapshot> badges = await _grainFactory
            .GetPlayerBadgeGrain(ctx.PlayerId)
            .GetBadgesAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new BadgesEventMessageComposer { Badges = badges }, ct)
            .ConfigureAwait(false);
    }
}
