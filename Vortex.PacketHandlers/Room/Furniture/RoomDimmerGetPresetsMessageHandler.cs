using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;
using Vortex.Protocol.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// Answers the moodlight dialog. The client cannot draw the window from anything it already holds —
/// the two presets it is not showing exist only on the server — so an unanswered request leaves the
/// dialog empty.
/// </summary>
public class RoomDimmerGetPresetsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RoomDimmerGetPresetsMessage>
{
    public async ValueTask HandleAsync(
        RoomDimmerGetPresetsMessage message,
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
            .GetDimmerStateAsync(ctx.AsActionContext(), message.ObjectId, ct)
            .ConfigureAwait(false);

        if (state is null)
        {
            return;
        }

        await ctx.SendComposerAsync(DimmerPresets.Compose(state), ct).ConfigureAwait(false);
    }
}
