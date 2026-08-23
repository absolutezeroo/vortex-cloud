using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

public class PlaceBotMessageHandler(IGrainFactory grainFactory) : IMessageHandler<PlaceBotMessage>
{
    public async ValueTask HandleAsync(
        PlaceBotMessage message,
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
            .PlaceBotAsync(ctx.AsActionContext(), message.BotId, message.X, message.Y, ct)
            .ConfigureAwait(false);
    }
}
