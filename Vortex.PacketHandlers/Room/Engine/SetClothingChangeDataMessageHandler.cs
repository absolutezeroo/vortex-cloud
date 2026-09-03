using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

/// <summary>
/// Sets one gender's outfit on a clothing-change booth. The merge with the other gender's belongs to
/// the room grain, which is where the item's data lives.
/// </summary>
public class SetClothingChangeDataMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetClothingChangeDataMessage>
{
    public async ValueTask HandleAsync(
        SetClothingChangeDataMessage message,
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
            .SetClothingChangeDataAsync(
                ctx.AsActionContext(),
                message.ItemId,
                message.Gender,
                message.Look,
                ct
            )
            .ConfigureAwait(false);
    }
}
