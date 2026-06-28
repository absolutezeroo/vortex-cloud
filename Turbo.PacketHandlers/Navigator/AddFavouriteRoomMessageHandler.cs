using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Navigator;

public class AddFavouriteRoomMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<AddFavouriteRoomMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        AddFavouriteRoomMessage message,
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
            .AddFavouriteRoomAsync(message.RoomId, ct)
            .ConfigureAwait(false);
    }
}
