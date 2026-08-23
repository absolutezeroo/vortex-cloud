using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

public class RemovePetFromFlatMessageHandler(IRoomService roomService)
    : IMessageHandler<RemovePetFromFlatMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        RemovePetFromFlatMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .PickUpPetInRoomAsync(ctx.AsActionContext(), message.PetId, ct)
            .ConfigureAwait(false);
    }
}
