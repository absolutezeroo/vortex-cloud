using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Navigator;

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
