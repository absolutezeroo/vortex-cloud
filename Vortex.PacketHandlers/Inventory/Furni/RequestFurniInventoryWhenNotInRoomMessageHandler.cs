using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Furni;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Inventory.Furni;

public class RequestFurniInventoryWhenNotInRoomMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RequestFurniInventoryWhenNotInRoomMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RequestFurniInventoryWhenNotInRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(ctx.PlayerId);

        await presence.OpenFurnitureInventoryAsync(ct).ConfigureAwait(false);
    }
}
