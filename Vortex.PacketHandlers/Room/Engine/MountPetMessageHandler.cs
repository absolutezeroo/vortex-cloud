using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Engine;

public class MountPetMessageHandler(IGrainFactory grainFactory) : IMessageHandler<MountPetMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        MountPetMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.PetId <= 0)
        {
            return;
        }

        IRoomPets room = _grainFactory.GetRoomPets(ctx.RoomId);

        await room.MountPetAsync(ctx.AsActionContext(), message.PetId, message.Mount, ct)
            .ConfigureAwait(false);
    }
}
