using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.PacketHandlers.FriendList;

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
