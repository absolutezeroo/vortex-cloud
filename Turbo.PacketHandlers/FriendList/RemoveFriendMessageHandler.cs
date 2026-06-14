using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.FriendList.Enums;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Snapshots.FriendList;

namespace Turbo.PacketHandlers.FriendList;

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
            return;

        var grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);

        var removed = await grain.RemoveFriendsAsync(message.FriendIds, ct).ConfigureAwait(false);

        if (removed.Count == 0)
            return;

        var updates = new List<FriendListUpdateSnapshot>();
        foreach (var friendId in removed)
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
