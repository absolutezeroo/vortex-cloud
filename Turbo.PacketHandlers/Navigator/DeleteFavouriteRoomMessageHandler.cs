using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Navigator;

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
