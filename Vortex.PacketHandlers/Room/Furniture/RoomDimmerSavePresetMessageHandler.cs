using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// Stores one of the moodlight's three presets, and switches to it when the dialog's apply button
/// is what sent this.
/// </summary>
public class RoomDimmerSavePresetMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RoomDimmerSavePresetMessage>
{
    public async ValueTask HandleAsync(
        RoomDimmerSavePresetMessage message,
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
            .SaveDimmerPresetAsync(
                ctx.AsActionContext(),
                message.ObjectId,
                message.PresetNumber,
                message.EffectId,
                message.ColorHex,
                message.Brightness,
                message.Apply,
                ct
            )
            .ConfigureAwait(false);

        if (state is null)
        {
            return;
        }

        await ctx.SendComposerAsync(DimmerPresets.Compose(state), ct).ConfigureAwait(false);
    }
}
