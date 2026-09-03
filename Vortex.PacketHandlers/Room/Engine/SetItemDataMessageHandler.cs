using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

/// <summary>
/// Writes a sticky note. The room grain owns the merge into one legacy string and the broadcast, so
/// this is the same path the spam-wall note already takes.
/// </summary>
public class SetItemDataMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetItemDataMessage>
{
    public async ValueTask HandleAsync(
        SetItemDataMessage message,
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
            .SetPostItAsync(
                ctx.AsActionContext(),
                message.ItemId,
                message.ColorHex,
                message.Text,
                ct
            )
            .ConfigureAwait(false);
    }
}
