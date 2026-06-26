using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Pets;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Inventory.Pets;

public class ConfirmPetBreedingMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ConfirmPetBreedingMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        ConfirmPetBreedingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.PetId <= 0)
        {
            return;
        }

        IRoomGrain room = _grainFactory.GetRoomGrain(ctx.RoomId);

        await room.ConfirmPetBreedingAsync(ctx.AsActionContext(), message.PetId, ct)
            .ConfigureAwait(false);
    }
}
