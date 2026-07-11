using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Navigator;

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
