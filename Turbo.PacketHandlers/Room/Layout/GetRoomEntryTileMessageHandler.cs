using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Layout;
using Turbo.Primitives.Messages.Outgoing.Room.Layout;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Grains;
using Turbo.Primitives.Rooms.Snapshots.Mapping;

namespace Turbo.PacketHandlers.Room.Layout;

public class GetRoomEntryTileMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetRoomEntryTileMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetRoomEntryTileMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        IRoomGrain room = _grainFactory.GetRoomGrain(ctx.RoomId);
        RoomMapSnapshot map = await room.GetMapSnapshotAsync(ct).ConfigureAwait(false);

        await _grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new RoomEntryTileMessageComposer
                {
                    X = map.DoorX,
                    Y = map.DoorY,
                    Rotation = map.DoorRotation,
                }
            )
            .ConfigureAwait(false);
    }
}
