using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Engine;

public class TogglePetRidingPermissionMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<TogglePetRidingPermissionMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        TogglePetRidingPermissionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.PetId <= 0)
        {
            return;
        }

        IRoomPets room = _grainFactory.GetRoomPets(ctx.RoomId);

        await room.TogglePetRidingPermissionAsync(ctx.AsActionContext(), message.PetId, ct)
            .ConfigureAwait(false);
    }
}
