using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Engine;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.PacketHandlers.Room.Engine;

public class MoveWallItemMessageHandler(IRoomService roomService)
    : IMessageHandler<MoveWallItemMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        MoveWallItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        // :w=4,11 l=32,57 l
        string[] position = message.WallPosition.Split(' ');

        if (position.Length != 3)
        {
            return;
        }

        string[] coords = position[0][3..].Split(',');
        string[] loc = position[1][2..].Split(',');
        Rotation rot = position[2].Equals("l") ? Rotation.South : Rotation.West;

        if (coords.Length != 2 || loc.Length != 2)
        {
            return;
        }

        if (!int.TryParse(coords[0], out int x))
        {
            return;
        }

        if (!int.TryParse(coords[1], out int y))
        {
            return;
        }

        if (!int.TryParse(loc[0], out int wallOffset))
        {
            return;
        }

        if (!double.TryParse(loc[1], out double z))
        {
            return;
        }

        await _roomService
            .MoveWallItemInRoomAsync(
                ctx.AsActionContext(),
                message.ObjectId,
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
