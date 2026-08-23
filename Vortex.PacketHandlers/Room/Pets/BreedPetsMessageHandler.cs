using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Protocol.Messages.Incoming.Room.Pets;

namespace Vortex.PacketHandlers.Room.Pets;

public class BreedPetsMessageHandler(IGrainFactory grainFactory) : IMessageHandler<BreedPetsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        BreedPetsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.PetOneId <= 0 || message.PetTwoId <= 0)
        {
            return;
        }

        IRoomPets room = _grainFactory.GetRoomPets(ctx.RoomId);

        await room.BreedPetsAsync(ctx.AsActionContext(), message.PetOneId, message.PetTwoId, ct)
            .ConfigureAwait(false);
    }
}
