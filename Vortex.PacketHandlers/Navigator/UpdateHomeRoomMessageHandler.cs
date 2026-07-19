using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Navigator;

public class UpdateHomeRoomMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<UpdateHomeRoomMessage>
{
    public async ValueTask HandleAsync(
        UpdateHomeRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerNavigatorGrain(ctx.PlayerId)
            .SetHomeRoomIdAsync(message.RoomId, ct)
            .ConfigureAwait(false);
    }
}
