using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Navigator;

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
