using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Navigator;

public class DeleteFavouriteRoomMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<DeleteFavouriteRoomMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        DeleteFavouriteRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetPlayerNavigatorGrain(ctx.PlayerId)
            .RemoveFavouriteRoomAsync(message.RoomId, ct)
            .ConfigureAwait(false);
    }
}
