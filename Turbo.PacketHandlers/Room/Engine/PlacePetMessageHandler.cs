using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Engine;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.PacketHandlers.Room.Engine;

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
