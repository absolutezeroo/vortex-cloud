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
