using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Snapshots.Mapping;
using Vortex.Protocol.Messages.Incoming.Room.Layout;
using Vortex.Protocol.Messages.Outgoing.Room.Layout;

namespace Vortex.PacketHandlers.Room.Layout;

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

        IRoomMap room = _grainFactory.GetRoomMap(ctx.RoomId);
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
