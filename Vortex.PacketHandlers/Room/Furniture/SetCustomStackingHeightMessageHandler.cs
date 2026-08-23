using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// Sets the altitude of a magic stack tile. The widget sends this on every slider release, arrow
/// press and text entry, so the room decides what is in range — the client will happily ask for a
/// height typed by hand.
/// </summary>
public class SetCustomStackingHeightMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetCustomStackingHeightMessage>
{
    public async ValueTask HandleAsync(
        SetCustomStackingHeightMessage message,
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
            .SetCustomStackHeightAsync(
                ctx.AsActionContext(),
                message.ObjectId,
                message.Height,
                message.MultiWalkMode,
                ct
            )
            .ConfigureAwait(false);
    }
}
