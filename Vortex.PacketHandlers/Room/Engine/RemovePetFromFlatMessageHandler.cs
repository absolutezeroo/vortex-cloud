using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Rooms;

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
