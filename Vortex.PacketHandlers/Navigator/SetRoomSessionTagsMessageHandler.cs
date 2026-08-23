using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Protocol.Messages.Incoming.Navigator;

namespace Vortex.PacketHandlers.Navigator;

public class SetRoomSessionTagsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetRoomSessionTagsMessage>
{
    public async ValueTask HandleAsync(
        SetRoomSessionTagsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        IRoomSettings roomGrain = grainFactory.GetRoomSettings(ctx.RoomId);

        await roomGrain
            .SetRoomTagsAsync(ctx.PlayerId, message.Tag1, message.Tag2, ct)
            .ConfigureAwait(false);
    }
}
