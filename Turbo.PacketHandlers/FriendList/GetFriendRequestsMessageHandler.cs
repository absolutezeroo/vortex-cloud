using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.FriendList.Grains;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Snapshots.FriendList;

namespace Turbo.PacketHandlers.FriendList;

public class GetFriendRequestsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetFriendRequestsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetFriendRequestsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        List<FriendRequestSnapshot> requests = await grain
            .GetFriendRequestsAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new FriendRequestsMessageComposer { Requests = requests }, ct)
            .ConfigureAwait(false);
    }
}
