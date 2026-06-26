using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Pets;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Room.Pets;

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

        IRoomGrain room = _grainFactory.GetRoomGrain(ctx.RoomId);

        await room.BreedPetsAsync(ctx.AsActionContext(), message.PetOneId, message.PetTwoId, ct)
            .ConfigureAwait(false);
    }
}
