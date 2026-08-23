using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// The moodlight's on/off switch. The room colour itself reaches everyone through the furni's
/// stuff-data refresh; this reply is only for the dialog the switch was pressed in, which draws its
/// own on/off state from the presets packet.
/// </summary>
public class RoomDimmerChangeStateMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RoomDimmerChangeStateMessage>
{
    public async ValueTask HandleAsync(
        RoomDimmerChangeStateMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        RoomDimmerStateSnapshot? state = await grainFactory
            .GetRoomFurni(ctx.RoomId)
            .ToggleDimmerAsync(ctx.AsActionContext(), message.ObjectId, ct)
            .ConfigureAwait(false);

        if (state is null)
        {
            return;
        }

        await ctx.SendComposerAsync(DimmerPresets.Compose(state), ct).ConfigureAwait(false);
    }
}
