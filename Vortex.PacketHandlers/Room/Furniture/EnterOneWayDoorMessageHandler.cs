using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// Walks a player through a one-way gate. Silent when they are not in position or the far side is
/// taken — the client sends this on every double-click, wherever the player is standing.
/// </summary>
public class EnterOneWayDoorMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<EnterOneWayDoorMessage>
{
    public async ValueTask HandleAsync(
        EnterOneWayDoorMessage message,
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
            .EnterOneWayDoorAsync(ctx.AsActionContext(), message.ObjectId, ct)
            .ConfigureAwait(false);
    }
}
