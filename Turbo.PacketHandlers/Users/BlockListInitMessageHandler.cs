using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.FriendList.Grains;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Users;

public class BlockListInitMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<BlockListInitMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        BlockListInitMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        List<int> blockedIds = await grain.GetBlockedUserIdsAsync(ct).ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new BlockListMessageComposer { BlockedPlayerIds = blockedIds },
                ct
            )
            .ConfigureAwait(false);
    }
}
