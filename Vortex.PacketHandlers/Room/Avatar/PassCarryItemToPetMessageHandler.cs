using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Incoming.Room.Avatar;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Room.Avatar;

public class PassCarryItemToPetMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<PassCarryItemToPetMessage>
{
    public async ValueTask HandleAsync(
        PassCarryItemToPetMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.PetId <= 0)
        {
            return;
        }

        await grainFactory
            .GetRoomAvatars(ctx.RoomId)
            .PassCarryItemToPetAsync(ctx.AsActionContext(), message.PetId, ct)
            .ConfigureAwait(false);
    }
}
