using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Protocol.Messages.Incoming.Room.Action;

namespace Vortex.PacketHandlers.Room.Action;

/// <summary>
/// The room-info panel's "mute all" button. The message carries nothing — it is a toggle, and the
/// room answers with where the switch landed.
/// </summary>
public class MuteAllInRoomMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<MuteAllInRoomMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        MuteAllInRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        IRoomModeration roomGrain = _grainFactory.GetRoomModeration(ctx.RoomId);

        await roomGrain.ToggleAllInRoomMuteAsync(ctx.AsActionContext()).ConfigureAwait(false);
    }
}
