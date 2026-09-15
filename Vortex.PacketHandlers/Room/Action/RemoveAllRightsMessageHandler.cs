using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Protocol.Messages.Incoming.Room.Action;

namespace Vortex.PacketHandlers.Room.Action;

public class RemoveAllRightsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RemoveAllRightsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RemoveAllRightsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        // The dialog is usually opened from the navigator, with the player standing in the hotel
        // view or in another room entirely, so the session's room is not the room being edited.
        RoomId targetRoomId = message.RoomId > 0 ? new RoomId(message.RoomId) : ctx.RoomId;

        if (ctx.PlayerId <= 0 || targetRoomId <= 0)
        {
            return;
        }

        IRoomSettings roomGrain = _grainFactory.GetRoomSettings(targetRoomId);
        await roomGrain.RemoveAllRightsAsync(ctx.PlayerId, ct).ConfigureAwait(false);
    }
}
