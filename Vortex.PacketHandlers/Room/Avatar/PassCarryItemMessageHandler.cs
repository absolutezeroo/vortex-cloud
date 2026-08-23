using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Protocol.Messages.Incoming.Room.Avatar;

namespace Vortex.PacketHandlers.Room.Avatar;

public class PassCarryItemMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<PassCarryItemMessage>
{
    public async ValueTask HandleAsync(
        PassCarryItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.TargetPlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetRoomAvatars(ctx.RoomId)
            .PassCarryItemAsync(ctx.AsActionContext(), new PlayerId(message.TargetPlayerId), ct)
            .ConfigureAwait(false);
    }
}
