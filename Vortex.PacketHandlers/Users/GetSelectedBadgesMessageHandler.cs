using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.PacketHandlers.Users;

public class GetSelectedBadgesMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetSelectedBadgesMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetSelectedBadgesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.UserId <= 0)
        {
            return;
        }

        ImmutableArray<PlayerBadgeSnapshot> badges = await _grainFactory
            .GetPlayerBadgeGrain(message.UserId)
            .GetBadgesAsync(ct)
            .ConfigureAwait(false);

        // Only badges equipped in a display slot (1..5) appear on a profile; unequipped badges have
        // slot 0.
        ImmutableArray<PlayerBadgeSnapshot> selected = badges
            .Where(b => b.SlotId > 0)
            .OrderBy(b => b.SlotId)
            .ToImmutableArray();

        await ctx.SendComposerAsync(
                new HabboUserBadgesMessageComposer { UserId = message.UserId, Badges = selected },
                ct
            )
            .ConfigureAwait(false);
    }
}
