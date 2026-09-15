using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Navigator;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players.Grains;
using Vortex.Protocol.Messages.Incoming.FriendList;
using Vortex.Protocol.Messages.Outgoing.FriendList;

namespace Vortex.PacketHandlers.FriendList;

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

        // Forwarded, not placed. Calling SetActiveRoomAsync here put the follower straight into the
        // room grain, past every gate RoomService owns -- the room ban, the population cap, the
        // password, the doorbell, raid protection and the cancellable entry event -- and without a
        // single entry packet, so the server had an avatar standing in a locked room that the
        // client did not know it was in. This is the same answer the navigator gives when it picks
        // a room for the player: the client is told where to go and runs its own connect handshake,
        // which arrives as OpenFlatConnection and passes through all of it.
        await RoomForwardHelper
            .SendGuestRoomResultAsync(
                _grainFactory,
                ctx,
                activeRoom.RoomId,
                enterRoom: true,
                roomForward: true,
                ct
            )
            .ConfigureAwait(false);
    }
}
