using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.FriendList;
using Vortex.Protocol.Messages.Outgoing.FriendList;

namespace Vortex.PacketHandlers.FriendList;

public class VisitUserMessageHandler(IGrainFactory grainFactory, IRoomService roomService)
    : IMessageHandler<VisitUserMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        VisitUserMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || string.IsNullOrWhiteSpace(message.PlayerName))
        {
            return;
        }

        IPlayerDirectoryGrain directory = _grainFactory.GetPlayerDirectoryGrain();
        PlayerId? targetId = await directory
            .GetPlayerIdAsync(message.PlayerName, ct)
            .ConfigureAwait(false);

        if (targetId is null)
        {
            return;
        }

        IPlayerPresenceGrain targetPresence = _grainFactory.GetPlayerPresenceGrain(targetId.Value);
        bool isOnline = await targetPresence.IsOnlineAsync(ct).ConfigureAwait(false);

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

        RoomPointerSnapshot activeRoom = await targetPresence
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

        // Following a friend is an ordinary room entry, not a teleport: routing it through the room
        // service is what keeps the ban list, the capacity limit, the password door and the doorbell
        // in play. Setting the active room directly would walk the follower straight past all four.
        await _roomService
            .OpenRoomForPlayerIdAsync(ctx.AsActionContext(), ctx.PlayerId, activeRoom.RoomId, ct)
            .ConfigureAwait(false);
    }
}
