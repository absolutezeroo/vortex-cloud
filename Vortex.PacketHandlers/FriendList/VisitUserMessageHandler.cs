using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.FriendList;

public class VisitUserMessageHandler(IGrainFactory grainFactory) : IMessageHandler<VisitUserMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

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

        IPlayerPresenceGrain selfPresence = _grainFactory.GetPlayerPresenceGrain(ctx.PlayerId);
        await selfPresence.SetActiveRoomAsync(activeRoom.RoomId, ct).ConfigureAwait(false);
    }
}
