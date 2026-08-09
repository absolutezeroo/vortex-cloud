using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Pets;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Pets;

/// <summary>Harvests a full-grown monsterplant. Ownership, growth and the remaining seed charge are all
/// checked in the room, which owns the pet.</summary>
public class HarvestPetMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<HarvestPetMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        HarvestPetMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.PetId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetRoomPets(ctx.RoomId)
            .HarvestPlantAsync(ctx.AsActionContext(), message.PetId, ct)
            .ConfigureAwait(false);
    }
}
