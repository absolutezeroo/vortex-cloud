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

public class AcceptFriendMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<AcceptFriendMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        AcceptFriendMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.Friends.Count == 0)
            return;

        var grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);

        var failures = await grain
            .AcceptFriendRequestsAsync(message.Friends, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new AcceptFriendResultMessageComposer { Failures = failures },
                ct
            )
            .ConfigureAwait(false);

        // Send updated friend list to self
        var friends = await grain.GetFriendsAsync(ct).ConfigureAwait(false);
        var newFriends = new List<FriendListUpdateSnapshot>();

        foreach (var friendId in message.Friends)
        {
            var snapshot = friends.Find(f => f.PlayerId.Value == friendId);
            if (snapshot is null)
                continue;

            newFriends.Add(
                new FriendListUpdateSnapshot
                {
                    ActionType = FriendListUpdateActionType.Added,
                    FriendId = friendId,
                    Friend = snapshot,
                }
            );
        }

        if (newFriends.Count > 0)
        {
            await ctx.SendComposerAsync(
                    new FriendListUpdateMessageComposer
                    {
                        FriendCategories = [],
                        Updates = newFriends,
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
