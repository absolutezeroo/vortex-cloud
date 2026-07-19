using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.PacketHandlers.Room.Engine;

public class PlacePetMessageHandler(IRoomService roomService) : IMessageHandler<PlacePetMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        PlacePetMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .PlacePetInRoomAsync(
                ctx.AsActionContext(),
                message.PetId,
                message.X,
                message.Y,
                Rotation.South,
                ct
            )
            .ConfigureAwait(false);
    }
}
