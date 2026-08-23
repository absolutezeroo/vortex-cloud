using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

public class RemoveBotFromFlatMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RemoveBotFromFlatMessage>
{
    public async ValueTask HandleAsync(
        RemoveBotFromFlatMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.BotId <= 0)
        {
            return;
        }

        await grainFactory
            .GetRoomBots(ctx.RoomId)
            .RemoveBotAsync(ctx.AsActionContext(), message.BotId, ct)
            .ConfigureAwait(false);
    }
}
