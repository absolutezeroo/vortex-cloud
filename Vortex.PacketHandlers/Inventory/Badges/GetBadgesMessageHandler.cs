using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Protocol.Messages.Incoming.Inventory.Badges;
using Vortex.Protocol.Messages.Outgoing.Inventory.Badges;
using Vortex.Protocol.Messages.Outgoing.Users;

namespace Vortex.PacketHandlers.Inventory.Badges;

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

        // BadgesEvent cannot say which badges are worn: WIN63's parser
        // (unknowns/_SafePkg_3206/_SafeCls_3564.as) reads its first int as the unseen-tracker
        // badgeId, and BadgesModel.initBadges() builds every badge inactive. HabboUserBadges (1292)
        // is the only thing that ever calls startWearingBadge(), and the client asks for it just
        // once per user, when someone opens that user's infostand -- so until the player clicked
        // their own avatar in a room, their inventory showed no worn badge at all. The real server
        // pushes it unsolicited; sending it with the badge list is the same moment the client is
        // asking about its own badges.
        ImmutableArray<PlayerBadgeSnapshot> worn = badges
            .Where(b => b.SlotId > 0)
            .OrderBy(b => b.SlotId)
            .ToImmutableArray();

        await ctx.SendComposerAsync(
                new HabboUserBadgesMessageComposer { UserId = ctx.PlayerId, Badges = worn },
                ct
            )
            .ConfigureAwait(false);
    }
}
