using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// Sticking a note on the wall. A sticky gets its own placement message rather than going through
/// PlaceObject because the client opens the note editor as soon as the server confirms it — the
/// text arrives afterwards, through AddSpamWallPostItMessage.
/// <para>
/// <c>ObjectId</c> here is the inventory item being placed, not something already in the room, so
/// this goes down the same ownership- and rights-checked path as any other wall item.
/// </para>
/// </summary>
public class PlacePostItMessageHandler(IRoomService roomService)
    : IMessageHandler<PlacePostItMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        PlacePostItMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (
            ctx.PlayerId <= 0
            || ctx.RoomId <= 0
            || !WallLocationParser.TryParse(
                message.Location,
                out int x,
                out int y,
                out double z,
                out int wallOffset,
                out Rotation rot
            )
        )
        {
            return;
        }

        await _roomService
            .PlaceWallItemInRoomAsync(
                ctx.AsActionContext(),
                message.ObjectId.Value,
                x,
                y,
                z,
                wallOffset,
                rot,
                ct
            )
            .ConfigureAwait(false);
    }
}
