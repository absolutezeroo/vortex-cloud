using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Room.Layout;
using Vortex.Protocol.Messages.Outgoing.Room.Layout;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Layout;

public class GetOccupiedTilesMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetOccupiedTilesMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetOccupiedTilesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        // The floor-plan editor opens on this answer. It used to be an empty packet, which the
        // client reads as "zero tiles occupied" rather than "no answer" -- so every tile looked
        // free and you could redraw the floor out from under a stack of furniture.
        ImmutableArray<(int X, int Y)> tiles = await _grainFactory
            .GetRoomMap(ctx.RoomId)
            .GetOccupiedTilesAsync(ct)
            .ConfigureAwait(false);

        await _grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(new RoomOccupiedTilesMessageComposer { Tiles = tiles })
            .ConfigureAwait(false);
    }
}
