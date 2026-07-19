using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

public class GetIgnoredUsersMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetIgnoredUsersMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetIgnoredUsersMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        List<int> ignoredIds = await grain.GetIgnoredUserIdsAsync(ct).ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new IgnoredUsersMessageComposer { IgnoredUserIds = ignoredIds },
                ct
            )
            .ConfigureAwait(false);
    }
}
