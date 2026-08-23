using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Protocol.Messages.Incoming.Room.Pets;

namespace Vortex.PacketHandlers.Room.Pets;

/// <summary>Composts a withered monsterplant. Destructive, so the room re-checks that the plant really is
/// dead and that the caller may do it.</summary>
public class CompostPlantMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<CompostPlantMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        CompostPlantMessage message,
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
            .CompostPlantAsync(ctx.AsActionContext(), message.PetId, ct)
            .ConfigureAwait(false);
    }
}
