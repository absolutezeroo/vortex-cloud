using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

public class GetHabboGroupBadgesMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetHabboGroupBadgesMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetHabboGroupBadgesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        List<GroupBadgeSnapshot> badges = await _grainFactory
            .GetGroupDirectoryGrain()
            .GetBadgesAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new HabboGroupBadgesMessageComposer { Badges = badges }, ct)
            .ConfigureAwait(false);
    }
}
