using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Pets;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Grains;
using Turbo.Primitives.Rooms.Object;

namespace Turbo.PacketHandlers.Room.Pets;

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

        IRoomGrain room = _grainFactory.GetRoomGrain(ctx.RoomId);

        await room.FeedPetAsync(
                ctx.AsActionContext(),
                message.PetId,
                new RoomObjectId(message.FurniItemId),
                ct
            )
            .ConfigureAwait(false);
    }
}
