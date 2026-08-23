using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Protocol.Messages.Incoming.Room.Avatar;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Room.Avatar;

public class DropCarryItemMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<DropCarryItemMessage>
{
    public async ValueTask HandleAsync(
        DropCarryItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        await grainFactory
            .GetRoomAvatars(ctx.RoomId)
            .DropCarryItemAsync(ctx.AsActionContext(), ct)
            .ConfigureAwait(false);
    }
}
