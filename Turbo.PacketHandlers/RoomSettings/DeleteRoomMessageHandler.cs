using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.RoomSettings;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.RoomSettings;

public class DeleteRoomMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<DeleteRoomMessage>
{
    public async ValueTask HandleAsync(
        DeleteRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(message.RoomId);
        await roomGrain.DeleteRoomAsync(ctx.PlayerId, ct).ConfigureAwait(false);
    }
}
