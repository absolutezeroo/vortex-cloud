using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Navigator;

public class RemoveOwnRoomRightsRoomMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RemoveOwnRoomRightsRoomMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RemoveOwnRoomRightsRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(message.RoomId);
        await roomGrain.RemoveOwnRightsAsync(ctx.PlayerId, ct).ConfigureAwait(false);
    }
}
