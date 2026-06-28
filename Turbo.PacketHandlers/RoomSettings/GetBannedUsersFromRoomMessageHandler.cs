using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.RoomSettings;
using Turbo.Primitives.Messages.Outgoing.Roomsettings;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.RoomSettings;

public class GetBannedUsersFromRoomMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetBannedUsersFromRoomMessage>
{
    public async ValueTask HandleAsync(
        GetBannedUsersFromRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(message.RoomId);
        ImmutableArray<RoomControllerSnapshot> bannedUsers = await roomGrain
            .GetBannedUsersAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new BannedUsersFromRoomEventMessageComposer
                {
                    RoomId = message.RoomId,
                    BannedUsers = bannedUsers,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
