using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Pets;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.PacketHandlers.Room.Pets;

public class CustomizePetWithFurniMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<CustomizePetWithFurniMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        CustomizePetWithFurniMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.PetId <= 0 || message.FurniItemId <= 0)
        {
            return;
        }

        IRoomPets room = _grainFactory.GetRoomPets(ctx.RoomId);

        await room.FeedPetAsync(
                ctx.AsActionContext(),
                message.PetId,
                new RoomObjectId(message.FurniItemId),
                ct
            )
            .ConfigureAwait(false);
    }
}
