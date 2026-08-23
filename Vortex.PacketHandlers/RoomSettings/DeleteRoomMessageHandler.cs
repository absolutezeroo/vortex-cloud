using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.RoomSettings;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.RoomSettings;

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

        IRoomSettings roomGrain = grainFactory.GetRoomSettings(message.RoomId);
        await roomGrain.DeleteRoomAsync(ctx.PlayerId, ct).ConfigureAwait(false);
    }
}
