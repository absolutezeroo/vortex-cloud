using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.PacketHandlers.FriendList;

public class RemoveFriendMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RemoveFriendMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RemoveFriendMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.FriendIds.Count == 0)
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);

        List<int> removed = await grain
            .RemoveFriendsAsync(message.FriendIds, ct)
            .ConfigureAwait(false);

        if (removed.Count == 0)
        {
            return;
        }

        List<FriendListUpdateSnapshot> updates = new List<FriendListUpdateSnapshot>();
        foreach (int friendId in removed)
        {
            updates.Add(
                new FriendListUpdateSnapshot
                {
                    ActionType = FriendListUpdateActionType.Removed,
                    FriendId = friendId,
                    Friend = null,
                }
            );
        }

        await ctx.SendComposerAsync(
                new FriendListUpdateMessageComposer { FriendCategories = [], Updates = updates },
                ct
            )
            .ConfigureAwait(false);
    }
}
