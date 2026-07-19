using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Navigator;

public class RateFlatMessageHandler(IGrainFactory grainFactory) : IMessageHandler<RateFlatMessage>
{
    public async ValueTask HandleAsync(
        RateFlatMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(ctx.RoomId);

        await roomGrain.RateRoomAsync(ctx.PlayerId, message.Points, ct).ConfigureAwait(false);
    }
}
