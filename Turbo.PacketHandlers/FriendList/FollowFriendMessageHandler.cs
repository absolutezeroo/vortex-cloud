using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.FriendList.Enums;
using Turbo.Primitives.FriendList.Grains;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players.Grains;

namespace Turbo.PacketHandlers.FriendList;

public class FollowFriendMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<FollowFriendMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        FollowFriendMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        bool isFriend = await grain.IsFriendAsync(message.PlayerId, ct).ConfigureAwait(false);

        if (!isFriend)
        {
            await ctx.SendComposerAsync(
                    new FollowFriendFailedMessageComposer
                    {
                        ErrorCode = FollowFriendErrorCodeType.NotFriend,
                    },
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        IPlayerPresenceGrain friendPresence = _grainFactory.GetPlayerPresenceGrain(
            message.PlayerId
        );
        bool isOnline = await friendPresence.IsOnlineAsync(ct).ConfigureAwait(false);

        if (!isOnline)
        {
            await ctx.SendComposerAsync(
                    new FollowFriendFailedMessageComposer
                    {
                        ErrorCode = FollowFriendErrorCodeType.Offline,
                    },
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        RoomPointerSnapshot activeRoom = await friendPresence
            .GetActiveRoomAsync()
            .ConfigureAwait(false);

        if (activeRoom.RoomId <= 0)
        {
            await ctx.SendComposerAsync(
                    new FollowFriendFailedMessageComposer
                    {
                        ErrorCode = FollowFriendErrorCodeType.HotelView,
                    },
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        // Navigate self to friend's room via our own presence
        IPlayerPresenceGrain selfPresence = _grainFactory.GetPlayerPresenceGrain(ctx.PlayerId);
        await selfPresence.SetActiveRoomAsync(activeRoom.RoomId, ct).ConfigureAwait(false);
    }
}
