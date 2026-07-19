using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

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

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(ctx.RoomId);

        await roomGrain
            .SetRoomTagsAsync(ctx.PlayerId, message.Tag1, message.Tag2, ct)
            .ConfigureAwait(false);
    }
}
