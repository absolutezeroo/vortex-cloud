using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Layout;
using Vortex.Primitives.Messages.Outgoing.Room.Layout;
using Vortex.Primitives.Orleans;

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
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(new RoomOccupiedTilesMessageComposer())
            .ConfigureAwait(false);
    }
}
