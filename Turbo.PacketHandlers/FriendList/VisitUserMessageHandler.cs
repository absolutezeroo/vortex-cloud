using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.FriendList.Enums;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;

namespace Turbo.PacketHandlers.FriendList;

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
