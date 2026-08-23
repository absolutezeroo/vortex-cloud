using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Protocol.Messages.Incoming.Room.Action;

namespace Vortex.PacketHandlers.Room.Action;

public class RemoveAllRightsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RemoveAllRightsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RemoveAllRightsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        IRoomSettings roomGrain = _grainFactory.GetRoomSettings(ctx.RoomId);
        await roomGrain.RemoveAllRightsAsync(ctx.PlayerId, ct).ConfigureAwait(false);
    }
}
