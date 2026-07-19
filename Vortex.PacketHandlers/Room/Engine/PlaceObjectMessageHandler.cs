using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.PacketHandlers.Room.Engine;

public class PlaceObjectMessageHandler(IRoomService roomService)
    : IMessageHandler<PlaceObjectMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        PlaceObjectMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        string[] position = message.Data.Split(' ');

        if (position.Length != 4)
        {
            return;
        }

        int itemId = int.TryParse(position[0], out int id) ? Math.Abs(id) : -1;
        string location = string.Join(' ', new[] { position[1], position[2], position[3] });

        position = location.Split(' ');

        if (location.StartsWith(':'))
        {
            string[] coords = position[0][3..].Split(',');
            string[] loc = position[1][2..].Split(',');
            Rotation rot = position[2].Equals("l") ? Rotation.South : Rotation.West;

            if (coords.Length != 2 || loc.Length != 2)
            {
                return;
            }

            await _roomService
                .PlaceWallItemInRoomAsync(
                    ctx.AsActionContext(),
                    itemId,
                    int.TryParse(coords[0], out int x) ? x : 0,
                    int.TryParse(coords[1], out int y) ? y : 0,
                    double.TryParse(loc[1], out double z) ? z : 0,
                    int.TryParse(loc[0], out int wallOffset) ? wallOffset : 0,
                    rot,
                    ct
                )
                .ConfigureAwait(false);
        }
        else
        {
            await _roomService
                .PlaceFloorItemInRoomAsync(
                    ctx.AsActionContext(),
                    itemId,
                    int.TryParse(position[0], out int xPos) ? xPos : 0,
                    int.TryParse(position[1], out int yPos) ? yPos : 0,
                    int.TryParse(position[2], out int rotation)
                        ? (Rotation)rotation
                        : Rotation.North,
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
