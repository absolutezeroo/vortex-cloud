using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.RoomSettings;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.RoomSettings;

public class UpdateRoomCategoryAndTradeSettingsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<UpdateRoomCategoryAndTradeSettingsMessage>
{
    public async ValueTask HandleAsync(
        UpdateRoomCategoryAndTradeSettingsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(message.RoomId);

        await roomGrain
            .UpdateCategoryAndTradeAsync(ctx.PlayerId, message.CategoryId, message.TradeType, ct)
            .ConfigureAwait(false);
    }
}
