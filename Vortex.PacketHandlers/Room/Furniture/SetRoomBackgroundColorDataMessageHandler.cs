using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// Applies a background toner's colour. The switch that turns it on is not this packet — the widget
/// sends a plain UseFurniture for that — so this only ever carries HSL.
/// </summary>
public class SetRoomBackgroundColorDataMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetRoomBackgroundColorDataMessage>
{
    public async ValueTask HandleAsync(
        SetRoomBackgroundColorDataMessage message,
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
            .SetBackgroundColorAsync(
                ctx.AsActionContext(),
                message.ObjectId,
                message.Hue,
                message.Saturation,
                message.Lightness,
                ct
            )
            .ConfigureAwait(false);
    }
}
