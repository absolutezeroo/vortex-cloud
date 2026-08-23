using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

/// <summary>
/// The bin button on a sticky note or a photo. Distinct from picking furniture up: this destroys
/// what it names, so the room decides what may be destroyed rather than trusting the packet.
/// </summary>
public class RemoveItemMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RemoveItemMessage>
{
    public async ValueTask HandleAsync(
        RemoveItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        await grainFactory
            .GetRoomFurni(ctx.RoomId)
            .DeleteDisposableWallItemAsync(ctx.AsActionContext(), message.ObjectId, ct)
            .ConfigureAwait(false);
    }
}
